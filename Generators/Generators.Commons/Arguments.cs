using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;

namespace CovidTracker.Generators.Commons;

public static class Arguments
{
    public static string GetArgumentsDeclaration(this ImmutableArray<IParameterSymbol> parameters)
       => string.Join(", ", parameters.Select(p => p.Name));
    
    public static string GetArgumentsAsTuples(this ImmutableArray<IParameterSymbol> parameters)
        => string.Join(", ", parameters.Select(
            parameter => $"(\"{parameter.Name}\", {parameter.Name}{(parameter.Type.IsReferenceType ? "?" : string.Empty)}.ToString())"));
}
