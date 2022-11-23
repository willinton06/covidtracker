using Microsoft.CodeAnalysis;
using System;

namespace CovidTracker.Generators.Commons;

public static class Naming
{
    public static string GetCamelCaseName(this ISymbol property)
    {
        if (property.Name[0] is 'I' or 'i')
            return char.ToLower(property.Name[1]) + property.Name.Substring(2);
        else return char.ToLower(property.Name[0]) + property.Name.Substring(1);
    }

    public static string GetFieldName(this ISymbol property)
        => '_' + GetCamelCaseName(property);

    public static string GetControllerNameFromInterface(this INamedTypeSymbol @interface)
    {
        string output = @interface.Name;

        if (output.Length <= 2)
        {
            return output;
        }

        if (output[0] is 'I')
        {
            output = output.Substring(1);
        }

        if (output.EndsWith("source", StringComparison.OrdinalIgnoreCase) && output.Length > 6)
        {
            output = output.Substring(0, output.Length - 6);
        }
        else if (output.EndsWith("service", StringComparison.OrdinalIgnoreCase) && output.Length > 7)
        {
            output = output.Substring(0, output.Length - 7);
        }

        return output;
    }
}
