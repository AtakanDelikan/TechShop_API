using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
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
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
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
                {"ApiSettings:Secret", "supersecretkey1234567890"}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();
        }

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
        public async Task RegisterAsync_Throws_WhenUsernameExists()
        {
            using var context = CreateDbContext();
            context.ApplicationUsers.Add(new ApplicationUser { UserName = "john" });
            await context.SaveChangesAsync();

            var mockUserManager = MockUserManager();
            var mockRoleManager = MockRoleManager();
            var config = CreateMockConfig();

            var service = new AuthService(context, config, mockUserManager.Object, mockRoleManager.Object);

            var dto = new RegisterRequestDTO
            {
                UserName = "john",
                Password = "Test123!",
                Name = "John Doe",
                Role = SD.Role_Customer
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(dto));
        }
    }
}
