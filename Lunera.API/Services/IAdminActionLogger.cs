using System.Threading.Tasks;
using Lunera.Domain.Enums;

namespace Lunera.API.Services
{
    public interface IAdminActionLogger
    {
        Task LogAsync(int adminUserId, AdminActionType actionType, string reason, int? targetUserId = null);
    }
}