using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RefreshToken.Entities;
using RefreshToken.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RefreshToken.Services
{
    public class AuthenticationServiceCustom : IAuthenticationServiceCustom
    {
        private readonly UserManager<AppUser> userManager;
        private readonly Jwt _jwt;
        public AuthenticationServiceCustom(UserManager<AppUser> userManager, IOptions<Jwt> jwt)
        {
            this.userManager = userManager;
            _jwt = jwt.Value;
        }
        public async Task<string> CreateToken(AppUser user)
        {
            var userClaims = await userManager.GetClaimsAsync(user);
            var roles = await userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                 new Claim(JwtRegisteredClaimNames.Sub, user.UserName!),
                 new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                 new Claim(JwtRegisteredClaimNames.Email, user.Email!)
            };

            claims.AddRange(roles.Select(role => new Claim("role", role)));
            claims.AddRange(userClaims);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwt.DurationInMinute),
                SigningCredentials = creds,
                Issuer = _jwt.Issuer,
                Audience = _jwt.Audience
            };

            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(securityToken);

        }

        public async Task<AuthenticationModel> Login(LoginModel model)
        {
            if (model is null)
                return new AuthenticationModel { Message = "Login model is null!" };
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                return new AuthenticationModel { Message = "Email or password is incorrect !" };


            var user = await userManager.FindByEmailAsync(model.Email);
            if (user is null || !await userManager.CheckPasswordAsync(user, model.Password))
            {
                return new AuthenticationModel { Message = "Email or password is incorrect !" };
            }

            var authenticationModel = new AuthenticationModel();

            var token = await CreateToken(user);

            if (user.RefreshTokens.Any(u => u.IsActive))
            {
                var refreshToken = user.RefreshTokens.FirstOrDefault(r => r.IsActive);
                authenticationModel.RefreshToken = refreshToken!.Token;
                authenticationModel.RefreshTokenExpiration = refreshToken.Expires;
            }
            else
            {
                var refreshToken = GeneratorRefreshToken();
                authenticationModel.RefreshToken = refreshToken.Token;
                authenticationModel.RefreshTokenExpiration = refreshToken.Expires;
                user.RefreshTokens.Add(refreshToken);
                await userManager.UpdateAsync(user);
            }

            authenticationModel.IsAuthenticated = true;
            authenticationModel.Token = token;
            authenticationModel.Email = user.Email!;
            authenticationModel.Username = user.UserName!;
            authenticationModel.Roles = (await userManager.GetRolesAsync(user)).ToList();

            return authenticationModel;

        }

        public async Task<AuthenticationModel> Registering(RegisterModel model)
        {
            if (model is null)
                return new AuthenticationModel { Message = "Register model is null!" };

            if (await userManager.FindByEmailAsync(model.Email) is not null)
                return new AuthenticationModel { Message = "Email is already registered!" };

            if (await userManager.FindByNameAsync(model.Username) is not null)
                return new AuthenticationModel { Message = "Username is already registered!" };


            var user = new AppUser
            {
                Email = model.Email,
                UserName = model.Username,
                FirstName = model.FirstName,
                LastName = model.LastName,
                RefreshTokens = new List<Refresh_Token>()

            };

            var passwordValidator = new PasswordValidator<AppUser>();
            var validationResult = await passwordValidator.ValidateAsync(userManager, user, model.Password);

            if (!validationResult.Succeeded)
            {
                var errors = validationResult.Errors.Select(e => e.Description);
                return new AuthenticationModel { Message = string.Join(" ", errors) };
            }

            var result = await userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return new AuthenticationModel { Message = string.Join(" ", errors) };
            }

            var authenticationModel = new AuthenticationModel();

            var refreshToken = GeneratorRefreshToken();
            authenticationModel.RefreshToken = refreshToken.Token;
            authenticationModel.RefreshTokenExpiration = refreshToken.Expires;
            user.RefreshTokens.Add(refreshToken); 
            await userManager.UpdateAsync(user);

            await userManager.AddToRoleAsync(user, "User");

            authenticationModel.IsAuthenticated = true;
            authenticationModel.Email = user.Email!;
            authenticationModel.Username = user.UserName!;
            authenticationModel.Token = await CreateToken(user);
            var roles = await userManager.GetRolesAsync(user);
            authenticationModel.Roles = roles.ToList();

            return authenticationModel;
        }

        public Refresh_Token GeneratorRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return new Refresh_Token
            {
                Token = Convert.ToBase64String(randomNumber),
                Expires = DateTime.UtcNow.AddDays(10),
                Created = DateTime.UtcNow
            };

        }

        public async Task<AuthenticationModel> RefreshToken(string Token)
        {
            var authenticationModel = new AuthenticationModel();

            var user = await userManager.Users.SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == Token));

            if(user is null)
            {
                authenticationModel.Message = "Invalid token!";
                return authenticationModel;
            }

             var refreshToken = user.RefreshTokens.SingleOrDefault(t => t.Token == Token)!;

            if(!refreshToken.IsActive)
            {
                authenticationModel.Message = "Invalid token!";
                return authenticationModel;
            }

            refreshToken.Revoked = DateTime.UtcNow;

            var newRefreshToken = GeneratorRefreshToken();
            user.RefreshTokens.Add(newRefreshToken);
            await userManager.UpdateAsync(user);

            var jwtToken = await CreateToken(user);

            authenticationModel.IsAuthenticated = true;
            authenticationModel.Email = user.Email!;
            authenticationModel.Username = user.UserName!;
            authenticationModel.Token = jwtToken;
            authenticationModel.RefreshToken = newRefreshToken.Token;
            authenticationModel.RefreshTokenExpiration = newRefreshToken.Expires;
            var roles = await userManager.GetRolesAsync(user);
            authenticationModel.Roles = roles.ToList();

            return authenticationModel;
        }

        public async Task<bool> RevokeToken(string Token)
        {
            var user = await userManager.Users.SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == Token));

            if (user is null)
                return false;

            var refreshToken = user.RefreshTokens.SingleOrDefault(t => t.Token == Token)!;

            if (!refreshToken.IsActive)
                return false;

            refreshToken.Revoked = DateTime.UtcNow;

            await userManager.UpdateAsync(user);

            return true;

        }
    }
}
