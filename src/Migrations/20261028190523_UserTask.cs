using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class UserTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserTasks_Users_CreateByUserID",
                table: "UserTasks");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "UserTasks");

            migrationBuilder.RenameColumn(
                name: "TeamID",
                table: "UserTasks",
                newName: "TeamId");

            migrationBuilder.RenameColumn(
                name: "CreateByUserID",
                table: "UserTasks",
                newName: "CreateByUserId");

            migrationBuilder.RenameColumn(
                name: "AssignedToUserID",
                table: "UserTasks",
                newName: "AssignedToUserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserTasks_CreateByUserID",
                table: "UserTasks",
                newName: "IX_UserTasks_CreateByUserId");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "UserTasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DueAt",
                table: "UserTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarkdownText",
                table: "UserTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "UserTasks",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "UserTasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_UserTasks_DueAt",
                table: "UserTasks",
                column: "DueAt");

            migrationBuilder.AddForeignKey(
                name: "FK_UserTasks_Users_CreateByUserId",
                table: "UserTasks",
                column: "CreateByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserTasks_Users_CreateByUserId",
                table: "UserTasks");

            migrationBuilder.DropIndex(
                name: "IX_UserTasks_DueAt",
                table: "UserTasks");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserTasks");

            migrationBuilder.DropColumn(
                name: "DueAt",
                table: "UserTasks");

            migrationBuilder.DropColumn(
                name: "MarkdownText",
                table: "UserTasks");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "UserTasks");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UserTasks");

            migrationBuilder.RenameColumn(
                name: "TeamId",
                table: "UserTasks",
                newName: "TeamID");

            migrationBuilder.RenameColumn(
                name: "CreateByUserId",
                table: "UserTasks",
                newName: "CreateByUserID");

            migrationBuilder.RenameColumn(
                name: "AssignedToUserId",
                table: "UserTasks",
                newName: "AssignedToUserID");

            migrationBuilder.RenameIndex(
                name: "IX_UserTasks_CreateByUserId",
                table: "UserTasks",
                newName: "IX_UserTasks_CreateByUserID");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "UserTasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_UserTasks_Users_CreateByUserID",
                table: "UserTasks",
                column: "CreateByUserID",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
