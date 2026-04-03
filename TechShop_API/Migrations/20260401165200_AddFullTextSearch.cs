using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechShop_API.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SearchText",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            // Create the Full-Text Catalog
            migrationBuilder.Sql("CREATE FULLTEXT CATALOG ProductSearchCatalog AS DEFAULT;", suppressTransaction: true);

            // Create the Full-Text Index on the SearchText column
            migrationBuilder.Sql(
                @"CREATE FULLTEXT INDEX ON Products(SearchText) 
                KEY INDEX PK_Products 
                ON ProductSearchCatalog 
                WITH CHANGE_TRACKING AUTO;",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FULLTEXT INDEX ON Products;", suppressTransaction: true);
            migrationBuilder.Sql("DROP FULLTEXT CATALOG ProductSearchCatalog;", suppressTransaction: true);
            migrationBuilder.DropColumn(
                name: "SearchText",
                table: "Products");
        }
    }
}
