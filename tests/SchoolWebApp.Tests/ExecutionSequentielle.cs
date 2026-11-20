// UNE SEULE BASE DE TEST, PARTAGÉE PAR TOUTES LES CLASSES : les tests de décompte (« le nombre affiché = la liste
// d'envoi ») comptent tous les parents de la base. En parallèle, une autre classe crée ou efface ses parents fictifs
// entre les deux lectures et le test échoue au hasard (vu le 20/11/2026 : 28 attendus, 29 lus). On exécute donc les
// classes l'une après l'autre ; la suite reste rapide (quelques secondes).
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
