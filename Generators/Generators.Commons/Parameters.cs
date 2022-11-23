using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;

namespace CovidTracker.Generators.Commons;

public static class Parameters
{
    public static string GetParametersDeclaration(this ImmutableArray<IParameterSymbol> parameters)
        => string.Join(", ", parameters.Select(GetDeclaration));
    
    public static string GetDeclaration(this IParameterSymbol parameter)
       => $"{parameter.Type} {parameter.Name}";
}