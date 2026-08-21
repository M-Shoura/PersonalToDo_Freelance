using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalToDo_Freelance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOriginalOccurrenceDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskOccurrences_TodoTaskId_RecurrenceRuleId_OccurrenceDate",
                table: "TaskOccurrences");

            migrationBuilder.AddColumn<DateTime>(
                name: "OriginalOccurrenceDate",
                table: "TaskOccurrences",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE TaskOccurrences SET OriginalOccurrenceDate = OccurrenceDate WHERE RecurrenceRuleId IS NOT NULL AND OriginalOccurrenceDate IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaskOccurrences_TodoTaskId_RecurrenceRuleId_OriginalOccurrenceDate",
                table: "TaskOccurrences",
                columns: new[] { "TodoTaskId", "RecurrenceRuleId", "OriginalOccurrenceDate" },
                unique: true,
                filter: "[RecurrenceRuleId] IS NOT NULL AND [OriginalOccurrenceDate] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskOccurrences_TodoTaskId_RecurrenceRuleId_OriginalOccurrenceDate",
                table: "TaskOccurrences");

            migrationBuilder.DropColumn(
                name: "OriginalOccurrenceDate",
                table: "TaskOccurrences");

            migrationBuilder.CreateIndex(
                name: "IX_TaskOccurrences_TodoTaskId_RecurrenceRuleId_OccurrenceDate",
                table: "TaskOccurrences",
                columns: new[] { "TodoTaskId", "RecurrenceRuleId", "OccurrenceDate" },
                unique: true,
                filter: "[RecurrenceRuleId] IS NOT NULL");
        }
    }
}
