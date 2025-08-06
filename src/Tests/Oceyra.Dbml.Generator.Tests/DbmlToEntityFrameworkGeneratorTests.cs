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


        var compiledProjectTableType = result.GetCompiledType("Oceyra.Dbml.Tests.Project")
            .ShouldNotBeNull();

        compiledProjectTableType.GetProperty("ProjectId")
            .ShouldNotBeNull();

        var compiledTaskResultTableType = result.GetCompiledType("Oceyra.Dbml.Tests.TaskResult")
            .ShouldNotBeNull();

        compiledProjectTableType.GetProperty("TaskResults")
            .ShouldNotBeNull()
            .PropertyType.ShouldBe(typeof(ICollection<>).MakeGenericType(compiledTaskResultTableType));

        compiledTaskResultTableType.GetProperty("ProjectNavigation")
            .ShouldNotBeNull()
            .PropertyType.ShouldBe(compiledProjectTableType);
    }

    [Fact]
    public void ConstructorGenerator_UsingConstructor_GeneratesWorkingConstructor()
    {
        var dbml = @"
Table ""projects"" {
  ""project_id"" varchar(500) [pk, not null]
  ""project_name"" varchar(500) [not null]
  ""requirements"" varchar(500)
  ""task_plan"" varchar(500)
  ""status"" varchar(500) [not null]
  ""created_at"" timestamp [not null]
  ""updated_at"" timestamp
}

Table ""task_results"" {
  ""project_id"" varchar(500) [not null] [ref: > ""projects"".""project_id""]
  ""task_id"" varchar(500) [pk, not null]
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
  ""project_id"" varchar(500) [not null]
  ""task_id"" varchar(500) [not null]
  ""dependency_task_id"" varchar(500) [not null] [ref: > ""task_results"".""task_id""]
  ""created_at"" timestamp
  ""updated_at"" timestamp

  Indexes {
    (task_id, dependency_task_id, project_id) [pk, unique, name: ""public_index_1""]
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


        var compiledProjectTableType = result.GetCompiledType("Oceyra.Dbml.Tests.Project")
            .ShouldNotBeNull();

        compiledProjectTableType.GetProperty("ProjectId")
            .ShouldNotBeNull();

        var compiledTaskResultTableType = result.GetCompiledType("Oceyra.Dbml.Tests.TaskResult")
            .ShouldNotBeNull();

        compiledProjectTableType.GetProperty("TaskResults")
            .ShouldNotBeNull()
            .PropertyType.ShouldBe(typeof(ICollection<>).MakeGenericType(compiledTaskResultTableType));

        compiledTaskResultTableType.GetProperty("ProjectNavigation")
            .ShouldNotBeNull()
            .PropertyType.ShouldBe(compiledProjectTableType);
    }

    [Fact]
    public void ConstructorGenerator_UsingConstructor_GeneratesWorkingConstructor2()
    {
        var dbml = @"
// Use DBML to define your database structure
// Docs: https://dbml.dbdiagram.io/docs

Table follows {
  id integer [primary key]
  following_user_id integer
  followed_user_id integer
  created_at timestamp
}

Table users {
  id integer [primary key]
  username varchar
  role varchar
  created_at timestamp
}

Table posts {
  id integer [primary key]
  title varchar
  body text [note: 'Content of the post']
  user_id integer [not null]
  status varchar
  created_at timestamp
}

Ref user_posts: posts.user_id > users.id // many-to-one

Ref: users.id < follows.following_user_id

Ref: users.id < follows.followed_user_id";

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
    }

    [Fact]
    public void ConstructorGenerator_UsingConstructor_GeneratesWorkingConstructor3()
    {
        var dbml = @"
Table merchants {
  id bigint [not null]
  country_code int [not null]

  Indexes {
    (id, country_code) [pk, unique]
  }
}

Table merchant_periods {
  id bigint [pk, not null]
  merchant_id bigint [not null]
  country_code int [not null]
}

Ref: merchant_periods.(merchant_id, country_code) > merchants.(id, country_code)";

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
    }

    [Fact]
    public void ConstructorGenerator_UsingIdAsForeignKey_GeneratesNameBasedOnTablename()
    {
        var dbml = @"
            Table ""datasource_providers"" {
              ""id"" integer [pk, not null, ref: < ""datasources"".""datasource_provider_id""]
              ""name"" varchar(500)
              ""description"" varchar(500)
              ""nuget_package_id"" integer
              ""use_method"" varchar(500) [not null]
              ""created_at"" timestamp [not null]
              ""created_by"" varchar(500) [not null]
              ""updated_at"" timestamp [not null]
              ""updated_by"" varchar(500) [not null]

              Indexes {
                name [name: ""idx_name""]
              }
            }

            Table ""datasources"" {
              ""id"" integer [pk, not null]
              ""name"" integer [unique, not null]
              ""description"" varchar(500)
              ""enabled"" bool
              ""datasource_provider_id"" integer [not null]
              ""connection"" varchar(500)
              ""username"" varchar(500)
              ""password"" varchar(500)
              ""created_at"" timestamp [not null]
              ""created_by"" varchar(500) [not null]
              ""updated_at"" timestamp [not null]
              ""updated_by"" varchar(500) [not null]

              Indexes {
                name [unique, name: ""idx_name""]
              }
            }

            Table ""nuget_packages"" {
              ""id"" integer [pk, not null, ref: < ""datasource_providers"".""nuget_package_id""]
              ""package_id"" varchar(500) [unique, not null]
              ""package_version"" varchar(500) [not null]
              ""created_at"" timestamp [not null]
              ""created_by"" varchar(500) [not null]
              ""updated_at"" timestamp [not null]
              ""updated_by"" varchar(500) [not null]
            }

            Table ""roles"" {
              ""id"" integer [pk, not null, ref: < ""user_roles"".""role_id""]
              ""name"" varchar(500)
              ""created_at"" timestamp [not null]
              ""created_by"" varchar(500) [not null]
              ""updated_at"" timestamp [not null]
              ""updated_by"" varchar(500) [not null]
            }

            Table ""users"" {
              ""id"" integer [pk, not null, ref: < ""user_roles"".""user_id""]
              ""username"" varchar(500) [unique, not null]
              ""password"" varchar(500)
              ""firstname"" varchar(500)
              ""lastname"" varchar(500)
              ""created_at"" timestamp [not null]
              ""created_by"" varchar(500) [not null]
              ""updated_at"" timestamp [not null]
              ""updated_by"" varchar(500) [not null]
              ""enabled"" bool [not null]
            }

            Table ""user_roles"" {
              ""id"" integer [pk, not null]
              ""user_id"" integer [not null]
              ""role_id"" integer [not null]
            }";

        var source = @"
using Microsoft.EntityFrameworkCore;
using Oceyra.Generator;

namespace Oceyra.Dbml.Tests;

[DbmlSource(""schema/customdb.dbml"")]
public partial class CustomDbContext : DbContext { }";

        var result = SourceGeneratorVerifier.CompileAndTest<DbmlToEntityFrameworkGenerator>(
           syntaxTrees: [CSharpSyntaxTree.ParseText(source, path: "CustomDbContext.cs")],
           additionalTexts: [new InMemoryAdditionalText("some/other/path/schema/customdb.dbml", dbml)]
        );

        result.ShouldHaveNoErrors();
        result.ShouldExecuteWithin(TimeSpan.FromMilliseconds(2000));
        result.ShouldHaveGeneratorTimeWithin<DbmlToEntityFrameworkGenerator>(TimeSpan.FromMilliseconds(1000));
        result.ShouldGenerateFiles(1);

        var compiledUserTableType = result.GetCompiledType("Oceyra.Dbml.Tests.User")
            .ShouldNotBeNull();

        compiledUserTableType.GetProperties().ShouldContain(p => p.Name.Equals("UserRoles", StringComparison.OrdinalIgnoreCase));
    }
}
