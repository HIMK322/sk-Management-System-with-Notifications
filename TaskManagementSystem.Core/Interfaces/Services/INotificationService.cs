using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagementSystem.Core.DTOs.Notification;

namespace TaskManagementSystem.Core.Interfaces.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetNotificationsAsync(Guid userId);
        Task<IEnumerable<NotificationDto>> GetUnreadNotificationsAsync(Guid userId);
        Task MarkAsReadAsync(Guid id);
        Task CreateTaskAssignmentNotificationAsync(Guid taskId, Guid userId);
    }
}