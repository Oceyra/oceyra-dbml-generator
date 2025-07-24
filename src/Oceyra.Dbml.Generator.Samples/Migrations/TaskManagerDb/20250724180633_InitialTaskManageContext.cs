using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oceyra.Dbml.Generator.Samples.Migrations.TaskManagerDb
{
    /// <inheritdoc />
    public partial class InitialTaskManageContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    project_id = table.Column<string>(type: "TEXT", nullable: false),
                    project_name = table.Column<string>(type: "TEXT", nullable: false),
                    requirements = table.Column<string>(type: "TEXT", nullable: true),
                    task_plan = table.Column<string>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.project_id);
                });

            migrationBuilder.CreateTable(
                name: "task_results",
                columns: table => new
                {
                    task_id = table.Column<string>(type: "TEXT", nullable: false),
                    project_id = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    agent_type = table.Column<string>(type: "TEXT", nullable: false),
                    priority = table.Column<string>(type: "TEXT", nullable: true),
                    estimated_hours = table.Column<long>(type: "INTEGER", nullable: true),
                    result_data = table.Column<string>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_results", x => x.task_id);
                    table.ForeignKey(
                        name: "FK_task_results_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_dependencies",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    project_id = table.Column<string>(type: "TEXT", nullable: false),
                    task_id = table.Column<string>(type: "TEXT", nullable: false),
                    dependency_task_id = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_dependencies", x => x.id);
                    table.ForeignKey(
                        name: "FK_task_dependencies_task_results_dependency_task_id",
                        column: x => x.dependency_task_id,
                        principalTable: "task_results",
                        principalColumn: "task_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_task_dependencies_dependency_task_id",
                table: "task_dependencies",
                column: "dependency_task_id");

            migrationBuilder.CreateIndex(
                name: "public_index_1",
                table: "task_dependencies",
                columns: new[] { "task_id", "dependency_task_id", "project_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_task_results_project_id",
                table: "task_results",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "public_idx_task_results_task_id_agent_type",
                table: "task_results",
                columns: new[] { "task_id", "agent_type", "project_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_dependencies");

            migrationBuilder.DropTable(
                name: "task_results");

            migrationBuilder.DropTable(
                name: "projects");
        }
    }
}
