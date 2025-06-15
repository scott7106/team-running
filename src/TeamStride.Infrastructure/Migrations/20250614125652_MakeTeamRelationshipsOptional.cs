using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamStride.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeTeamRelationshipsOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TeamId",
                table: "UserTeams",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "TeamRegistrationWindows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxRegistrations = table.Column<int>(type: "int", nullable: false),
                    RegistrationPasscode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamRegistrationWindows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamRegistrationWindows_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodeOfConductAccepted = table.Column<bool>(type: "bit", nullable: false),
                    CodeOfConductAcceptedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TeamRegistrationWindowId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamRegistrations_TeamRegistrationWindows_TeamRegistrationWindowId",
                        column: x => x.TeamRegistrationWindowId,
                        principalTable: "TeamRegistrationWindows",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeamRegistrations_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamRegistrations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AthleteRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Birthdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GradeLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AthleteRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AthleteRegistrations_TeamRegistrations_RegistrationId",
                        column: x => x.RegistrationId,
                        principalTable: "TeamRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AthleteRegistrations_RegistrationId",
                table: "AthleteRegistrations",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamRegistrations_TeamId",
                table: "TeamRegistrations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamRegistrations_TeamRegistrationWindowId",
                table: "TeamRegistrations",
                column: "TeamRegistrationWindowId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamRegistrations_UserId",
                table: "TeamRegistrations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamRegistrationWindows_TeamId",
                table: "TeamRegistrationWindows",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AthleteRegistrations");

            migrationBuilder.DropTable(
                name: "TeamRegistrations");

            migrationBuilder.DropTable(
                name: "TeamRegistrationWindows");

            migrationBuilder.AlterColumn<Guid>(
                name: "TeamId",
                table: "UserTeams",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }
    }
}
