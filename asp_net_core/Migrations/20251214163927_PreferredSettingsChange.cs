using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace asp_net_core.Migrations
{
    /// <inheritdoc />
    public partial class PreferredSettingsChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "PreferredSettings",
                columns: table => new
                {
                    User = table.Column<Guid>(type: "char(36)", nullable: false),
                    LowerHeight = table.Column<int>(type: "int", nullable: false),
                    UpperHeight = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_PreferredSettings_AspNetUsers_User",
                        column: x => x.User,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PreferredSettings_User",
                table: "PreferredSettings",
                column: "User");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreferredSettings");

            migrationBuilder.AddColumn<byte>(
                name: "Role",
                table: "AspNetUsers",
                type: "tinyint unsigned",
                nullable: true);
        }
    }
}
