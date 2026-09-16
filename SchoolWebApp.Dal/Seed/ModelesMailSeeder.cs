using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Dal.Seed
{
    /// <summary>
    /// Les six courriels automatiques du menu « Automatique » — Camara, le
    /// 15/09/2026.
    ///
    /// NE RÉÉCRIT JAMAIS UNE LIGNE EXISTANTE : le nom, la description, le texte
    /// et la programmation appartiennent à l'administration dès que la ligne
    /// existe. Un semis qui les rétablirait au démarrage effacerait chaque
    /// correction faite depuis l'écran.
    ///
    /// NE RECRÉE PAS CE QU'ON A SUPPRIMÉ. Relance et Rappels, sans règle
    /// d'envoi, se suppriment depuis l'écran : ils ne sont semés qu'à la toute
    /// première fois, quand aucun courriel automatique n'existe encore. Les
    /// quatre courriels reliés à un envoi (bilans, fin d'essai, demandes d'avis)
    /// ne se suppriment pas, et sont rétablis s'ils manquent : sans eux, le code
    /// d'envoi n'aurait plus de texte à envoyer.
    ///
    /// TOUS ÉTEINTS, SAUF LES BILANS. Un courriel qui écrit à des parents ne
    /// doit partir que parce qu'un administrateur l'a relu et programmé. Les
    /// bilans partent déjà chaque semaine : les semer éteints les arrêterait
    /// au premier déploiement.
    ///
    /// LES VARIABLES `{{prenom}}`, `{{lienAvis}}`… sont remplacées pour chaque
    /// parent au moment de l'envoi. L'écran en affiche la liste à côté du
    /// message.
    /// </summary>
    public static class ModelesMailSeeder
    {
        public static async Task SeedAsync(SchoolWebAppDatabaseContext context, CancellationToken ct = default)
        {
            var maintenant = DateTime.UtcNow;

            var modeles = new[]
            {
                new ModeleMail
                {
                    Code = CodeModeleMail.Bilans,
                    Nom = "Bilans aux parents",
                    Description = "Chaque semaine, le bilan de chaque enfant : ce qu’il a travaillé, ses progrès "
                                  + "et ce qui reste fragile. Les destinataires ne changent pas : les parents "
                                  + "d’un enfant ayant une formule payante.",
                    Sujet = "Le bilan de {{prenomEnfant}} — {{dateDebut}}",
                    Titre = string.Empty,
                    Texte = string.Empty,
                    Frequence = FrequenceEnvoi.Semaine,
                    JourSemaine = 1,
                    HeureEnvoi = new TimeOnly(9, 0),
                    Actif = true,
                },
                new ModeleMail
                {
                    Code = CodeModeleMail.Relance,
                    Nom = "Relance",
                    Description = "Relancer les parents. Les règles restent à définir — plusieurs types de "
                                  + "relance sont prévus : rien n’est envoyé pour l’instant.",
                    Frequence = FrequenceEnvoi.Aucune,
                },
                new ModeleMail
                {
                    Code = CodeModeleMail.Rappels,
                    Nom = "Rappels",
                    Description = "Des rappels aux parents, sans doute sous forme de messages individuels. "
                                  + "Rien n’est envoyé pour l’instant.",
                    Frequence = FrequenceEnvoi.Aucune,
                },
                new ModeleMail
                {
                    Code = CodeModeleMail.FinEssai,
                    Nom = "Période d’essai",
                    Description = "La veille de la fin de sa période d’essai, prévient le parent et l’invite à "
                                  + "choisir une formule pour que son enfant garde son professeur.",
                    Sujet = "Votre période d’essai Mimia se termine demain",
                    Titre = "Votre essai se termine demain",
                    Texte = "Bonjour {{prenom}},\n\n"
                            + "Votre période d’essai Mimia se termine le **{{dateFinEssai}}**.\n\n"
                            + "Pour que votre enfant garde son professeur — et tout ce qu’il a déjà appris "
                            + "sur lui —, choisissez la formule qui vous convient : {{lienTarifs}}\n\n"
                            + "À bientôt,\nL’équipe Mimia",
                    Frequence = FrequenceEnvoi.Jour,
                    HeureEnvoi = new TimeOnly(10, 0),
                },
                new ModeleMail
                {
                    Code = CodeModeleMail.AvisEssai,
                    Nom = "Demande d’avis période d’essai",
                    Description = "Trois jours après son inscription, demande au parent comment se passe sa "
                                  + "découverte de Mimia, et l’invite à laisser son avis.",
                    Sujet = "Comment se passent vos premiers jours sur Mimia ?",
                    Titre = "Votre avis compte",
                    Texte = "Bonjour {{prenom}},\n\n"
                            + "Vous avez rejoint Mimia il y a quelques jours. Comment se passent les "
                            + "premières séances de votre enfant ?\n\n"
                            + "Votre retour nous aide à améliorer le professeur : ce qui fonctionne, ce qui "
                            + "manque, ce qui vous a surpris. Quelques mots suffisent : {{lienAvis}}\n\n"
                            + "Merci,\nL’équipe Mimia",
                    Frequence = FrequenceEnvoi.Jour,
                    HeureEnvoi = new TimeOnly(11, 0),
                },
                new ModeleMail
                {
                    Code = CodeModeleMail.AvisGeneral,
                    Nom = "Demande d’avis général",
                    Description = "Demande aux parents abonnés qui n’ont pas encore laissé d’avis ce qu’ils "
                                  + "pensent du site et de leur expérience, et s’ils ont des suggestions "
                                  + "d’amélioration. Au plus une fois tous les trois mois par parent.",
                    Sujet = "Votre avis sur Mimia, et vos idées pour l’améliorer",
                    Titre = "Qu’en pensez-vous ?",
                    Texte = "Bonjour {{prenom}},\n\n"
                            + "Votre enfant travaille avec Mimia depuis quelque temps. Qu’en pensez-vous : "
                            + "le site, les séances, le suivi que vous recevez ?\n\n"
                            + "Et si une chose pouvait être améliorée, laquelle ? Toutes les suggestions "
                            + "sont lues : {{lienAvis}}\n\n"
                            + "Merci pour votre aide,\nL’équipe Mimia",
                    Frequence = FrequenceEnvoi.Semaine,
                    JourSemaine = 4,
                    HeureEnvoi = new TimeOnly(11, 0),
                },
            };

            var dejaSemes = await context.ModelesMail
                .AnyAsync(m => m.Nature == NatureModeleMail.Automatique, ct);

            var existants = await context.ModelesMail
                .Where(m => m.Code != null)
                .Select(m => m.Code!)
                .ToListAsync(ct);

            var ajoutes = 0;

            foreach (var modele in modeles)
            {
                if (existants.Contains(modele.Code!)) continue;

                // Absent alors que le menu a déjà été semé : un administrateur l'a
                // supprimé. On respecte ce choix, sauf pour un courriel dont le
                // code d'envoi a besoin.
                if (dejaSemes && !CodeModeleMail.EstRelieAUnEnvoi(modele.Code)) continue;

                modele.Nature = NatureModeleMail.Automatique;
                modele.DateCreation = maintenant;
                context.ModelesMail.Add(modele);
                ajoutes++;
            }

            if (ajoutes > 0) await context.SaveChangesAsync(ct);
        }
    }
}
