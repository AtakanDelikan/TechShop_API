using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechShop_API.Migrations
{
    /// <inheritdoc />
    public partial class refactorCategoryAttribute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Max",
                table: "CategoryAttributes",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Min",
                table: "CategoryAttributes",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UniqueValues",
                table: "CategoryAttributes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Max",
                table: "CategoryAttributes");

            migrationBuilder.DropColumn(
                name: "Min",
                table: "CategoryAttributes");

            migrationBuilder.DropColumn(
                name: "UniqueValues",
                table: "CategoryAttributes");
        }
    }
}
