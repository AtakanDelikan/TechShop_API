using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services.Interfaces;
using TechShop_API.Utility;

namespace TechShop_API.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly string _secretKey;

        public AuthService(ApplicationDbContext db, IConfiguration configuration,
            UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO model)
        {
            var user = await _db.ApplicationUsers
                .FirstOrDefaultAsync(u => u.UserName.ToLower() == model.UserName.ToLower());
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                throw new UnauthorizedAccessException("Invalid username or password");

            var roles = await _userManager.GetRolesAsync(user);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email ?? user.UserName),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault() ?? SD.Role_Customer)
                }),
                Expires = DateTime.UtcNow.AddYears(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new LoginResponseDTO
            {
                Email = user.Email,
                Token = tokenHandler.WriteToken(token)
            };
        }

        public async Task<bool> RegisterAsync(RegisterRequestDTO model)
        {
            if (await _db.ApplicationUsers.AnyAsync(u => u.UserName.ToLower() == model.UserName.ToLower()))
                throw new InvalidOperationException("Username already exists");

            var newUser = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.UserName,
                NormalizedEmail = model.UserName.ToUpper(),
                Name = model.Name
            };

            var result = await _userManager.CreateAsync(newUser, model.Password);
            if (!result.Succeeded) return false;

            if (!await _roleManager.RoleExistsAsync(SD.Role_Admin))
            {
                await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
                await _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer));
            }

            var role = model.Role?.ToLower() == SD.Role_Admin.ToLower()
                ? SD.Role_Admin
                : SD.Role_Customer;

            await _userManager.AddToRoleAsync(newUser, role);
            return true;
        }

        public async Task<UserDetailsDTO> GetUserDataAsync(string userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            return new UserDetailsDTO
            {
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address
            };
        }

        public async Task<bool> UpdateUserDataAsync(string userId, UserDetailsDTO model)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            user.Name = model.Name;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;

            _db.ApplicationUsers.Update(user);
            await _db.SaveChangesAsync();

            return true;
        }
    }
}
