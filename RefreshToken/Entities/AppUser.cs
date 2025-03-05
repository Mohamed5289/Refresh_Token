using Microsoft.AspNetCore.Identity;
using RefreshToken.Models;

namespace RefreshToken.Entities
{
    public class AppUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public ICollection<Refresh_Token> RefreshTokens { get; set; }
    }
}
