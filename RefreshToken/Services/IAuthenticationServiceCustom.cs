using RefreshToken.Entities;
using RefreshToken.Models;

namespace RefreshToken.Services
{
    public interface IAuthenticationServiceCustom
    {
        Task<AuthenticationModel> Registering(RegisterModel model);
        Task<AuthenticationModel> Login(LoginModel model);
        Task<string> CreateToken(AppUser user);
        Task<AuthenticationModel> RefreshToken(string refreshToken);
        Task<bool> RevokeToken(string refreshToken);
    }
}
