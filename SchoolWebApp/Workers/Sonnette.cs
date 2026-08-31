namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// Le mécanisme commun aux réveils de workers : on sonne, le worker repart.
    ///
    /// POURQUOI UNE BASE PARTAGÉE
    /// --------------------------
    /// Il y en a deux — les planches, les documents — et il y en aura d'autres.
    /// Recopier un sémaphore de quinze lignes à chaque fois, c'est recopier
    /// aussi le piège : `Release` lève quand le maximum est atteint, et un
    /// oubli de ce `try` fait tomber le contrôleur qui sonne, pas le worker.
    /// Une seule copie du piège, corrigée une seule fois.
    ///
    /// COMMENT
    /// -------
    /// Un sémaphore initialement vide. Le worker l'attend à la place de son
    /// délai : il repart dès qu'on sonne. Aucune file, aucun message, aucun
    /// état à nettoyer.
    ///
    /// Le maximum vaut UN volontairement. Trois envois rapprochés ne demandent
    /// pas trois tours : le worker vide sa file avant de se rendormir, donc la
    /// sonnerie en attente fait le travail des trois.
    /// </summary>
    public abstract class Sonnette : IDisposable
    {
        private readonly SemaphoreSlim _signal = new(0, 1);

        /// <summary>Demande au worker de repartir tout de suite.</summary>
        public void Sonner()
        {
            // `Release` lève quand le maximum est déjà atteint — c'est le cas
            // normal de deux envois rapprochés, pas une anomalie : la sonnerie
            // en attente fera le travail des deux.
            try { _signal.Release(); }
            catch (SemaphoreFullException) { }
        }

        /// <summary>
        /// Attend la sonnerie, ou l'intervalle, selon ce qui vient en premier.
        /// </summary>
        public Task AttendreAsync(TimeSpan intervalle, CancellationToken ct) =>
            _signal.WaitAsync(intervalle, ct);

        /// <summary>
        /// Attend la sonnerie, SANS LIMITE DE TEMPS.
        ///
        /// POURQUOI CETTE SECONDE FORME EXISTE
        /// -----------------------------------
        /// Une ronde périodique n'a de sens que si du travail peut apparaître
        /// sans qu'on le sache. Ce n'est le cas d'aucun de ces workers : une
        /// planche n'arrive que par un import, un document que par un envoi, et
        /// les deux sonnent.
        ///
        /// Elle a coûté cher : une pièce qui échoue reste dans la file, et la
        /// ronde la reprend indéfiniment. Le tableau périodique a été relu sept
        /// mille fois en dix jours pour rendre à chaque fois une réponse vide.
        /// Sans ronde, un échec attend le prochain envoi — et l'incident se
        /// borne de lui-même.
        /// </summary>
        public Task AttendreAsync(CancellationToken ct) =>
            _signal.WaitAsync(ct);

        public void Dispose()
        {
            _signal.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
