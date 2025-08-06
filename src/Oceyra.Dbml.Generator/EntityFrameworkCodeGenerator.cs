using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Oceyra.Dbml.Parser.Models;
using System;
using System.Text;

namespace Oceyra.Dbml.Generator;

public class EntityFrameworkCodeGenerator
{
    public string GenerateEntitiesAndExtensions(DatabaseModel database, DbContextInfo dbContextInfo)
    {
        var sb = new StringBuilder();

        // Add usings
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(dbContextInfo.Namespace))
        {
            // Generate namespace - use the same namespace as the DbContext
            sb.AppendLine($"namespace {dbContextInfo.Namespace};");
            sb.AppendLine();
        }

        // Generate enums first
        foreach (var enumModel in database.Enums)
        {
            GenerateEnum(sb, enumModel);
            sb.AppendLine();
        }

        // Generate entity classes
        foreach (var table in database.Tables)
        {
            GenerateEntityClass(sb, table, database.Relationships);
            sb.AppendLine();
        }

        // Generate DbContext extension (partial class)
        GenerateDbContextExtension(sb, database, dbContextInfo);

        return sb.ToString();
    }

    private static void GenerateDbContextExtension(StringBuilder sb, DatabaseModel database, DbContextInfo dbContextInfo)
    {
        sb.AppendLine($"public partial class {dbContextInfo.ClassName}");
        sb.AppendLine("{");

        // Generate DbSets
        foreach (var table in database.Tables)
        {
            var setName = table.Name.Dehumanize();
            var entityName = setName.Singularize();
            sb.AppendLine($"    public virtual DbSet<{entityName}> {setName} {{ get; set; }}");
        }

        sb.AppendLine();
        sb.AppendLine("    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);");
        sb.AppendLine();
        sb.AppendLine("    protected override void OnModelCreating(ModelBuilder modelBuilder)");
        sb.AppendLine("    {");
        sb.AppendLine("        base.OnModelCreating(modelBuilder);");
        sb.AppendLine();

        // Configure entities
        foreach (var table in database.Tables)
        {
            var entityName = table.Name.Dehumanize().Singularize();
            sb.AppendLine($"        modelBuilder.Entity<{entityName}>(entity =>");
            sb.AppendLine("        {");
            sb.AppendLine($"            entity.ToTable(\"{table.Name}\");");

            // Configure indexes
            if (table.Indexes.Any())
            {
                sb.AppendLine();

                foreach (var index in table.Indexes)
                {
                    GenerateIndexConfiguration(sb, index, entityName!);
                }
            }

            // Configure relationships
            var relationships = database.Relationships.Where(r => r.LeftTable == table.Name);

            foreach (var rel in relationships)
            {
                sb.AppendLine();
                GenerateRelationshipConfiguration(sb, rel, entityName!);
            }

            sb.AppendLine("        });");
            sb.AppendLine();
        }

        sb.AppendLine("        OnModelCreatingPartial(modelBuilder);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
    }

    private static void GenerateEnum(StringBuilder sb, EnumModel enumModel)
    {
        sb.AppendLine($"public enum {enumModel.Name}");
        sb.AppendLine("{");

        for (int i = 0; i < enumModel.Values.Count; i++)
        {
            var value = enumModel.Values[i];
            sb.Append($"    {value}");
            if (i < enumModel.Values.Count - 1)
            {
                sb.Append(",");
            }
            sb.AppendLine();
        }

        sb.AppendLine("}");
    }


    private static void GenerateIndexConfiguration(StringBuilder sb, IndexModel index, string currentEntity)
    {
        var indexName = index.Settings.TryGetValue("name", out string name) ? name : $"IX_{currentEntity}_{string.Join("_", index.Columns.Select(c => c.Dehumanize()))}";
        var properties = string.Join(", ", index.Columns.Select(p => $"\"{p.Dehumanize()}\""));

        if (index.IsPrimaryKey)
        {
            sb.AppendLine($"            entity.HasKey({properties})");
            sb.AppendLine($"                .HasName(\"{indexName}\");");
        }
        else
        {
            sb.AppendLine($"            entity.HasIndex({properties})");

            if (index.IsUnique)
            {
                sb.AppendLine($"                .IsUnique()");
            }

            sb.AppendLine($"                .HasDatabaseName(\"{indexName}\");");
        }
    }

    private static void GenerateRelationshipConfiguration(StringBuilder sb, RelationshipModel relationship, string currentEntity)
    {
        var (leftPropertyName, rightPropertyName) = GetRelationshipPropertyNames(relationship, relationship.LeftTable!);

        switch (relationship.RelationshipType)
        {
            case RelationshipType.OneToMany:
                sb.AppendLine($"            entity.HasMany<{relationship.RightTable.Dehumanize().Singularize()}>(\"{leftPropertyName}\")");
                sb.AppendLine($"                .WithOne(\"{rightPropertyName}\")");
                sb.AppendLine($"                .HasForeignKey(\"{string.Join("\", \"", relationship.RightColumns.Dehumanize())}\");");
                break;
            case RelationshipType.ManyToOne:
                sb.AppendLine($"            entity.HasOne<{relationship.RightTable.Dehumanize().Singularize()}>(\"{leftPropertyName}\")");
                sb.AppendLine($"                .WithMany(\"{rightPropertyName}\")");
                sb.AppendLine($"                .HasForeignKey(\"{string.Join("\", \"", relationship.LeftColumns.Dehumanize())}\");");
                break;
            case RelationshipType.OneToOne:
                sb.AppendLine($"            entity.HasOne<{relationship.RightTable.Dehumanize().Singularize()}>(\"{leftPropertyName}\")");
                sb.AppendLine($"                .WithOne(\"{rightPropertyName}\")");
                sb.AppendLine($"                .HasForeignKey<{currentEntity}>(\"{string.Join("\", \"", relationship.LeftColumns.Dehumanize())}\");");
                break;
        }
    }

    private void GenerateEntityClass(StringBuilder sb, TableModel table, List<RelationshipModel> relationships)
    {
        var className = table.Name.Dehumanize().Singularize();

        sb.AppendLine($"[Table(\"{table.Name}\")]");

        foreach (var index in table.Indexes)
        {
            var indexName = index.Settings.TryGetValue("name", out string name) ? name : $"IX_{className}_{string.Join("_", index.Columns.Select(c => c.Dehumanize()))}";
            var properties = string.Join(", ", index.Columns.Select(p => $"nameof({p.Dehumanize()})"));
            sb.AppendLine($"[Index({properties}, Name = \"{indexName}\", IsUnique = {index.IsUnique.ToString().ToLower()})]");
        }

        sb.AppendLine($"public partial class {className}");
        sb.AppendLine("{");

        // Generate constructor for navigation collections
        var manyRelationships = relationships.Where(r => (r.LeftTable == table.Name && r.RelationshipType == RelationshipType.OneToMany) ||
                                                         (r.RightTable == table.Name && r.RelationshipType == RelationshipType.ManyToOne)).ToList();

        if (manyRelationships.Any())
        {
            sb.AppendLine($"    public {className}()");
            sb.AppendLine("    {");
            foreach (var relationship in manyRelationships.Where(r => r.RelationshipType == RelationshipType.OneToMany))
            {
                var (leftPropertyName, _) = GetRelationshipPropertyNames(relationship, relationship.LeftTable!);

                sb.AppendLine($"        {leftPropertyName.Pluralize()} = new HashSet<{relationship.RightTable.Dehumanize().Singularize()}>();");
            }

            foreach (var relationship in manyRelationships.Where(r => r.RelationshipType == RelationshipType.ManyToOne))
            {
                var (rightPropertyName, _) = GetRelationshipPropertyNames(relationship, relationship.RightTable!);

                sb.AppendLine($"        {rightPropertyName.Pluralize()} = new HashSet<{relationship.LeftTable.Dehumanize().Singularize()}>();");
            }
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // Generate properties for columns
        foreach (var column in table.Columns)
        {
            GenerateProperty(sb, column);
        }

        // Generate navigation properties
        foreach (var relationship in relationships)
        {
            if (relationship.LeftTable == table.Name)
            {
                GenerateNavigationProperty(sb, relationship, true);
            }
            else if (relationship.RightTable == table.Name)
            {
                GenerateNavigationProperty(sb, relationship, false);
            }
        }

        sb.AppendLine("}");
    }

    private void GenerateProperty(StringBuilder sb, ColumnModel column)
    {
        var csharpType = MapDbmlTypeToCSharp(column.Type!, column.IsNull);
        var propertyName = column.Name.Dehumanize();

        // Add attributes
        if (column.IsPrimaryKey)
        {
            sb.AppendLine("    [Key]");
        }

        if (column.IsIncrement)
        {
            sb.AppendLine("    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
        }

        sb.AppendLine($"    [Column(\"{column.Name}\")]");

        if (column.IsNotNull && csharpType.EndsWith("?"))
        {
            csharpType = csharpType.TrimEnd('?');
        }

        if (column.IsNotNull && csharpType == "string")
        {
            sb.AppendLine("    [Required]");
        }

        sb.AppendLine($"    public {csharpType} {propertyName} {{ get; set; }}");
        sb.AppendLine();
    }

    private static void GenerateNavigationProperty(StringBuilder sb, RelationshipModel relationship, bool isFromSide)
    {
        if (isFromSide)
        {
            var (leftPropertyName, rightPropertyName) = GetRelationshipPropertyNames(relationship, isFromSide ? relationship.LeftTable! : relationship.RightTable!);
            switch (relationship.RelationshipType)
            {
                case RelationshipType.OneToMany:
                    sb.AppendLine($"    [InverseProperty(\"{rightPropertyName}\")]");
                    sb.AppendLine($"    public virtual ICollection<{relationship.RightTable.Dehumanize().Singularize()}> {leftPropertyName} {{ get; set; }}");
                    break;
                case RelationshipType.ManyToOne:
                case RelationshipType.OneToOne:
                    sb.AppendLine($"    public virtual {relationship.RightTable.Dehumanize().Singularize()} {leftPropertyName} {{ get; set; }}");
                    break;
            }
        }
        else
        {
            var (rightPropertyName, leftPropertyName) = GetRelationshipPropertyNames(relationship, isFromSide ? relationship.LeftTable! : relationship.RightTable!);
            switch (relationship.RelationshipType)
            {
                case RelationshipType.ManyToOne:
                    sb.AppendLine($"    [InverseProperty(\"{leftPropertyName}\")]");
                    sb.AppendLine($"    public virtual ICollection<{relationship.LeftTable.Dehumanize().Singularize()}> {rightPropertyName} {{ get; set; }}");
                    break;
                case RelationshipType.OneToOne:
                case RelationshipType.OneToMany:
                    sb.AppendLine($"    public virtual {relationship.LeftTable.Dehumanize().Singularize()} {rightPropertyName} {{ get; set; }}");
                    break;
            }
        }
        sb.AppendLine();
    }

    private static (string, string) GetRelationshipPropertyNames(RelationshipModel relationship, string currentTable)
    {
        bool isFromSide = relationship.LeftTable == currentTable;
        bool isCurrentMany = isFromSide ? relationship.RelationshipType == RelationshipType.ManyToOne : relationship.RelationshipType == RelationshipType.OneToMany;
        bool isOppositeMany = isFromSide ? relationship.RelationshipType == RelationshipType.OneToMany : relationship.RelationshipType == RelationshipType.ManyToOne;

        currentTable = currentTable.Dehumanize().Singularize();
        var oppositeTable = (isFromSide ? relationship.RightTable : relationship.LeftTable).Dehumanize().Singularize();

        var currentPropertyName = currentTable;
        var oppositePropertyName = oppositeTable;

        if (relationship.LeftColumns.Count == 1)
        {
            var oppositeColumns = (isFromSide != isCurrentMany ? relationship.RightColumns[0] : relationship.LeftColumns[0]).RemoveId().Dehumanize();
            var checkTable = isCurrentMany ? oppositeTable : currentTable;
            var columnName = oppositeColumns.RemoveId().Dehumanize();

            if (!string.IsNullOrWhiteSpace(columnName) && !checkTable.Equals(columnName, StringComparison.OrdinalIgnoreCase) && !columnName.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                currentPropertyName = columnName;
                oppositePropertyName = columnName;
            }
        }

        if (isCurrentMany)
        {
            currentPropertyName = currentPropertyName.Pluralize();
        }
        else
        {
            currentPropertyName = $"{currentPropertyName}Navigation";
        }

        if (isOppositeMany)
        {
            oppositePropertyName = oppositePropertyName.Pluralize();
        }
        else
        {
            oppositePropertyName = $"{oppositePropertyName}Navigation";
        }

        return (oppositePropertyName, currentPropertyName);
    }

    private string MapDbmlTypeToCSharp(string dbmlType, bool canBeNull)
    {
        var baseType = dbmlType.ToLower() switch
        {
            "int" => "int",
            "integer" => "int",
            "bigint" => "long",
            "smallint" => "short",
            "tinyint" => "byte",
            "varchar" => "string",
            "text" => "string",
            "char" => "string",
            "nvarchar" => "string",
            "nchar" => "string",
            "bit" => "bool",
            "boolean" => "bool",
            "decimal" => "decimal",
            "numeric" => "decimal",
            "money" => "decimal",
            "float" => "float",
            "real" => "float",
            "double" => "double",
            "datetime" => "DateTime",
            "datetime2" => "DateTime",
            "datetimeoffset" => "DateTimeOffset",
            "date" => "DateTime",
            "time" => "TimeSpan",
            "timestamp" => "DateTime",
            "uuid" => "Guid",
            "uniqueidentifier" => "Guid",
            "json" => "string",
            "jsonb" => "string",
            _ => "string"
        };

        // Handle array types
        if (dbmlType.EndsWith("[]"))
        {
            var elementType = MapDbmlTypeToCSharp(dbmlType.Substring(0, dbmlType.Length - 2), false);
            return $"{elementType}[]";
        }

        // Handle nullable value types
        if (canBeNull && baseType != "string" && !baseType.EndsWith("[]"))
        {
            return baseType + "?";
        }

        return baseType;
    }
}

public static class OceyraStringExtensions
{
    public static string RemoveId(this string value)
    {
        if (value.Length > 2 &&
            (value.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ||
             value.EndsWith("Fk", StringComparison.OrdinalIgnoreCase) ||
             value.EndsWith("Pk", StringComparison.OrdinalIgnoreCase)))
        {
            return value.Substring(0, value.Length - 2);
        }

        return value;
    }
}

public static class OceyraEnumerableExtensions
{
    public static IEnumerable<string> Dehumanize(this IEnumerable<string> values)
    {
        return values.Select(v => v.Dehumanize());
    }
}
