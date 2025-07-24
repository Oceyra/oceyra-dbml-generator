using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oceyra.Core.Generator.Tests.Helper;
using Oceyra.Dbml.Generator.Tests.Dummy;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Oceyra.Dbml.Generator.Tests;

public class DbmlToEntityFrameworkGeneratorTests
{
    public DbmlToEntityFrameworkGeneratorTests()
    {
        // Ensure the Microsoft.EntityFrameworkCore.Relational is loaded to test the constructor generation
        if ("".Contains("abc"))
        {
#pragma warning disable IDE0079 // Pragma Disable Message.
#pragma warning disable EF1001 // Internal EF Core API usage.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            EntityTypeBuilder builder = new EntityTypeBuilder<ForceLoadDll>(null);
            builder?.ToTable("DummyClassToLoadDll");
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore EF1001 // Internal EF Core API usage.
#pragma warning restore IDE0079 // Pragma Disable Message.
        }
    }

    [Fact]
    public void ConstructorGenerator_UsingField_GeneratesWorkingConstructor()
    {
        var dbml = @"Table ""projects"" {
  ""project_id"" varchar(500) [pk, not null] [ref: < ""task_results"".""project_id""]
  ""project_name"" varchar(500) [not null]
  ""requirements"" varchar(500)
  ""task_plan"" varchar(500)
  ""status"" varchar(500) [not null]
  ""created_at"" timestamp [not null]
  ""updated_at"" timestamp
}

Table ""task_results"" {
  ""project_id"" varchar(500) [not null]
  ""task_id"" varchar(500) [not null] [ref: < ""task_dependencies"".""dependency_task_id""]
  ""name"" varchar(500) [not null]
  ""description"" varchar(500)
  ""agent_type"" varchar(500) [not null]
  ""priority"" varchar(500)
  ""estimated_hours"" bigint
  ""result_data"" varchar(500)
  ""status"" varchar(500) [not null]
  ""created_at"" timestamp [not null]
  ""updated_at"" timestamp

  Indexes {
    (task_id, agent_type, project_id) [unique, name: ""public_idx_task_results_task_id_agent_type""]
  }
}

Table ""task_dependencies"" {
  ""id"" bigint [pk, not null]
  ""project_id"" varchar(500) [not null]
  ""task_id"" varchar(500) [not null]
  ""dependency_task_id"" varchar(500) [not null]
  ""created_at"" timestamp
  ""updated_at"" timestamp

  Indexes {
    (task_id, dependency_task_id, project_id) [unique, name: ""public_index_1""]
  }
}";

        var source = @"
using Microsoft.EntityFrameworkCore;
using Oceyra.Generator;

namespace Oceyra.Dbml.Tests;

[DbmlSource(""schema/customdb.dbml"")]
public partial class CustomDbContext : DbContext { }
";

        var result = SourceGeneratorVerifier.CompileAndTest<DbmlToEntityFrameworkGenerator>(
           syntaxTrees: [CSharpSyntaxTree.ParseText(source, path: "CustomDbContext.cs")],
           additionalTexts: [new InMemoryAdditionalText("some/other/path/schema/customdb.dbml", dbml)]
        );

        result.ShouldHaveNoErrors();
        result.ShouldExecuteWithin(TimeSpan.FromMilliseconds(2000));
        result.ShouldHaveGeneratorTimeWithin<DbmlToEntityFrameworkGenerator>(TimeSpan.FromMilliseconds(1000));
        result.ShouldGenerateFiles(1);

        // Test actual functionality
        var compiledType = result.GetCompiledType("Oceyra.Dbml.Tests.CustomDbContext");
        compiledType.ShouldNotBeNull();


        var compiledProjectTableType = result.GetCompiledType("Oceyra.Dbml.Tests.Project");
        compiledProjectTableType.ShouldNotBeNull();

        var propertyProjectId = compiledProjectTableType.GetProperty("ProjectId");
        propertyProjectId.ShouldNotBeNull();
    }
}
