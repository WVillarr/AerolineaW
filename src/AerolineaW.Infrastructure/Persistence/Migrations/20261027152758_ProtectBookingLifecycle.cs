using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AerolineaW.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProtectBookingLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_FlightId_Seat",
                table: "Bookings");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Bookings",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_FlightId_Seat",
                table: "Bookings",
                columns: new[] { "FlightId", "Seat" },
                unique: true,
                filter: "[Status] <> 'Cancelled'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_FlightId_Seat",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_FlightId_Seat",
                table: "Bookings",
                columns: new[] { "FlightId", "Seat" },
                unique: true);
        }
    }
}
