using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CovidTracker.Generators.Commons;

public static class Filtering
{
    public static bool ContainsAttribute(this GeneratorSyntaxContext context,
        Func<INamedTypeSymbol?> attribute,
        out INamedTypeSymbol? type,
        out AttributeData[]? attributes)
    {
        type = default;
        attributes = default;

        if (context.Node is not TypeDeclarationSyntax { AttributeLists.Count: > 0 } typeDeclaration) return false;

        if ((type = context.SemanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol) is null) return false;

        var attributeClass = attribute();

        attributes = type.GetAttributes().Where(a => a.AttributeClass?.ToString() == attributeClass?.ToString()).ToArray();

        return attributes.Any();
    }

    public static bool HasWithAttribute(this ImmutableArray<INamedTypeSymbol> types,
        INamedTypeSymbol? attribute,
        out IEnumerable<INamedTypeSymbol> withAttribute)
    {
        withAttribute = types.Where(t => t.GetAttributes().Any(a => a.AttributeClass?.ToString() == attribute?.ToString()));

        return withAttribute.Any();
    }

    public static bool ContainsAttribute(this GeneratorSyntaxContext context,
        INamedTypeSymbol attribute,
        out INamedTypeSymbol? type,
        out AttributeData[]? attributes)
    {
        type = default;
        attributes = default;

        if (context.Node is not TypeDeclarationSyntax { AttributeLists.Count: > 0 } typeDeclaration) return false;

        if ((type = context.SemanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol) is null) return false;

        attributes = type.GetAttributes().Where(a => a.AttributeClass?.ToString() == attribute.ToString()).ToArray();

        return attributes.Any();
    }

    public static AttributeData[] AllAttributesExcept(this GeneratorSyntaxContext context,
        INamedTypeSymbol attribute)
    {
        if (context.Node is not TypeDeclarationSyntax { AttributeLists.Count: > 0 } typeDeclaration)
            return Array.Empty<AttributeData>();

        if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration) is not INamedTypeSymbol type)
            return Array.Empty<AttributeData>();

        return type.GetAttributes().Where(a => a.AttributeClass?.ToString() != attribute.ToString()).ToArray();
    }
}
