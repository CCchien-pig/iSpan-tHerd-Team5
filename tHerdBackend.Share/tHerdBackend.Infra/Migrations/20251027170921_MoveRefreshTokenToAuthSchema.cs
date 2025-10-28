using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tHerdBackend.Infra.Migrations
{
    /// <inheritdoc />
    public partial class MoveRefreshTokenToAuthSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                newName: "RefreshTokens",
                newSchema: "auth");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                schema: "auth",
                newName: "RefreshTokens");
        }
    }
}
