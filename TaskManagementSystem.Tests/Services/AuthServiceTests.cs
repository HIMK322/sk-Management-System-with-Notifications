using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using TaskManagementSystem.Core.DTOs.Auth;
using TaskManagementSystem.Core.Entities;
using TaskManagementSystem.Core.Exceptions;
using TaskManagementSystem.Core.Interfaces.Repositories;
using TaskManagementSystem.Infrastructure.Data.Repositories;
using TaskManagementSystem.Infrastructure.Services;
using TaskManagementSystem.Tests.Helpers;
using Xunit;
using FluentAssertions;

namespace TaskManagementSystem.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        
        public AuthServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(s => s.Value).Returns("your_test_jwt_key_that_is_at_least_32_chars_long");
            _configurationMock.Setup(c => c["Jwt:Key"]).Returns("your_test_jwt_key_that_is_at_least_32_chars_long");
            _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("test_issuer");
            _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("test_audience");
        }
        
        [Fact]
        public async Task RegisterAsync_WithValidData_ShouldCreateUser()
        {
            // Arrange
            var context = DbContextFactory.Create();
            var userRepository = new UserRepository(context);
            var authService = new AuthService(userRepository, _configurationMock.Object);
            
            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "Password123!",
                FirstName = "Test",
                LastName = "User"
            };
            
            // Act
            var result = await authService.RegisterAsync(registerDto);
            
            // Assert
            result.Should().NotBeNull();
            result.Username.Should().Be(registerDto.Username);
            result.Email.Should().Be(registerDto.Email);
        }
        
        [Fact]
        public async Task RegisterAsync_WithDuplicateUsername_ShouldThrowValidationException()
        {
            // Arrange
            var context = DbContextFactory.Create();
            var userRepository = new UserRepository(context);
            var authService = new AuthService(userRepository, _configurationMock.Object);
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "existinguser",
                Email = "existing@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Existing",
                LastName = "User",
                CreatedAt = DateTime.UtcNow
            };
            
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            
            var registerDto = new RegisterDto
            {
                Username = "existinguser", // Same username
                Email = "new@example.com",
                Password = "Password123!",
                FirstName = "New",
                LastName = "User"
            };
            
            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => 
                authService.RegisterAsync(registerDto));
        }
        
        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange
            var context = DbContextFactory.Create();
            var userRepository = new UserRepository(context);
            var authService = new AuthService(userRepository, _configurationMock.Object);
            
            // Create a user with known password
            var registerDto = new RegisterDto
            {
                Username = "loginuser",
                Email = "login@example.com",
                Password = "Password123!",
                FirstName = "Login",
                LastName = "User"
            };
            
            await authService.RegisterAsync(registerDto);
            
            var loginDto = new LoginDto
            {
                Username = "loginuser",
                Password = "Password123!"
            };
            
            // Act
            var result = await authService.LoginAsync(loginDto);
            
            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().NotBeNullOrEmpty();
            result.Username.Should().Be(loginDto.Username);
        }
        
        [Fact]
        public async Task LoginAsync_WithInvalidCredentials_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var context = DbContextFactory.Create();
            var userRepository = new UserRepository(context);
            var authService = new AuthService(userRepository, _configurationMock.Object);
            
            // Create a user with known password
            var registerDto = new RegisterDto
            {
                Username = "failuser",
                Email = "fail@example.com",
                Password = "Password123!",
                FirstName = "Fail",
                LastName = "User"
            };
            
            await authService.RegisterAsync(registerDto);
            
            var loginDto = new LoginDto
            {
                Username = "failuser",
                Password = "WrongPassword!"
            };
            
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(() => 
                authService.LoginAsync(loginDto));
        }
    }
}