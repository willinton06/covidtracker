using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using CovidTracker.Generators.Commons;

namespace CovidTracker.Generators.Registering;

class RegistrationSyntaxContextReceiver : ISyntaxContextReceiver
{
    public List<RegistrationSet> RegistrationSets { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.ContainsAttribute(() => context.GetRegisterAttribute(), out var @class, out var attributes)
            && @class is not null
            && attributes is not null)
        {
            foreach (var attribute in attributes)
            {
                RegisterType registerType = default;
                string? target = null;
                bool ignoreInterfaces = false;
                foreach (var argument in attribute.NamedArguments)
                {
                    switch (argument.Key)
                    {
                        case "Target":
                            if (argument.Value.Value is string value && string.IsNullOrWhiteSpace(value) is false)
                            {
                                target = value;
                            }
                            break;
                        case "Type":
                            Enum.TryParse(argument.Value.Value?.ToString(), out registerType);
                            break;
                        case "IgnoreInterfaces":
                            if (argument.Value.Value is bool ignore)
                            {
                                ignoreInterfaces = ignore;
                            }
                            break;
                        default:
                            break;
                    };
                }

                @class.Interfaces.HasWithAttribute(context.GetRegisterableAttribute(), out var registerables);

                RegistrationSets.Add(new(@class, target, registerType, registerables.ToArray(), ignoreInterfaces));
            }
        }
    }
}

class RegistrationSet
{
    public RegistrationSet(INamedTypeSymbol @class, string? target, RegisterType type, INamedTypeSymbol[] symbols, bool ignoreInterfaces)
    {
        Class = @class;
        Target = target;
        Type = type;
        Symbols = symbols;
        IgnoreInterfaces = ignoreInterfaces;
    }

    public bool IgnoreInterfaces { get; set; }
    public INamedTypeSymbol Class { get; set; }
    public string? Target { get; set; }
    public RegisterType Type { get; set; }
    public INamedTypeSymbol[] Symbols { get; set; }

    public void Deconstruct(out INamedTypeSymbol @class, out RegisterType type, out bool ignoreInterfaces, out INamedTypeSymbol[] symbols)
    {
        @class = Class;
        type = Type;
        ignoreInterfaces = IgnoreInterfaces;
        symbols = Symbols;
    }
}

enum RegisterType
{
    Transient,
    Scoped,
    Singleton
}