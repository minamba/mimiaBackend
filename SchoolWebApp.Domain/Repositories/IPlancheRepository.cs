using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Les planches importées qui remplacent les dessins des professeurs.
    ///
    /// Le dépôt ne connaît AUCUN catalogue : il ne sait pas quelles figures
    /// existent, seulement lesquelles ont été importées. La liste des clés
    /// possibles vit dans le front, où l'administration et le tableau la
    /// partagent déjà — la dupliquer ici n'ajouterait qu'une occasion de
    /// divergence.
    /// </summary>
    public interface IPlancheRepository
    {
        /// <summary>
        /// Toutes les planches importées, SANS leurs octets. C'est ce que
        /// l'administration affiche pour cocher les lignes.
        /// </summary>
        Task<IEnumerable<Planche>> GetToutesAsync(CancellationToken ct = default);

        /// <summary>
        /// Ce qui attend d'être traité, par file.
        ///
        /// SANS CET ÉCRAN, ON INTERROGE LA BASE À LA MAIN. C'est ce qui a fait
        /// passer une nuit entière à chercher pourquoi la facture montait : le
        /// journal dit ce qui a été dépensé, jamais ce qui reste à faire.
        /// </summary>
        Task<FilesPlanches> GetFilesAsync(CancellationToken ct = default);

        /// <summary>
        /// Vide les files SANS appeler le modèle : chaque planche en attente
        /// est marquée comme traitée, avec un contenu qui dit qu'elle ne l'a
        /// pas été.
        ///
        /// Le coupe-feu de l'administrateur. Une figure que le modèle n'arrive
        /// pas à lire bloquait la file et se payait à chaque tour ; il fallait
        /// une mise à jour SQL sur la production pour s'en sortir. Un bouton
        /// vaut mieux qu'un accès base à deux heures du matin.
        /// </summary>
        Task<int> ViderLesFilesAsync(CancellationToken ct = default);

        /// <summary>Une planche avec ses octets, pour la servir. Null si absente.</summary>
        Task<Planche?> GetAsync(string cle, CancellationToken ct = default);

        /// <summary>
        /// Une planche SANS ses octets, pour son crédit.
        ///
        /// Le crédit est demandé à chaque affichage, en même temps que l'image.
        /// Charger le varbinary pour rendre trois chaînes doublerait le poids
        /// transféré depuis la base à chaque schéma montré en séance.
        /// </summary>
        Task<Planche?> GetSansDonneesAsync(string cle, CancellationToken ct = default);

        /// <summary>
        /// Importe une planche, ou remplace celle qui portait déjà cette clé.
        ///
        /// Un remplacement plutôt qu'un doublon : réimporter une figure est
        /// l'opération normale — on trouve mieux, on écrase. Deux versions
        /// coexistantes rendraient imprévisible celle qui s'affiche.
        /// </summary>
        Task<Planche> ImporterAsync(Planche planche, CancellationToken ct = default);

        /// <summary>Retire une planche. Le professeur redessine à la main.</summary>
        Task<bool> SupprimerAsync(string cle, CancellationToken ct = default);

        /// <summary>
        /// Les planches dont la description reste à extraire : octets présents,
        /// contenu vide. Rendues avec leurs octets, pour être lues.
        /// </summary>
        Task<IEnumerable<Planche>> GetADecrireAsync(int limite, CancellationToken ct = default);

        /// <summary>Enregistre la liste des légendes d'une planche.</summary>
        Task EnregistrerContenuAsync(int id, string contenu, CancellationToken ct = default);

        /// <summary>
        /// Les planches écartées pour cause de langue, SANS leurs octets.
        ///
        /// Sert à les re-juger quand la règle de langue change — et elle a
        /// changé deux fois. Les légendes sont déjà extraites : re-juger ne
        /// coûte qu un appel de texte de quelques jetons, là où réimporter
        /// repaierait une lecture d image par planche.
        /// </summary>
        Task<IEnumerable<Planche>> GetMarqueesEtrangeresAsync(CancellationToken ct = default);

        /// <summary>
        /// Les planches importées sans mention d'origine — typiquement celles
        /// déposées à la main depuis l'administration. Rendues avec leurs
        /// octets : c'est leur empreinte qui permettra de les identifier.
        /// </summary>
        Task<IEnumerable<Planche>> GetSansCreditAsync(int limite, CancellationToken ct = default);

        /// <summary>
        /// Les planches décrites dont on ne connaît pas encore la POSITION des
        /// étiquettes. Elles portent leurs octets : c'est une lecture par un
        /// modèle de vision qui les attend.
        /// </summary>
        Task<IEnumerable<Planche>> GetSansReperesAsync(int limite, CancellationToken ct = default);

        /// <summary>Enregistre la carte des étiquettes, en JSON.</summary>
        Task EnregistrerReperesAsync(int id, string reperes, CancellationToken ct = default);

        /// <summary>Enregistre l'auteur, la source et la licence d'une planche.</summary>
        Task EnregistrerCreditAsync(
            int id, string? auteur, string? source, string? licence,
            CancellationToken ct = default);
    }
}
