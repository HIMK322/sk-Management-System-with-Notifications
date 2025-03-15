using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagementSystem.Core.Entities;

namespace TaskManagementSystem.Core.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task<Notification> GetByIdAsync(Guid id);
        Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId);
        Task<Notification> CreateAsync(Notification notification);
        Task UpdateAsync(Notification notification);
        Task MarkAsReadAsync(Guid id);
        Task DeleteAsync(Guid id);
    }
}