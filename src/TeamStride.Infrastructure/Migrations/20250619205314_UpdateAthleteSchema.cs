using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamStride.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAthleteSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete all existing athlete records as part of migration strategy
            migrationBuilder.Sql("DELETE FROM AthleteProfiles");
            migrationBuilder.Sql("DELETE FROM Athletes");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "Athletes");

            migrationBuilder.DropColumn(
                name: "JerseyNumber",
                table: "Athletes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "Athletes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "Athletes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GradeLevel",
                table: "Athletes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Athletes");

            migrationBuilder.DropColumn(
                name: "GradeLevel",
                table: "Athletes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "Athletes",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "Athletes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JerseyNumber",
                table: "Athletes",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
