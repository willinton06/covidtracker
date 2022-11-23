using CovidTracker.Generators.Commons;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace CovidTracker.Generators.Controllerss;

class ControllerSyntaxContextReceiver : ISyntaxContextReceiver
{
    public List<ControllerDefinition> ControllerDefinitions { get; } = new();
    public IEnumerable<PartialControllerDefinition> PartialControllerDefinitions => ControllerDefinitions.SelectMany(t => t.GetPartialControllerDefinition());

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.GetControllerAttribute() is INamedTypeSymbol controllerAttribute &&
            context.ContainsAttribute(controllerAttribute, out var @class, out _)
            && @class is not null
            && @class.Interfaces.HasWithAttribute(context.GetRegisterableAttribute(), out var registerables))
        {
            ControllerDefinitions.Add(new(
                @class,
                registerables.ToArray(),
                context.AllAttributesExcept(controllerAttribute),
                @class.GetMembers().OfType<IMethodSymbol>()
                    .Where(m => m.DeclaredAccessibility is Accessibility.Public)
                    .Select(t => new MethodWithAttributes(t.Name, t.GetAttributes().ToArray())).ToArray()));
        }
    }
}

public class ControllerDefinition
{
    public ControllerDefinition(INamedTypeSymbol @class,
        INamedTypeSymbol[] registerables,
        AttributeData[] attributes,
        MethodWithAttributes[] methodAttributes)
    {
        Class = @class;
        Registerables = registerables;
        Attributes = attributes;
        MethodAttributes = methodAttributes;
    }

    public INamedTypeSymbol Class { get; set; }
    public INamedTypeSymbol[] Registerables { get; set; }
    public AttributeData[] Attributes { get; set; }
    public MethodWithAttributes[] MethodAttributes { get; set; }

    public IEnumerable<PartialControllerDefinition> GetPartialControllerDefinition()
    {
        foreach (var registerable in Registerables)
        {
            yield return new(registerable, Attributes, MethodAttributes);
        }
    }

    public void Deconstruct(out INamedTypeSymbol @class, out INamedTypeSymbol[] registerables)
    {
        @class = Class;
        registerables = Registerables;
    }
}

public class PartialControllerDefinition
{
    public PartialControllerDefinition(INamedTypeSymbol @interface,
        AttributeData[] attributes,
        MethodWithAttributes[] methodAttributes)
    {
        Interface = @interface;
        Attributes = attributes;
        MethodAttributes = methodAttributes;
    }

    public INamedTypeSymbol Interface { get; set; }
    public AttributeData[] Attributes { get; set; }
    public MethodWithAttributes[] MethodAttributes { get; set; }

    public void Deconstruct(out INamedTypeSymbol @interface, out AttributeData[] attributes, out MethodWithAttributes[] methodAttributes)
    {
        @interface = Interface;
        attributes = Attributes;
        methodAttributes = MethodAttributes;
    }
}

public class MethodWithAttributes
{
    public MethodWithAttributes(string name, AttributeData[] attributes)
    {
        Name = name;
        Attributes = attributes;
    }
    public string Name { get; set; }
    public AttributeData[] Attributes { get; set; }
}