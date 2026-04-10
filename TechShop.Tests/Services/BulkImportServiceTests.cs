using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Text;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Services;
using TechShop_API.Utility;

namespace TechShop_API.Tests.Services
{
    public class BulkImportControllerTests
    {
        // --- Setup Helper ---
        // Creates a fresh, isolated In-Memory database for every single test
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
                .Options;

            return new ApplicationDbContext(options);
        }

        // Helper to convert a string (CSV data) into a Stream for the service
        private Stream GetCsvStream(string csvContent)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(csvContent);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        // ==========================================
        // IMPORT CATEGORIES TESTS
        // ==========================================

        [Fact]
        public async Task ImportCategories_HappyPath_SavesCategories()
        {
            var db = GetDbContext();
            var service = new BulkImportService(db);
            var csv = """
              Name,Description,Parent
              Computers,All computers,
              Laptops,Portable computers,Computers
              """;

            var result = await service.ImportCategoriesAsync(GetCsvStream(csv));

            Assert.True(result.IsSuccess);
            Assert.Equal(2, await db.Categories.CountAsync());

            var laptopCategory = await db.Categories.FirstOrDefaultAsync(c => c.Name == "Laptops");
            Assert.NotNull(laptopCategory.ParentCategory);
            Assert.Equal("Computers", laptopCategory.ParentCategory.Name);
        }

        // ==========================================
        // IMPORT CATEGORY ATTRIBUTES TESTS
        // ==========================================

        [Fact]
        public async Task ImportCategoryAttributes_ValidCsv_SavesAttributes()
        {
            var db = GetDbContext();
            db.Categories.Add(new Category { Id = 1, Name = "Laptops" });
            await db.SaveChangesAsync();

            var service = new BulkImportService(db);
            var csv = """
                CategoryName,AttributeName,DataType
                Laptops,RAM,integer
                Laptops,Brand,string
                """;

            var result = await service.ImportCategoryAttributesAsync(GetCsvStream(csv));

            Assert.True(result.IsSuccess);
            Assert.Equal(2, await db.CategoryAttributes.CountAsync());
            Assert.Contains(db.CategoryAttributes, a => a.AttributeName == "RAM" && a.DataType == SD.DataTypeEnum.Integer);
        }

        [Fact]
        public async Task ImportCategoryAttributes_CategoryNotFound_ReturnsErrorAndSkips()
        {
            var db = GetDbContext();
            var service = new BulkImportService(db);
            var csv = """
                CategoryName,AttributeName,DataType
                NonExistentCategory,RAM,integer
                """;

            var result = await service.ImportCategoryAttributesAsync(GetCsvStream(csv));

            Assert.True(result.IsSuccess); // The file processed, but logged an error
            Assert.Single(result.ErrorMessages);
            Assert.Contains("not found", result.ErrorMessages[0]);
            Assert.Empty(db.CategoryAttributes); // Nothing saved
        }

        [Fact]
        public async Task ImportCategoryAttributes_InvalidDataType_ThrowsException()
        {
            var db = GetDbContext();
            db.Categories.Add(new Category { Id = 1, Name = "Laptops" });
            await db.SaveChangesAsync();

            var service = new BulkImportService(db);
            var csv = """
                CategoryName,AttributeName,DataType
                Laptops,RAM,invalid_type
                """;

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ImportCategoryAttributesAsync(GetCsvStream(csv)));

            Assert.Contains("Unknown data type", ex.Message);
        }

        // ==========================================
        // IMPORT PRODUCTS TESTS
        // ==========================================

        [Fact]
        public async Task ImportProducts_ValidData_SavesProductsWithImagesAndAttributes()
        {
            var db = GetDbContext();
            var category = new Category { Id = 1, Name = "Cases" };
            db.Categories.Add(category);

            db.CategoryAttributes.Add(new CategoryAttribute { Id = 1, CategoryId = 1, AttributeName = "Material", DataType = SD.DataTypeEnum.String, UniqueValues = new List<string>() });
            db.CategoryAttributes.Add(new CategoryAttribute { Id = 2, CategoryId = 1, AttributeName = "RGB", DataType = SD.DataTypeEnum.Boolean });
            await db.SaveChangesAsync();

            var service = new BulkImportService(db);

            // Includes all data types, missing fields test (Price missing -> 0), and image splitting
            var csv = """
                Category,Name,Description,Price,Images,Material,RGB
                Cases,NZXT H510,A nice case,99.99,image1.png|image2.png,Steel,Yes
                """;

            var result = await service.ImportProductsAsync(GetCsvStream(csv));

            Assert.True(result.IsSuccess);
            Assert.Equal(1, await db.Products.CountAsync());

            var savedProduct = await db.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductAttributes)
                .FirstAsync();

            Assert.Equal("NZXT H510", savedProduct.Name);
            Assert.Equal(99.99m, savedProduct.Price);

            // Check Images
            Assert.Equal(2, savedProduct.ProductImages.Count);
            Assert.Equal("image1.png", savedProduct.ProductImages.First(i => i.DisplayOrder == 1).Url);

            // Check Attributes
            Assert.Equal(2, savedProduct.ProductAttributes.Count);
            Assert.Equal("Steel", savedProduct.ProductAttributes.First(pa => pa.CategoryAttributeId == 1).String);
            Assert.True(savedProduct.ProductAttributes.First(pa => pa.CategoryAttributeId == 2).Boolean);
        }

        [Fact]
        public async Task ImportProducts_EmptyFile_ReturnsSuccessButDoesNothing()
        {
            var db = GetDbContext();
            var service = new BulkImportService(db);
            var csv = """
                Category,Name,Price
                """; // Just headers, no data
                

            var result = await service.ImportProductsAsync(GetCsvStream(csv));

            Assert.True(result.IsSuccess);
            Assert.Empty(db.Products);
        }

        [Fact]
        public async Task ImportProducts_CategoryNotFound_ReturnsError()
        {
            var db = GetDbContext(); // Empty DB, no categories
            var service = new BulkImportService(db);
            var csv = """
                Category,Name,Price
                FakeCategory,Test,10
                """;

            var result = await service.ImportProductsAsync(GetCsvStream(csv));

            Assert.False(result.IsSuccess);
            Assert.Contains("Category not found", result.ErrorMessages[0]);
            Assert.Empty(db.Products);
        }

        [Fact]
        public async Task ImportProducts_UpdatesMinMaxAndUniqueValues()
        {
            var db = GetDbContext();
            var category = new Category { Id = 1, Name = "Phones" };
            db.Categories.Add(category);

            // Add attributes that need tracking
            db.CategoryAttributes.Add(new CategoryAttribute { Id = 1, CategoryId = 1, AttributeName = "RAM", DataType = SD.DataTypeEnum.Integer, UniqueValues = new List<string>() });
            db.CategoryAttributes.Add(new CategoryAttribute { Id = 2, CategoryId = 1, AttributeName = "Weight", DataType = SD.DataTypeEnum.Decimal, UniqueValues = new List<string>() });
            await db.SaveChangesAsync();

            var service = new BulkImportService(db);

            // Import 3 products to test if Min/Max scale correctly
            var csv = """
                Category,Name,RAM,Weight
                Phones,Phone A,8,150.5
                Phones,Phone B,4,200.0
                Phones,Phone C,16,100.2
                """;

            await service.ImportProductsAsync(GetCsvStream(csv));

            var ramAttribute = await db.CategoryAttributes.FirstAsync(ca => ca.AttributeName == "RAM");
            var weightAttribute = await db.CategoryAttributes.FirstAsync(ca => ca.AttributeName == "Weight");

            // RAM Min/Max check (Integer)
            Assert.Equal(4, ramAttribute.Min);
            Assert.Equal(16, ramAttribute.Max);

            // Weight Min/Max check (Decimal)
            Assert.Equal(100.2, weightAttribute.Min);
            Assert.Equal(200.0, weightAttribute.Max);
        }

        [Fact]
        public async Task ImportProducts_MalformedRow_SafelyDefaultsValues()
        {
            var db = GetDbContext();
            db.Categories.Add(new Category { Id = 1, Name = "Cases" });
            await db.SaveChangesAsync();

            var service = new BulkImportService(db);

            // Missing Name, Price is text instead of decimal
            var csv = """
                Category,Description,Price
                Cases,Nice Case,InvalidPriceString
                """;

            await service.ImportProductsAsync(GetCsvStream(csv));

            var savedProduct = await db.Products.FirstAsync();

            // Expect fallback values mapped by CreateBaseProduct
            Assert.Equal("Unknown", savedProduct.Name);
            Assert.Equal(0m, savedProduct.Price); // decimal.TryParse failed, defaulted to 0
            Assert.Equal("Nice Case", savedProduct.Description);
        }
    }
}