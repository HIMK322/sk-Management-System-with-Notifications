namespace TaskManagementSystem.Core.DTOs.Auth
{
    public class TokenDto
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
        public string Username { get; set; }
    }
}