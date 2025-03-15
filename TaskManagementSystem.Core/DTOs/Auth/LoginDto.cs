using System.ComponentModel.DataAnnotations;

namespace TaskManagementSystem.Core.DTOs.Auth
{
    public class LoginDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}