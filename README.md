# Oceyra DBML Source Generator
This generator allow to build the DbContext for entity framework core, based on a DBML from DbDiagram or ChartDb

[![Build status](https://gitea.duchaine.freeddns.org/ManufacturingTyde/oceyra-dbml-generator/actions/workflows/publish.yaml/badge.svg?branch=main&event=push)](https://gitea.duchaine.freeddns.org/ManufacturingTyde/oceyra-dbml-generator/actions/workflows/publish.yaml?query=branch%3Amain+event%3Apush)

## Usage Sample
In schema/TaskManagerDb.dbml
```dbml
Table "projects" {
  "project_id" varchar(500) [pk, not null] [ref: < "task_results"."project_id"]
  "project_name" varchar(500) [not null]
  "requirements" varchar(500)
  "task_plan" varchar(500)
  "status" varchar(500) [not null]
  "created_at" timestamp [not null]
  "updated_at" timestamp
}

Table "task_results" {
  "project_id" varchar(500) [not null]
  "task_id" varchar(500) [not null] [ref: < "task_dependencies"."dependency_task_id"]
  "name" varchar(500) [not null]
  "description" varchar(500)
  "agent_type" varchar(500) [not null]
  "priority" varchar(500)
  "estimated_hours" bigint
  "result_data" varchar(500)
  "status" varchar(500) [not null]
  "created_at" timestamp [not null]
  "updated_at" timestamp

  Indexes {
    (task_id, agent_type, project_id) [unique, name: "public_idx_task_results_task_id_agent_type"]
  }
}

Table "task_dependencies" {
  "id" bigint [pk, not null]
  "project_id" varchar(500) [not null]
  "task_id" varchar(500) [not null]
  "dependency_task_id" varchar(500) [not null]
  "created_at" timestamp
  "updated_at" timestamp

  Indexes {
    (task_id, dependency_task_id, project_id) [unique, name: "public_index_1"]
  }
}
```

In the *.csproj, flag the DBML as an ```AdditionalFiles```
```xml
  <ItemGroup>
    <AdditionalFiles Include="schema\TaskManagerDb.dbml" />
  </ItemGroup>
```

In TaskManagerDbContext.cs
```c#
namespace Oceyra.Dbml.Generator.Samples;

[DbmlSource("schema/TaskManagerDb.dbml")]
public partial class TaskManagerDbContext : DbContext { }
```
