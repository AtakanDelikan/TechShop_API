using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services;

namespace TechShop.Tests.Services
{
    public class CommentServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;

        public CommentServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            _mockUserManager = MockUserManager();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        }

        private ClaimsPrincipal CreateUserPrincipal(string userId)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            return new ClaimsPrincipal(new ClaimsIdentity(claims));
        }

        [Fact]
        public async Task GetCommentsByProductAsync_ReturnsExpectedComments()
        {
            var product = new Product { Id = 1, Name = "TestProduct" };
            var user = new ApplicationUser { Id = "u1", Name = "Alice" };
            _context.Products.Add(product);
            _context.Users.Add(user);
            _context.Comments.AddRange(
                new Comment { ProductId = 1, ApplicationUserId = "u1", Content = "Good", Rating = 4, User = user, CreatedAt = DateTime.UtcNow.AddMinutes(-1) },
                new Comment { ProductId = 1, ApplicationUserId = "u1", Content = "Nice", Rating = 5, User = user, CreatedAt = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            var service = new CommentService(_context, _mockUserManager.Object);
            var result = await service.GetCommentsByProductAsync(1);

            Assert.Equal(2, result.Count);
            Assert.All(result, c => Assert.Equal("Alice", c.UserName));
        }

        [Fact]
        public async Task CreateCommentAsync_ThrowsUnauthorized_WhenUserNotFound()
        {
            var service = new CommentService(_context, _mockUserManager.Object);
            var dto = new CommentCreateDTO { ProductId = 1, Content = "Hello", Rating = 5 };
            var userPrincipal = new ClaimsPrincipal();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateCommentAsync(dto, userPrincipal));
        }

        [Fact]
        public async Task CreateCommentAsync_AddsComment_AndUpdatesProductRating()
        {
            var product = new Product { Id = 1, Name = "Laptop", Rating = 0 };
            var user = new ApplicationUser { Id = "user1", Name = "Alice" };
            _context.Products.Add(product);
            _context.Users.Add(user);

            _context.Comments.Add(new Comment
            {
                ProductId = 1,
                ApplicationUserId = "user1",
                Rating = 3,
                Content = "Okay",
                CreatedAt = DateTime.UtcNow.AddMinutes(-1)
            });
            await _context.SaveChangesAsync();

            var userPrincipal = CreateUserPrincipal("user1");
            var dto = new CommentCreateDTO { ProductId = 1, Content = "Great product!", Rating = 5 };
            var service = new CommentService(_context, _mockUserManager.Object);

            await service.CreateCommentAsync(dto, userPrincipal);

            var updatedProduct = await _context.Products.FindAsync(1);
            var comments = await _context.Comments.Where(c => c.ProductId == 1).OrderBy(c => c.CreatedAt).ToListAsync();

            Assert.Equal(2, comments.Count);
            Assert.Equal("Okay", comments[0].Content);
            Assert.Equal(3, comments[0].Rating);
            Assert.Equal("Great product!", comments[1].Content);
            Assert.Equal(5, comments[1].Rating);

            double expectedAverage = (3 + 5) / 2.0;
            Assert.InRange(updatedProduct.Rating, expectedAverage - 0.0001, expectedAverage + 0.0001);
        }

        [Fact]
        public async Task DeleteCommentAsync_AllowsOwnerToDeleteComment()
        {
            var owner = new ApplicationUser { Id = "user1", Name = "Alice" };
            var product = new Product { Id = 1, Name = "Laptop", Rating = 5 };
            var comment = new Comment { Id = 10, ProductId = 1, ApplicationUserId = "user1", Content = "Great product!", Rating = 5, CreatedAt = DateTime.UtcNow };
            _context.Users.Add(owner);
            _context.Products.Add(product);
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(owner);
            var service = new CommentService(_context, _mockUserManager.Object);

            var userPrincipal = CreateUserPrincipal("user1");

            await service.DeleteCommentAsync(10, userPrincipal);

            var deletedComment = await _context.Comments.FindAsync(10);
            Assert.Null(deletedComment);

            var updatedProduct = await _context.Products.FindAsync(1);
            Assert.Equal(0, updatedProduct.Rating);
        }

        [Fact]
        public async Task DeleteCommentAsync_Throws_WhenUserNotOwner()
        {
            var owner = new ApplicationUser { Id = "user1", Name = "Owner" };
            var otherUser = new ApplicationUser { Id = "user2", Name = "Stranger" };
            var product = new Product { Id = 1, Name = "Laptop", Rating = 5 };
            var comment = new Comment { Id = 20, ProductId = 1, ApplicationUserId = "user1", Content = "Secret", Rating = 5, CreatedAt = DateTime.UtcNow };
            _context.Users.AddRange(owner, otherUser);
            _context.Products.Add(product);
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(otherUser);
            var service = new CommentService(_context, _mockUserManager.Object);

            var userPrincipal = CreateUserPrincipal("user2");

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteCommentAsync(20, userPrincipal));
        }
    }
}
