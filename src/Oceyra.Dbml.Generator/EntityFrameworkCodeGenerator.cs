using Humanizer;
using System.Text;
using Microsoft.CodeAnalysis;
using Oceyra.Dbml.Parser.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        foreach (var tableName in database.Tables.Select(t=> t.Name))
        {
            var entityName = tableName.Dehumanize().Singularize();
            sb.AppendLine($"        modelBuilder.Entity<{entityName}>(entity =>");
            sb.AppendLine("        {");
            sb.AppendLine($"            entity.ToTable(\"{tableName}\");");

            // Configure relationships
            var relationships = database.Relationships.Where(r => r.LeftTable == tableName || r.RightTable == tableName);
            foreach (var rel in relationships)
            {
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



    private static void GenerateRelationshipConfiguration(StringBuilder sb, RelationshipModel relationship, string currentEntity)
    {
        if (relationship.LeftTable != currentEntity)
            return;

        var relationshipEntity = relationship.RightTable.Dehumanize().Singularize();

        switch (relationship.RelationshipType)
        {
            case RelationshipType.OneToMany:
                sb.AppendLine($"            entity.HasMany<{relationshipEntity}>(\"{relationshipEntity}\")");
                sb.AppendLine($"                .WithOne(\"{currentEntity}Navigation\")");
                sb.AppendLine($"                .HasForeignKey(\"{relationship.RightColumn}\");");
                break;
            case RelationshipType.ManyToOne:
                sb.AppendLine($"            entity.HasOne<{relationshipEntity}>(\"{relationshipEntity}Navigation\")");
                sb.AppendLine($"                .WithMany(\"{currentEntity}\")");
                sb.AppendLine($"                .HasForeignKey(\"{relationship.LeftColumn}\");");
                break;
            case RelationshipType.OneToOne:
                sb.AppendLine($"            entity.HasOne<{relationshipEntity}>(\"{relationshipEntity}Navigation\")");
                sb.AppendLine($"                .WithOne(\"{currentEntity}Navigation\")");
                sb.AppendLine($"                .HasForeignKey<{currentEntity}>(\"{relationship.LeftColumn}\");");
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
        var oneToManyRels = relationships.Where(r => r.LeftTable == table.Name && r.RelationshipType == RelationshipType.OneToMany).ToList();

        if (oneToManyRels.Any())
        {
            sb.AppendLine($"    public {className}()");
            sb.AppendLine("    {");
            foreach (var rel in oneToManyRels)
            {
                var propertyName = rel.RightColumn?.Length > 2 ? rel.RightColumn.RemoveId().Dehumanize().Pluralize() : rel.RightTable.Dehumanize();
                sb.AppendLine($"        {propertyName} = new HashSet<{rel.RightTable.Dehumanize().Singularize()}>();");
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
            switch (relationship.RelationshipType)
            {
                case RelationshipType.OneToMany:
                    var rightPropertyName = relationship.RightColumn?.Length > 2 ? relationship.RightColumn.RemoveId().Dehumanize().Pluralize() : relationship.RightTable.Dehumanize();
                    sb.AppendLine($"    [InverseProperty(\"{rightPropertyName.Singularize()}Navigation\")]");
                    sb.AppendLine($"    public virtual ICollection<{relationship.RightTable.Dehumanize().Singularize()}> {rightPropertyName} {{ get; set; }}");
                    break;
                case RelationshipType.ManyToOne:
                case RelationshipType.OneToOne:
                    var leftPropertyName = relationship.LeftColumn?.Length > 2 ? relationship.LeftColumn.RemoveId().Dehumanize() : relationship.LeftTable.Dehumanize().Singularize();
                    sb.AppendLine($"    [ForeignKey(\"{relationship.LeftColumn.Dehumanize()}\")]");
                    sb.AppendLine($"    public virtual {relationship.RightTable.Dehumanize().Singularize()} {leftPropertyName}Navigation {{ get; set; }}");
                    break;
            }
        }
        else
        {
            switch (relationship.RelationshipType)
            {
                case RelationshipType.ManyToOne:
                    var leftPropertyName = relationship.LeftColumn?.Length > 2 ? relationship.LeftColumn.RemoveId().Dehumanize().Pluralize() : relationship.LeftTable.Dehumanize();
                    sb.AppendLine($"    [InverseProperty(\"{leftPropertyName.Singularize()}Navigation\")]");
                    sb.AppendLine($"    public virtual ICollection<{relationship.LeftTable.Dehumanize().Singularize()}> {leftPropertyName} {{ get; set; }}");
                    break;
                case RelationshipType.OneToOne:
                case RelationshipType.OneToMany:
                    var rightPropertyName = relationship.RightColumn?.Length > 2 ? relationship.RightColumn.RemoveId().Dehumanize() : relationship.RightTable.Dehumanize().Singularize();
                    sb.AppendLine($"    [ForeignKey(\"{relationship.RightColumn.Dehumanize()}\")]");
                    sb.AppendLine($"    public virtual {relationship.LeftTable.Dehumanize().Singularize()} {rightPropertyName}Navigation {{ get; set; }}");
                    break;
            }
        }
        sb.AppendLine();
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
