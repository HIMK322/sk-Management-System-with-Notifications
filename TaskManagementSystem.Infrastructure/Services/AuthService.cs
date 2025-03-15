using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using BC = BCrypt.Net.BCrypt;
using TaskManagementSystem.Core.DTOs.Auth;
using TaskManagementSystem.Core.Entities;
using TaskManagementSystem.Core.Exceptions;
using TaskManagementSystem.Core.Interfaces.Repositories;
using TaskManagementSystem.Core.Interfaces.Services;

namespace TaskManagementSystem.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public async Task<TokenDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByUsernameAsync(loginDto.Username);
            
            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedException("Invalid username or password");
            }

            var token = GenerateJwtToken(user);
            
            return new TokenDto
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresIn = 3600, // 1 hour
                Username = user.Username
            };
        }

        public async Task<User> RegisterAsync(RegisterDto registerDto)
        {
            var existingUser = await _userRepository.GetByUsernameAsync(registerDto.Username);
            if (existingUser != null)
            {
                throw new ValidationException("Username already exists");
            }

            existingUser = await _userRepository.GetByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                throw new ValidationException("Email already exists");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = HashPassword(registerDto.Password),
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                CreatedAt = DateTime.UtcNow
            };

            return await _userRepository.CreateAsync(user);
        }

        public async Task<User> GetCurrentUserAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                throw new NotFoundException("User not found");
            }
            return user;
        }

        private string HashPassword(string password)
        {
            return BC.HashPassword(password);
        }

        private bool VerifyPassword(string password, string hash)
        {
            return BC.Verify(password, hash);
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}