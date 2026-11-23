using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Dal.Stockage
{
    /// <summary>
    /// Les documents des examens blancs : le stockage des cours de contrôle, dans son sous-dossier
    /// <c>examens-blancs/</c> (voir le constructeur protégé de <see cref="StockageCoursControlesDisque"/>).
    /// </summary>
    public class StockageExamensBlancsDisque : StockageCoursControlesDisque, IStockageExamensBlancs
    {
        public const string SousDossier = "examens-blancs";

        public StockageExamensBlancsDisque(IConfiguration configuration, ILogger<StockageCoursControlesDisque> logger)
            : base(configuration, logger, SousDossier)
        {
        }
    }
}
