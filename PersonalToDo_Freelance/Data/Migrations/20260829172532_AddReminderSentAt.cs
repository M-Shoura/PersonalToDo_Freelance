using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalToDo_Freelance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderSentAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentAt",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentAt",
                table: "TaskOccurrences",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                table: "TaskOccurrences");
        }
    }
}
