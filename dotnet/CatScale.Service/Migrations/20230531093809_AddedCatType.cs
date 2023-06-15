using CatScale.Domain.Model;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatScale.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddedCatType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Cats",
                type: "integer",
                nullable: false,
                defaultValue: CatType.Active);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Cats");
        }
    }
}
