using System.Threading.Tasks;
using TaskManagementSystem.Core.DTOs.Auth;
using TaskManagementSystem.Core.Entities;

namespace TaskManagementSystem.Core.Interfaces.Services
{
    public interface IAuthService
    {
        Task<TokenDto> LoginAsync(LoginDto loginDto);
        Task<User> RegisterAsync(RegisterDto registerDto);
        Task<User> GetCurrentUserAsync(string username);
    }
}
