using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TaskManagementSystem.Core.DTOs.Auth;
using TaskManagementSystem.Core.Interfaces.Services;

namespace TaskManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var user = await _authService.RegisterAsync(registerDto);
            return Ok(new { message = "User registered successfully", username = user.Username });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var tokenDto = await _authService.LoginAsync(loginDto);
            return Ok(tokenDto);
        }
    }
}