using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;

namespace SchoolWebApp.Dal.Seed
{
    /// <summary>
    /// LE PREMIER BANC DES SONS — le 04/11/2026 : les mots courants du CP et les syllabes, passés
    /// dans le vrai jugement avec 4 voix de synthèse. N'insère que ce qui manque : un mot testé
    /// depuis garde son résultat. Les autres se testent à leur première apparition.
    /// </summary>
    public static class MotsVerifiesSeeder
    {
        private const int Total = 4;

        private static readonly (string Mot, int Justes)[] Banc =
        [
            ("abeille", 3), ("air", 4), ("ami", 4), ("âne", 3), ("araignée", 3), ("arbre", 3), ("auto", 1), ("ba", 4),
            ("bain", 0), ("ballon", 4), ("banc", 1), ("bateau", 4), ("bé", 3), ("beau", 3), ("bébé", 2), ("bête", 2),
            ("beurre", 2), ("bi", 3), ("bille", 0), ("blanc", 3), ("blé", 4), ("bleu", 4), ("bo", 2), ("bol", 4),
            ("bon", 3), ("bonbon", 3), ("bou", 3), ("bouche", 4), ("boule", 4), ("brun", 2), ("bu", 1), ("café", 4),
            ("cha", 4), ("chaise", 2), ("chapeau", 4), ("char", 3), ("chat", 4), ("chaud", 4), ("ché", 3), ("cheval", 4),
            ("chi", 4), ("chien", 3), ("cho", 2), ("chou", 3), ("chu", 4), ("cinq", 4), ("clef", 4), ("cœur", 4),
            ("col", 4), ("cour", 4), ("da", 4), ("dé", 4), ("dent", 3), ("di", 4), ("dix", 4), ("do", 4),
            ("dou", 4), ("du", 4), ("dur", 4), ("eau", 4), ("enfant", 3), ("épée", 1), ("été", 4), ("fa", 4),
            ("faim", 0), ("famille", 2), ("fé", 4), ("fée", 4), ("fer", 4), ("fête", 2), ("feu", 3), ("fi", 2),
            ("fille", 3), ("fin", 0), ("fleur", 4), ("fo", 3), ("fou", 3), ("four", 4), ("frère", 3), ("fruit", 2),
            ("fu", 0), ("fusée", 3), ("ga", 2), ("gain", 2), ("gare", 3), ("gâteau", 2), ("genou", 3), ("girafe", 3),
            ("go", 3), ("gomme", 4), ("gou", 2), ("grain", 2), ("grand", 3), ("gu", 3), ("guitare", 4), ("huit", 4),
            ("image", 2), ("ja", 3), ("jardin", 3), ("jé", 3), ("jeu", 3), ("ji", 3), ("jo", 2), ("jou", 3),
            ("jour", 3), ("ju", 3), ("jupe", 3), ("la", 4), ("lac", 4), ("lait", 4), ("lampe", 2), ("lapin", 3),
            ("lé", 4), ("lent", 3), ("li", 4), ("ligne", 3), ("lin", 0), ("lion", 3), ("lit", 4), ("livre", 4),
            ("lo", 1), ("loi", 4), ("long", 2), ("lou", 3), ("loup", 4), ("lourd", 4), ("lu", 2), ("lundi", 1),
            ("lune", 2), ("ma", 4), ("main", 0), ("maison", 3), ("maman", 2), ("matin", 2), ("mé", 4),
            ("melon", 2), ("mer", 4), ("mère", 4), ("mi", 4), ("mo", 3), ("moi", 4), ("mon", 2), ("montagne", 4),
            ("moto", 3), ("mou", 3), ("mouche", 2), ("moulin", 0), ("mouton", 3), ("mu", 1), ("mur", 4), ("na", 4),
            ("nain", 0), ("né", 4), ("neige", 4), ("neuf", 4), ("nez", 4), ("ni", 4), ("niche", 3), ("nid", 4),
            ("no", 4), ("nou", 2), ("nu", 2), ("nuit", 3), ("oiseau", 3), ("orange", 2), ("oreille", 3), ("oui", 4),
            ("ours", 4), ("pa", 1), ("page", 2), ("pain", 2), ("pan", 3), ("papa", 4), ("parfum", 2), ("pé", 4),
            ("peau", 3), ("peigne", 4), ("père", 2), ("peu", 1), ("peur", 3), ("pi", 3), ("pie", 3), ("pied", 4),
            ("pluie", 4), ("po", 3), ("poche", 1), ("poisson", 2), ("pomme", 4), ("pont", 2), ("porte", 4), ("pou", 2),
            ("poule", 3), ("pu", 2), ("pur", 2), ("quatre", 3), ("ra", 4), ("rat", 4), ("ré", 4), ("reine", 3),
            ("ri", 4), ("riz", 4), ("ro", 3), ("robe", 3), ("robot", 3), ("roche", 4), ("roi", 4), ("rond", 2),
            ("rou", 4), ("roue", 4), ("route", 4), ("ru", 2), ("sa", 4), ("sable", 3), ("sac", 4), ("sapin", 1),
            ("savon", 3), ("sé", 3), ("seau", 2), ("sept", 4), ("si", 4), ("singe", 0), ("six", 4), ("so", 4),
            ("sœur", 4), ("sol", 4), ("soleil", 3), ("son", 3), ("sou", 3), ("soupe", 3), ("souris", 4), ("su", 4),
            ("sur", 4), ("ta", 4), ("table", 4), ("té", 3), ("tête", 4), ("thé", 4), ("ti", 4), ("tigre", 3),
            ("to", 4), ("toi", 4), ("ton", 4), ("tortue", 4), ("tou", 3), ("tour", 4), ("train", 1), ("tu", 4),
            ("tulipe", 3), ("un", 1), ("va", 4), ("vache", 4), ("vé", 4), ("vélo", 3), ("vent", 3), ("ver", 4),
            ("vi", 0), ("vie", 4), ("vin", 1), ("vo", 2), ("vol", 4), ("vou", 4), ("vu", 3), ("za", 3),
            ("zé", 3), ("zèbre", 3), ("zéro", 4), ("zi", 4), ("zo", 3), ("zou", 3), ("zu", 1),
            // 05/11/2026 : les sons seuls, par lesquels un exercice commence, et les familles pauvres.
            ("ain", 4), ("am", 4), ("an", 4), ("au", 4), ("eu", 4), ("i", 4), ("im", 4), ("in", 4),
            ("moins", 4), ("o", 4), ("oi", 4), ("om", 4), ("ou", 4), ("rein", 4), ("è", 4), ("é", 4),
            ("a", 3), ("ai", 3), ("ein", 3), ("u", 3), ("certain", 2), ("demain", 2), ("e", 2), ("en", 2),
            ("on", 2), ("plein", 2), ("poing", 2), ("prince", 2), ("voisin", 2), ("besoin", 1), ("copain", 1), ("dessin", 1),
            ("lointain", 1), ("lutin", 1), ("magasin", 1), ("parrain", 1), ("peinture", 1), ("pointu", 1), ("poussin", 1), ("quinze", 1),
            ("requin", 1), ("simple", 1), ("timbre", 1), ("vingt", 1), ("ceinture", 0), ("chemin", 0), ("cousin", 0), ("coussin", 0),
            ("dauphin", 0), ("dindon", 0), ("frein", 0), ("gamin", 0), ("humain", 0), ("linge", 0), ("marin", 0), ("peintre", 0),
            ("pince", 0), ("pingouin", 0), ("printemps", 0), ("raisin", 0), ("terrain", 0), ("témoin", 0), ("vilain", 0), ("écrivain", 0),
            ("oin", 4), ("coin", 4), ("loin", 3), ("point", 1), ("foin", 1),
        ];

        public static async Task SeedAsync(SchoolWebAppDatabaseContext context, CancellationToken ct = default)
        {
            // LA LISTE EST MONTRÉE AU PROFESSEUR DEPUIS LE 05/11/2026 : il écrit ces mots au tableau.
            // « ane » restait sans accent, et « âne » n'aurait jamais été reconnu ; « mama » n'est pas un mot.
            // Le mot est la clé : l'ancienne ligne part, le banc ci-dessous remet la bonne orthographe.
            string[] retires = ["ane", "coeur", "soeur", "mama"];
            var lignes = await context.MotsVerifies.Where(m => m.Langue == "fr" && retires.Contains(m.Mot)).ToListAsync(ct);
            context.MotsVerifies.RemoveRange(lignes.Where(l => retires.Contains(l.Mot, StringComparer.Ordinal)));
            await context.SaveChangesAsync(ct);

            var connus = await context.MotsVerifies.AsNoTracking().Where(m => m.Langue == "fr").Select(m => m.Mot).ToListAsync(ct);
            var deja = connus.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var date = new DateTime(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc);

            context.MotsVerifies.AddRange(Banc
                .Where(b => !deja.Contains(b.Mot))
                .Select(b => new MotVerifie { Mot = b.Mot, Justes = b.Justes, Total = Total, DateTest = date }));

            // LA LISTE FERMÉE DE L'ANGLAIS : voir MotsVerifiesAnglais.
            var connusAnglais = await context.MotsVerifies.AsNoTracking().Where(m => m.Langue == "en").Select(m => m.Mot).ToListAsync(ct);
            var dejaAnglais = connusAnglais.ToHashSet(StringComparer.OrdinalIgnoreCase);

            context.MotsVerifies.AddRange(MotsVerifiesAnglais.Banc
                .Where(b => !dejaAnglais.Contains(b.Mot))
                .Select(b => new MotVerifie { Mot = b.Mot, Langue = "en", Justes = b.Justes, Total = Total, DateTest = date }));

            await context.SaveChangesAsync(ct);
        }
    }
}
