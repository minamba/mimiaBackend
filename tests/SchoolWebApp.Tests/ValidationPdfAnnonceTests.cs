using System.Text;
using SchoolWebApp.Api.Services.PiecesJointes;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Writer;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// UN PDF REFUSÉ SANS RAISON (25/11/2026) : une maman ne pouvait plus envoyer de PDF sur le bloc physique-chimie
    /// d'un brevet blanc. Deux contrôles trop stricts sont assouplis — une annonce « fichier quelconque » ne vaut plus
    /// désaccord, et quelques octets devant « %PDF » sont retirés — sans rien ouvrir de ce qui était refusé à raison.
    /// </summary>
    public class ValidationPdfAnnonceTests
    {
        private static byte[] PdfDUnePage()
        {
            var builder = new PdfDocumentBuilder();
            builder.AddPage(PageSize.A4);
            return builder.Build();
        }

        [Theory]
        [InlineData("application/octet-stream")]
        [InlineData("binary/octet-stream")]
        [InlineData("application/x-download")]
        [InlineData("application/x-pdf")]
        [InlineData("application/pdf; charset=binary")]
        [InlineData("")]
        [InlineData(null)]
        public void Un_pdf_annonce_vaguement_ou_sous_un_ancien_nom_est_accepte(string? annonce)
        {
            var verdict = ValidationPieceJointe.Valider(PdfDUnePage(), annonce);

            Assert.True(verdict.Accepte, verdict.Motif);
            Assert.Equal(1, verdict.NombrePages);
        }

        [Fact]
        public void Un_vrai_desaccord_reste_refuse()
        {
            var verdict = ValidationPieceJointe.Valider(PdfDUnePage(), "image/png");

            Assert.False(verdict.Accepte);
            Assert.Equal("Ce fichier ne correspond pas à son format.", verdict.Motif);
        }

        [Fact]
        public void Les_octets_devant_l_en_tete_sont_retires_et_le_pdf_passe()
        {
            var pdf = PdfDUnePage();
            var avecPrefixe = Encoding.ASCII.GetBytes("\r\n   ").Concat(pdf).ToArray();

            Assert.Equal(5, ValidationPieceJointe.DebutPdf(avecPrefixe));

            var propre = ValidationPieceJointe.RognerAvantPdf(avecPrefixe);
            Assert.Equal(pdf, propre);
            Assert.True(ValidationDocumentCours.ValiderSansPlafond(propre, "application/pdf", 100 * 1024 * 1024).Accepte);
        }

        [Fact]
        public void Un_fichier_deja_reconnu_ou_office_n_est_jamais_rogne()
        {
            // Une « photo » dont les premiers octets contiennent « %PDF » : signature JPEG, donc intouchable.
            var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }.Concat(Encoding.ASCII.GetBytes("xx%PDF-1.4")).ToArray();
            Assert.Same(jpeg, ValidationPieceJointe.RognerAvantPdf(jpeg));

            var office = Encoding.ASCII.GetBytes("PK\u0003\u0004 ... %PDF-1.4");
            Assert.Same(office, ValidationPieceJointe.RognerAvantPdf(office));
        }

        [Fact]
        public void Un_en_tete_au_dela_du_premier_kilo_octet_n_est_pas_un_pdf()
        {
            var loin = new byte[ValidationPieceJointe.FenetreEnTetePdf].Concat(PdfDUnePage()).ToArray();

            Assert.Equal(-1, ValidationPieceJointe.DebutPdf(loin));
            Assert.Same(loin, ValidationPieceJointe.RognerAvantPdf(loin));
        }
    }
}
