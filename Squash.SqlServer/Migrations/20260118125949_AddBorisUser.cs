using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Squash.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddBorisUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Players",
                columns: new[] { "Id", "CountryId", "DateCreated", "DateUpdated", "EntitySourceId", "EsfMemberId", "ImaId", "LastOperationUserId", "Name", "PictureUrl", "RankedinId", "UserId" },
                values: new object[] { 1, 28, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, "ES793398940", null, 0, "Boris Braykov", null, null, 4 });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Address", "BadgeId", "BirthDate", "City", "CountryId", "DateCreated", "DateUpdated", "Email", "EmailNotificationsEnabled", "FirstName", "Gender", "IdentityUserId", "LastName", "LastOperationUserId", "Name", "Phone", "PlayerId", "PreferredLanguage", "PreferredSport", "StripeCustomerId", "TimeZoneId", "VerificationDate", "Verified", "Zip" },
                values: new object[] { 4, "Not Provided", null, new DateTime(2012, 12, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sofia", 28, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "boris.braikov@gmail.com", true, "Boris", "Male", "c340d87e-9f37-4d7c-8e21-65483f9931d8", "Braykov", 0, "Boris Braykov", "+359885038308", 1, "bg-BG", null, null, null, null, true, "1000" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Players",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
