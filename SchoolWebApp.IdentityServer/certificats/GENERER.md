# Certificats de signature des jetons

La procédure complète est dans **`PROCEDURE.html`**, à côté de ce fichier.
Ouvrez-le dans un navigateur — et faites `Ctrl + P` → « Enregistrer au format
PDF » si vous préférez l'avoir sous la main hors du projet.

Elle couvre : ce que sont ces certificats (et ce qu'ils ne sont pas), comment
les générer, les vérifier, les transférer sur le serveur, ce qui est déjà
configuré, le renouvellement dans dix ans, et les erreurs courantes.

Un seul document plutôt que deux : deux procédures pour la même manipulation
finissent toujours par diverger, et c'est celle qu'on ne lit pas qui reste à
jour.

## En deux lignes, si vous savez déjà

Générer `signature.pfx` et `chiffrement.pfx` dans ce dossier, avec le mot de
passe inscrit dans `appsettings.Production.json` (clé `Certificats:MotDePasse`),
puis les déposer au même endroit sur le serveur. Le montage Docker et la
configuration sont déjà en place.
