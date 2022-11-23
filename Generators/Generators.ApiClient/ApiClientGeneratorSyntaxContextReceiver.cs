using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using CovidTracker.Generators.Commons;

namespace CovidTracker.Generators.ApiClient;

class ApiClientGeneratorSyntaxContextReceiver : ISyntaxContextReceiver
{
    public List<INamedTypeSymbol> Interfaces { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.ContainsAttribute(() => context.GetApiClientAttribute(), out var @interface, out _)
            && @interface is not null)
        {
            Interfaces.Add(@interface);
        }
    }
}
