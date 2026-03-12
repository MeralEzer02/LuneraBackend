using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.Domain.Entities;
using TheSocialMediaV2.Domain.Enums;

namespace TheSocialMediaV2.API.Services
{
    public class AdminActionLogger : IAdminActionLogger
    {
        private readonly AppDbContext _context;

        public AdminActionLogger(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(int adminUserId, AdminActionType actionType, string reason, int? targetUserId = null)
        {
            var log = new AdminActionLog
            {
                AdminUserId = adminUserId,
                ActionType = actionType,
                Reason = reason,
                TargetUserId = targetUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.AdminActionLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}