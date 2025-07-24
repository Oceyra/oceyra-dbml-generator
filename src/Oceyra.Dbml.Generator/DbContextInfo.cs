namespace Oceyra.Dbml.Generator;

// Model classes
public class DbContextInfo(string className, string @namespace, string dbmlFileName)
{
    public string ClassName { get; } = className;
    public string Namespace { get; } = @namespace;
    public string DbmlFileName { get; } = dbmlFileName;
}
