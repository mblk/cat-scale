using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatScale.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddedTemperatureHumidityPressureToScaleEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Humidity",
                table: "ScaleEvents",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Pressure",
                table: "ScaleEvents",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Temperature",
                table: "ScaleEvents",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Humidity",
                table: "ScaleEvents");

            migrationBuilder.DropColumn(
                name: "Pressure",
                table: "ScaleEvents");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "ScaleEvents");
        }
    }
}
