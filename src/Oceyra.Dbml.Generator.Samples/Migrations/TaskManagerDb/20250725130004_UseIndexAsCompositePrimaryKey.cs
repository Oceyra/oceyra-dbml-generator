using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oceyra.Dbml.Generator.Samples.Migrations.TaskManagerDb
{
    /// <inheritdoc />
    public partial class UseIndexAsCompositePrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_task_dependencies",
                table: "task_dependencies");

            migrationBuilder.DropColumn(
                name: "id",
                table: "task_dependencies");

            migrationBuilder.AddPrimaryKey(
                name: "public_index_1",
                table: "task_dependencies",
                columns: new[] { "task_id", "dependency_task_id", "project_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "public_index_1",
                table: "task_dependencies");

            migrationBuilder.AddColumn<long>(
                name: "id",
                table: "task_dependencies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_task_dependencies",
                table: "task_dependencies",
                column: "id");
        }
    }
}
