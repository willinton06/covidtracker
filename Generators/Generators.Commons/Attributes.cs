using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace CovidTracker.Generators.Commons;

public static class Attributes
{
    public static INamedTypeSymbol? GetControllerAttribute(this GeneratorSyntaxContext context)
        => context.SemanticModel.Compilation.GetTypeByMetadataName("CovidTracker.Generators.Controllers.GenerateControllerAttribute");

    public static INamedTypeSymbol? GetRegisterAttribute(this GeneratorSyntaxContext context)
        => context.SemanticModel.Compilation.GetTypeByMetadataName("CovidTracker.Generators.Register.RegisterAttribute");

    public static INamedTypeSymbol? GetRegisterableAttribute(this GeneratorSyntaxContext context)
        => context.SemanticModel.Compilation.GetTypeByMetadataName("CovidTracker.Generators.Register.RegisterableAttribute");

    public static INamedTypeSymbol? GetApiClientAttribute(this GeneratorSyntaxContext context)
        => context.SemanticModel.Compilation.GetTypeByMetadataName("CovidTracker.Generators.ApiClient.GenerateApiClientAttribute");

    public static string GetDeclaration(this IEnumerable<AttributeData> attributes)
    {
        string attributeGroup = string.Join(",", attributes);

        if (string.IsNullOrEmpty(attributeGroup) is false)
            return $"[{attributeGroup}]";
        else return string.Empty;
    }

    public static bool TryGetFirstConstructorParameterValue(this AttributeData attribute, out string value)
    {
        if (attribute.ConstructorArguments.FirstOrDefault() is TypedConstant typedConstant
            && typedConstant.Value?.ToString() is string output
            && string.IsNullOrWhiteSpace(output) is false)
        {
            value = output;
            return true;
        }
        else
        {
            value = string.Empty;
            return false;
        }
    }
}
