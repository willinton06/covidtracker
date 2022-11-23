using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;

namespace CovidTracker.Generators.Commons;

public static class Helpers
{
    public static void AddGeneratedSource(this GeneratorExecutionContext context, string name, string source)
        => context.AddSource($"CovidTracker.Generators.{name}", SourceText.From(source, Encoding.UTF8));

    public static void AddPostInitializationSource(this GeneratorInitializationContext context, string name, string source)
        => context.RegisterForPostInitialization(i => i.AddSource($"CovidTracker.Generators.{name}", source));

    public static string GetTypeWithoutTask(this ITypeSymbol symbol)
       => ((INamedTypeSymbol)symbol).TypeArguments.FirstOrDefault()?.ToString() ?? string.Empty;

    public static bool HasTypeArguments(this ITypeSymbol symbol)
       => ((INamedTypeSymbol)symbol).TypeArguments.Any();

    public static bool TryGetStatusCode(this ITypeSymbol symbol, out string? name)
    {
        var prop = ((INamedTypeSymbol)symbol).TypeArguments.FirstOrDefault()?
            .GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(m => m.Type.Name is "HttpStatusCode");

        name = prop?.Name;

        return string.IsNullOrEmpty(name) is false;
    }

    public static bool HasBody(this MethodRequestType type)
        => type is MethodRequestType.Post or MethodRequestType.Put or MethodRequestType.Patch;
}