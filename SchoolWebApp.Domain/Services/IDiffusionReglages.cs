using System.Threading.Channels;

namespace SchoolWebApp.Domain.Services
{
    /// <summary>
    /// ANNONCE UN CHANGEMENT DE RÉGLAGE AUX NAVIGATEURS OUVERTS — Camara, le
    /// 16/09/2026 : « quand j'active Blue Sky, ça ne change pas sur tous les
    /// ordinateurs et mobiles de manière instantanée ».
    ///
    /// L'INTERFACE EST ICI, L'IMPLÉMENTATION DANS L'API. Le dépôt de réglages
    /// vit dans la couche d'accès aux données, qui ne connaît pas l'API : sans
    /// cette interface dans le Domain, il aurait fallu annoncer depuis chaque
    /// route qui écrit un réglage — et en oublier une le jour où l'on en
    /// ajoute. Le dépôt annonce ce qu'il vient d'écrire ; personne d'autre n'a
    /// à y penser.
    ///
    /// UNE IMPLÉMENTATION MUETTE EST LÉGITIME : un service qui ne diffuse rien
    /// (un worker, un test) laisse simplement les navigateurs relire à leur
    /// rythme habituel.
    /// </summary>
    public interface IDiffusionReglages
    {
        /// <summary>
        /// Ouvre une écoute. L'identifiant rendu doit être passé à
        /// <see cref="Desabonner"/> à la fermeture de la connexion, sans quoi
        /// le canal resterait dans la liste.
        /// </summary>
        (Guid Id, ChannelReader<string> Lecteur) Abonner();

        /// <summary>Referme une écoute.</summary>
        void Desabonner(Guid id);

        /// <summary>Annonce qu'un réglage vient de changer. Ne bloque jamais.</summary>
        void Diffuser(string cle);

        /// <summary>
        /// La diffusion est-elle allumée ? Éteinte, plus aucune écoute n'est
        /// acceptée et les navigateurs relisent à leur rythme habituel.
        /// </summary>
        bool Actif { get; }

        /// <summary>
        /// Allume ou éteint la diffusion — LE COUPE-CIRCUIT, voulu par Camara
        /// le 16/09/2026 avant le lancement : « si le site explose, je ne veux
        /// pas que cette histoire de SSE ait un impact ».
        ///
        /// ÉTEINDRE FERME LES ÉCOUTES EN COURS, tout de suite. Se contenter de
        /// refuser les nouvelles viderait l'interrupteur de son sens : on
        /// l'actionne précisément quand on veut que ça cesse MAINTENANT, et les
        /// connexions déjà ouvertes sont justement celles qui inquiètent.
        ///
        /// Aucune lecture en base n'en découle : l'état vit en mémoire. Relire
        /// un drapeau dans chaque écoute serait l'accumulation qu'on cherche à
        /// éviter — à deux mille écoutes, quatre-vingts requêtes par seconde.
        /// </summary>
        void DefinirActif(bool actif);

        /// <summary>Le nombre de navigateurs à l'écoute — pour la journalisation.</summary>
        int Abonnes { get; }
    }
}
