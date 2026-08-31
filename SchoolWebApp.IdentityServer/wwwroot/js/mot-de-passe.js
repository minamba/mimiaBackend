/*
 * L'œil qui dévoile un mot de passe.
 *
 * Appliqué À TOUS les champs de mot de passe des pages d'authentification —
 * inscription, connexion, réinitialisation — plutôt qu'ajouté vue par vue. Ce
 * sont trois formulaires aujourd'hui, et le jour où il s'en ajoute un
 * quatrième, il l'aura sans qu'on y pense.
 *
 * Amélioration progressive : sans JavaScript, les formulaires fonctionnent
 * exactement comme avant, les champs restent simplement masqués. Rien de ce
 * qui permet de créer un compte ne dépend de ce fichier.
 */
(function () {
    'use strict';

    var OEIL = 'M1.8 12S5.5 5.2 12 5.2 22.2 12 22.2 12 18.5 18.8 12 18.8 1.8 12 1.8 12z';

    function dessiner(visible) {
        // La barre oblique n'apparaît QUE lorsque le mot de passe est visible :
        // l'icône montre alors ce qu'un clic ferait — le masquer. Beaucoup
        // d'implémentations inversent les deux, et le bouton ment.
        var barre = visible ? '<path d="M4 20 20 4" />' : '';

        return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" '
            + 'stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" '
            + 'aria-hidden="true"><path d="' + OEIL + '" />'
            + '<circle cx="12" cy="12" r="3.1" />' + barre + '</svg>';
    }

    function equiper(champ) {
        if (champ.dataset.oeilPose === '1') return;
        champ.dataset.oeilPose = '1';

        var boite = document.createElement('div');
        boite.className = 'champ-mdp';

        champ.parentNode.insertBefore(boite, champ);
        boite.appendChild(champ);

        var bouton = document.createElement('button');
        // `type="button"` est indispensable : dans un formulaire, un bouton
        // sans type vaut `submit`. Sans lui, cliquer sur l'œil enverrait
        // l'inscription à moitié remplie.
        bouton.type = 'button';
        bouton.className = 'champ-mdp__oeil';
        // Hors du parcours de tabulation : entre le mot de passe et sa
        // confirmation, on veut passer au champ suivant, pas s'arrêter sur un
        // bouton d'affichage. Il reste atteignable à la souris.
        bouton.tabIndex = -1;

        function refleter(visible) {
            champ.type = visible ? 'text' : 'password';
            bouton.innerHTML = dessiner(visible);
            bouton.setAttribute('aria-pressed', visible ? 'true' : 'false');
            bouton.setAttribute(
                'aria-label',
                visible ? 'Masquer le mot de passe' : 'Afficher le mot de passe');
            bouton.title = visible ? 'Masquer' : 'Afficher';
        }

        refleter(false);

        bouton.addEventListener('click', function () {
            refleter(champ.type === 'password');
            champ.focus();
        });

        boite.appendChild(bouton);
    }

    /*
     * Les règles cochées au fil de la frappe.
     *
     * La liste est écrite par le serveur : sans ce script, elle reste lisible
     * telle quelle, et c'était déjà l'essentiel — les règles n'apparaissaient
     * qu'APRÈS l'envoi, sous forme de reproche. Ici on ne fait qu'ajouter le
     * retour immédiat, qui évite de deviner laquelle manque encore.
     *
     * Les tests portent sur les mêmes contraintes que `Program.cs` : longueur
     * 10, une majuscule, une minuscule, un chiffre. Les changer d'un côté sans
     * l'autre ferait mentir la liste — c'est le seul risque de ce duplicata,
     * et il est assumé : l'alternative serait de faire descendre la politique
     * du serveur jusqu'à la vue pour quatre booléens.
     */
    var TESTS = {
        longueur: function (v) { return v.length >= 10; },
        majuscule: function (v) { return /[A-ZÀ-Þ]/.test(v); },
        minuscule: function (v) { return /[a-zß-ÿ]/.test(v); },
        chiffre: function (v) { return /[0-9]/.test(v); }
    };

    function equiperRegles(champ) {
        var liste = document.getElementById('mdp-regles');
        if (!liste) return;

        var lignes = liste.querySelectorAll('[data-regle]');

        function verifier() {
            var valeur = champ.value;

            for (var i = 0; i < lignes.length; i++) {
                var nom = lignes[i].getAttribute('data-regle');
                var ok = TESTS[nom] ? TESTS[nom](valeur) : false;

                lignes[i].classList.toggle('regles-mdp__ok', ok);
                // Rien n'est marqué en ROUGE tant que le champ est vide : une
                // liste d'échecs affichée avant d'avoir tapé le premier
                // caractère accueille mal, et n'apprend rien.
                lignes[i].classList.toggle('regles-mdp__ko', !ok && valeur.length > 0);
            }
        }

        champ.addEventListener('input', verifier);
        verifier();
    }

    /*
     * Le message du navigateur, écrit en nos mots.
     *
     * `required` seul suffit à bloquer l'envoi — c'est lui qui fait le travail,
     * et il le fait même sans JavaScript. Mais le navigateur affiche alors sa
     * propre phrase, « Veuillez cocher cette case si vous souhaitez
     * continuer », qui ne dit pas POURQUOI elle est obligatoire. Ici on ne
     * change que la phrase.
     *
     * LE MESSAGE EST POSÉ D'AVANCE, PAS AU MOMENT DU REFUS.
     *
     * La tentation est de l'écrire dans un gestionnaire `invalid` — ça ne
     * marche pas : le navigateur a déjà calculé la phrase à afficher quand cet
     * événement part, et la bulle du même envoi montre encore la sienne. Il
     * faut que la validité personnalisée soit en place AVANT l'envoi.
     *
     * On la remet donc à jour à chaque changement, et une fois au chargement —
     * sans quoi un formulaire renvoyé par le serveur, case déjà cochée,
     * resterait bloqué sur un message qui n'a plus lieu d'être.
     */
    function equiperMessages() {
        var champs = document.querySelectorAll('[data-message-requis]');

        for (var i = 0; i < champs.length; i++) {
            (function (champ) {
                var message = champ.getAttribute('data-message-requis');

                function refleter() {
                    var manquant = champ.type === 'checkbox' ? !champ.checked : !champ.value;
                    champ.setCustomValidity(manquant ? message : '');
                }

                champ.addEventListener('change', refleter);
                champ.addEventListener('input', refleter);
                refleter();
            })(champs[i]);
        }
    }

    /*
     * Les deux mots de passe comparés AVANT l'envoi.
     *
     * Même défaut que la case à cocher : une faute de frappe dans la
     * confirmation faisait repartir le formulaire, et les deux champs
     * revenaient vides. Tout était à retaper à cause d'un caractère.
     *
     * On ne bloque QUE sur la concordance, jamais sur les règles de
     * complexité. Les deux listes de règles — celle-ci et celle de
     * `Program.cs` — sont un duplicata assumé, et si elles divergeaient un
     * jour, bloquer ici refuserait un mot de passe que le serveur aurait
     * accepté. La concordance, elle, ne peut pas diverger : deux chaînes sont
     * égales ou elles ne le sont pas.
     */
    function equiperConcordance() {
        var mdp = document.querySelector('[data-regles="mdp"]');
        var confirmation = document.querySelector('[data-confirmation="mdp"]');
        if (!mdp || !confirmation) return;

        function comparer() {
            confirmation.setCustomValidity(
                confirmation.value && confirmation.value !== mdp.value
                    ? 'Les deux mots de passe ne correspondent pas.'
                    : '');
        }

        mdp.addEventListener('input', comparer);
        confirmation.addEventListener('input', comparer);
    }

    function equiperTout() {
        var champs = document.querySelectorAll('input[type="password"]');
        for (var i = 0; i < champs.length; i++) equiper(champs[i]);

        var aRegles = document.querySelector('[data-regles="mdp"]');
        if (aRegles) equiperRegles(aRegles);

        equiperMessages();
        equiperConcordance();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', equiperTout);
    } else {
        equiperTout();
    }
})();
