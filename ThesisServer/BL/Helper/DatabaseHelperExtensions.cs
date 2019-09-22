using System.Net;
using System.Threading.Tasks;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Infrastructure.Middleware.Helper.Exception;

namespace ThesisServer.BL.Helper
{
    public static class DatabaseHelperExtensions
    {
        public static async Task SaveDbChangesWithSuccessCheckAsync(this VirtualNetworkDbContext dbContext)
        {
            var changedItems = await dbContext.SaveChangesAsync();

            if (changedItems == 0)
                throw new OperationFailedException(
                    message: "Unable to save database changes.",
                    statusCode: HttpStatusCode.InternalServerError,
                    webSocket: null);
        }
    }
}
