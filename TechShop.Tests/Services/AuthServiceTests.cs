using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services;
using TechShop_API.Utility;
using Xunit;

namespace TechShop.Tests.Services
{
    public class AuthServiceTests
    {
        private ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Ensures a fresh DB per test
                .Options;
            return new ApplicationDbContext(options);
        }

        private Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);
        }

        private Mock<RoleManager<IdentityRole>> MockRoleManager()
        {
            var store = new Mock<IRoleStore<IdentityRole>>();
            return new Mock<RoleManager<IdentityRole>>(
                store.Object, null, null, null, null);
        }

        private IConfiguration CreateMockConfig()
        {
            var inMemorySettings = new Dictionary<string, string> {
                // Key MUST be at least 32 characters for HMAC-SHA256
                {"ApiSettings:Secret", "supersecretkeythatisatleast32byteslong123!"}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();
        }

        // Helper to generate a validly signed JWT for RefreshToken testing
        private string GenerateTestJwtToken(string userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("supersecretkeythatisatleast32byteslong123!");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", userId) }),
                NotBefore = DateTime.UtcNow.AddMinutes(-20),
                Expires = DateTime.UtcNow.AddMinutes(-10), // Purposely expired
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }

        // --- LOGIN TESTS ---

        [Fact]
        public async Task LoginAsync_ThrowsUnauthorized_WhenUserNotFound()
        {
            using var context = CreateDbContext();
            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var config = CreateMockConfig();

            var service = new AuthService(context, config, mockUserManager.Object, mockRoleManager.Object);
            var dto = new LoginRequestDTO { UserName = "ghost", Password = "1234" };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(dto));
        }

        [Fact]
        public async Task LoginAsync_ThrowsUnauthorized_WhenPasswordIsIncorrect()
        {
            using var context = CreateDbContext();
            context.ApplicationUsers.Add(new ApplicationUser
            {   UserName = "john",
                Name = "John Doe"
            });
            await context.SaveChangesAsync();

            var mockUserManager = MockUserManager();
            // Simulate bad password
            mockUserManager.Setup(u => u.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var service = new AuthService(context, CreateMockConfig(), mockUserManager.Object, MockRoleManager().Object);
            var dto = new LoginRequestDTO { UserName = "john", Password = "WrongPassword!" };

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(dto));
            Assert.Equal("Invalid username or password", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_ReturnsTokens_WhenCredentialsAreValid()
        {
            using var context = CreateDbContext();
            var testUser = new ApplicationUser { Id = "user1", UserName = "john", Name = "John Doe", Email = "john@test.com" };
            context.ApplicationUsers.Add(testUser);
            await context.SaveChangesAsync();

            var mockUserManager = MockUserManager();
            mockUserManager.Setup(u => u.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(true);
            mockUserManager.Setup(u => u.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { SD.Role_Customer });
            mockUserManager.Setup(u => u.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

            var service = new AuthService(context, CreateMockConfig(), mockUserManager.Object, MockRoleManager().Object);
            var dto = new LoginRequestDTO { UserName = "john", Password = "CorrectPassword!" };

            var result = await service.LoginAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("john@test.com", result.Email);
            Assert.False(string.IsNullOrEmpty(result.Token));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));

            // Verify DB was updated with new refresh token
            var dbUser = await context.ApplicationUsers.FindAsync("user1");
            Assert.NotNull(dbUser.RefreshToken);
            Assert.True(dbUser.RefreshTokenExpiryTime > DateTime.UtcNow);
        }

        // --- REFRESH TOKEN TESTS ---

        [Fact]
        public async Task RefreshAccessTokenAsync_ThrowsUnauthorized_WhenRefreshTokenExpired()
        {
            using var context = CreateDbContext();
            var testUser = new ApplicationUser
            {
                Id = "user1",
                Name = "john",
                RefreshToken = "valid_token_string",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1) // Expired yesterday
            };
            context.ApplicationUsers.Add(testUser);
            await context.SaveChangesAsync();

            var service = new AuthService(context, CreateMockConfig(), MockUserManager().Object, MockRoleManager().Object);

            var dto = new TokenRequestDTO
            {
                AccessToken = GenerateTestJwtToken("user1"),
                RefreshToken = "valid_token_string"
            };

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RefreshAccessTokenAsync(dto));
            Assert.Equal("Invalid or expired refresh token", exception.Message);
        }

        // --- REVOKE TESTS ---

        [Fact]
        public async Task RevokeTokenAsync_ReturnsTrue_AndWipesTokens_WhenUserExists()
        {
            using var context = CreateDbContext();
            var testUser = new ApplicationUser
            {
                Id = "user1",
                Name = "john",
                RefreshToken = "active_token",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(5)
            };
            context.ApplicationUsers.Add(testUser);
            await context.SaveChangesAsync();

            var mockUserManager = MockUserManager();
            mockUserManager.Setup(u => u.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

            var service = new AuthService(context, CreateMockConfig(), mockUserManager.Object, MockRoleManager().Object);

            var result = await service.RevokeTokenAsync("user1");

            Assert.True(result);
            var dbUser = await context.ApplicationUsers.FindAsync("user1");
            Assert.Null(dbUser.RefreshToken);
            Assert.Equal(DateTime.MinValue, dbUser.RefreshTokenExpiryTime);
        }

        // --- REGISTRATION TESTS ---

        [Fact]
        public async Task RegisterAsync_Throws_WhenUsernameExists()
        {
            using var context = CreateDbContext();
            context.ApplicationUsers.Add(new ApplicationUser { UserName = "john", Name = "John Doe" });
            await context.SaveChangesAsync();

            var service = new AuthService(context, CreateMockConfig(), MockUserManager().Object, MockRoleManager().Object);
            var dto = new RegisterRequestDTO { UserName = "john", Password = "Test123!", Name = "John Doe", Role = SD.Role_Customer };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(dto));
        }

        [Fact]
        public async Task RegisterAsync_ReturnsFalse_WhenCreateAsyncFails()
        {
            using var context = CreateDbContext();
            var mockUserManager = MockUserManager();

            // Simulate Identity framework rejecting the password
            mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

            var service = new AuthService(context, CreateMockConfig(), mockUserManager.Object, MockRoleManager().Object);
            var dto = new RegisterRequestDTO { UserName = "newuser", Password = "123", Name = "New User" };

            var result = await service.RegisterAsync(dto);

            Assert.False(result);
        }

        // --- USER DATA TESTS ---

        [Fact]
        public async Task GetUserDataAsync_ReturnsData_WhenUserExists()
        {
            using var context = CreateDbContext();
            context.Users.Add(new ApplicationUser
            {
                Id = "user1",
                Name = "Alice",
                Email = "alice@test.com",
                PhoneNumber = "555-1234",
                Address = "123 Main St"
            });
            await context.SaveChangesAsync();

            var service = new AuthService(context, CreateMockConfig(), MockUserManager().Object, MockRoleManager().Object);

            var result = await service.GetUserDataAsync("user1");

            Assert.NotNull(result);
            Assert.Equal("Alice", result.Name);
            Assert.Equal("alice@test.com", result.Email);
            Assert.Equal("123 Main St", result.Address);
        }

        [Fact]
        public async Task UpdateUserDataAsync_UpdatesDatabase_WhenUserExists()
        {
            using var context = CreateDbContext();
            var user = new ApplicationUser { Id = "user1", Name = "Old Name" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = new AuthService(context, CreateMockConfig(), MockUserManager().Object, MockRoleManager().Object);

            var dto = new UserDetailsDTO
            {
                Name = "New Name",
                Email = "new@test.com",
                PhoneNumber = "111",
                Address = "New Address"
            };

            var result = await service.UpdateUserDataAsync("user1", dto);

            Assert.True(result);
            var dbUser = await context.Users.FindAsync("user1");
            Assert.Equal("New Name", dbUser.Name);
            Assert.Equal("new@test.com", dbUser.Email);
        }
    }
}