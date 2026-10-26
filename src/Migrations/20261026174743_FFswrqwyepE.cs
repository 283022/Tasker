using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class FFswrqwyepE : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Visible",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "IdName",
                table: "Teams",
                type: "text",
                nullable: false,
                defaultValue: "");
            
            migrationBuilder.Sql("""
                                 UPDATE "Users"
                                 SET "DisplayName" = replace(gen_random_uuid()::text, '-', '')
                                 WHERE "DisplayName" = '';

                                 UPDATE "Teams"
                                 SET "IdName" = gen_random_uuid()::text
                                 WHERE "IdName" = '';
                                 """);
            
            migrationBuilder.CreateIndex(
                name: "IX_Users_DisplayName",
                table: "Users",
                column: "DisplayName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_IdName",
                table: "Teams",
                column: "IdName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_DisplayName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Teams_IdName",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Visible",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IdName",
                table: "Teams");
        }
    }
}
