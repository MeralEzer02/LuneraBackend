using System.Threading.Tasks;
using TheSocialMediaV2.API.Enums;

namespace TheSocialMediaV2.API.Services
{
    public interface IAdminActionLogger
    {
        Task LogAsync(int adminUserId, AdminActionType actionType, string reason, int? targetUserId = null);
    }
}