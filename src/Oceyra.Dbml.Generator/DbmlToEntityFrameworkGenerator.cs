using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Oceyra.Dbml.Parser;

namespace Oceyra.Dbml.Generator;

[Generator]
public class DbmlToEntityFrameworkGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Create the attribute source first
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "DbmlSourceAttribute.sg.cs",
            SourceText.From("""
                using System;

                namespace Oceyra.Generator;
                
                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
                public sealed class DbmlSourceAttribute : Attribute
                {
                    public string DbmlFile { get; }
                    
                    public DbmlSourceAttribute(string dbmlFile)
                    {
                        DbmlFile = dbmlFile;
                    }
                }
                """, Encoding.UTF8)));

        // Find classes decorated with DbmlSourceAttribute
        var dbContextClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax,
                transform: static (ctx, _) => GetDbContextInfo(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        // Get DBML files
        var dbmlFiles = context.AdditionalTextsProvider
            .Where(static (file) => Path.GetExtension(file.Path).Equals(".dbml", StringComparison.OrdinalIgnoreCase))
            .Select(static  (file, cancellationToken) =>
            {
                var content = file.GetText(cancellationToken)?.ToString();
                return (file.Path, Content: content);
            });

        // Combine DbContext classes with DBML files
        var combined = dbContextClasses.Combine(dbmlFiles.Collect());

        // Register source output
        context.RegisterSourceOutput(combined, (sourceProductionContext, item) =>
        {
            var (dbContextInfo, dbmlFiles) = item;

            // Find the matching DBML file
            var matchingDbml = dbmlFiles.FirstOrDefault(f =>
                Path.GetFileName(f.Path).Equals(Path.GetFileName(dbContextInfo.DbmlFileName), StringComparison.OrdinalIgnoreCase) ||
                f.Path.EndsWith(dbContextInfo.DbmlFileName, StringComparison.OrdinalIgnoreCase));

            if (matchingDbml.Content == null)
            {
                var diagnostic = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "DBML003",
                        "DBML file not found",
                        $"DBML file '{dbContextInfo.DbmlFileName}' not found for DbContext '{dbContextInfo.ClassName}'",
                        "DbmlGenerator",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None);

                sourceProductionContext.ReportDiagnostic(diagnostic);
                return;
            }

            try
            {
                var database = DbmlParser.Parse(matchingDbml.Content);

                var codeGenerator = new EntityFrameworkCodeGenerator();
                var generatedCode = codeGenerator.GenerateEntitiesAndExtensions(database, dbContextInfo);

                // Add the generated source to the compilation
                var fileName = Path.Combine(dbContextInfo.Namespace, $"{dbContextInfo.ClassName}.g.cs");
                sourceProductionContext.AddSource(fileName, SourceText.From(generatedCode, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                var diagnostic = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "DBML001",
                        "DBML parsing error",
                        $"Error parsing DBML file {dbContextInfo.DbmlFileName}: {ex.Message}",
                        "DbmlGenerator",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None);

                sourceProductionContext.ReportDiagnostic(diagnostic);
            }
        });
    }

    private static DbContextInfo? GetDbContextInfo(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Check if class has DbmlSourceAttribute
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeName = attribute.Name.ToString();
                if (attributeName == "DbmlSource" || attributeName == "DbmlSourceAttribute")
                {
                    // Get the semantic model to resolve the attribute
                    var semanticModel = context.SemanticModel;
                    var attributeSymbol = semanticModel.GetSymbolInfo(attribute).Symbol as IMethodSymbol;

                    if (attributeSymbol?.ContainingType.Name == "DbmlSourceAttribute")
                    {
                        // Extract DBML file name from attribute argument
                        var dbmlFileName = "";
                        if (attribute.ArgumentList?.Arguments.Count > 0)
                        {
                            var firstArg = attribute.ArgumentList.Arguments[0];
                            if (firstArg.Expression is LiteralExpressionSyntax literal)
                            {
                                dbmlFileName = literal.Token.ValueText;
                            }
                        }

                        // Get namespace and class name
                        var namespaceName = GetNamespace(classDeclaration);
                        var className = classDeclaration.Identifier.ValueText;

                        return new DbContextInfo(className, namespaceName, dbmlFileName);
                    }
                }
            }
        }

        return null;
    }

    private static string GetNamespace(ClassDeclarationSyntax classDeclaration)
    {
        var namespaceDeclaration = classDeclaration.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
        if (namespaceDeclaration != null)
        {
            return namespaceDeclaration.Name.ToString();
        }

        var fileScopedNamespace = classDeclaration.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>();
        if (fileScopedNamespace != null)
        {
            return fileScopedNamespace.Name.ToString();
        }

        return string.Empty;
    }
}
