using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TechShop_API.Migrations
{
    /// <inheritdoc />
    public partial class InitialSeedLaptop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Laptops",
                columns: new[] { "Id", "Brand", "CPU", "Description", "GPU", "Image", "Name", "Price", "Resolution", "ScreenSize", "Stock", "Storage" },
                values: new object[,]
                {
                    { 1, "Acer", "intell i3", "Great Laptop", "GTX 1050ti", "https://images.unsplash.com/photo-1522199755839-a2bacb67c546?q=80&w=500&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D", "Test Laptop1", 699.99000000000001, "1280 x 720", 14.1, 22, 256 },
                    { 2, "Apple", "intell i5", "Greater Laptop", "GTX 1070ti", "https://images.unsplash.com/photo-1611186871348-b1ce696e52c9?q=80&w=500&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D", "Test Laptop2", 999.99000000000001, "1920 x 1080", 15.300000000000001, 24, 512 },
                    { 3, "Monster", "intell i9", "Greatest Laptop", "GTX 2080ti", "https://images.unsplash.com/photo-1640955014216-75201056c829?q=80&w=500&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D", "Test Laptop3", 1999.99, "2560 × 1440", 17.300000000000001, 18, 1024 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Laptops",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Laptops",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Laptops",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
