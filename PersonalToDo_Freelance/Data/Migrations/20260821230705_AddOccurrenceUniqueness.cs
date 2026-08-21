using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalToDo_Freelance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOccurrenceUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TaskOccurrences_TodoTaskId_RecurrenceRuleId_OccurrenceDate",
                table: "TaskOccurrences",
                columns: new[] { "TodoTaskId", "RecurrenceRuleId", "OccurrenceDate" },
                unique: true,
                filter: "[RecurrenceRuleId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskOccurrences_TodoTaskId_RecurrenceRuleId_OccurrenceDate",
                table: "TaskOccurrences");
        }
    }
}
