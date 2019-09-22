using System.Threading.Tasks;
using ThesisServer.Data.Repository.Db;

namespace ThesisServer.BL.Services
{
    public interface INetworkService
    {
        Task<NetworkEntity> CreateNetwork(string networkName, string passWord);
        Task AddUserToNetwork(NetworkEntity networkEntity, UserEntity user, string givenPassword);
        Task<bool> IsUserConnectedToThisNetwork(UserEntity user, NetworkEntity network);
    }
}
