using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CovidTracker.Generators.Registering;

[Generator]
class RegisterGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not RegistrationSyntaxContextReceiver receiver)
            return;

        AddExtension(receiver.RegistrationSets, context);
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        const string attributeText = @"using System;
namespace CovidTracker.Generators.Register
{
    /// <summary>
    /// Registration is by default transient
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class RegisterAttribute : Attribute
    {
        public RegisterType Type { get; set; }
        public string Target { get; set; } = string.Empty;
        public bool IgnoreInterfaces { get; set; }
    }

    [AttributeUsage(AttributeTargets.Interface)]
    internal class RegisterableAttribute : Attribute { }

    internal enum RegisterType
    {
        Transient,
        Scoped,
        Singleton
    }
}";

        context.RegisterForSyntaxNotifications(() => new RegistrationSyntaxContextReceiver());

        context.RegisterForPostInitialization(i => i.AddSource("CovidTracker.Generators.RegisterAttribute", attributeText));
#if DEBUG
        if (Debugger.IsAttached is false)
        {
            //Debugger.Launch();
        }
#endif
    }

    void AddExtension(List<RegistrationSet> registrationSets, GeneratorExecutionContext context)
    {
        const string tabs = "            ";
        const string top = @"using Microsoft.Extensions.DependencyInjection;
namespace CovidTracker.Generators.Register 
{
    public static partial class IServiceCollectionExtensions 
    {
{ExtensionMethods}
    }
}";
        string method = @"        public static IServiceCollection Add{RegisterTarget}GeneratedRegistrations(this IServiceCollection services)
        {
{ServiceList}
            return services;
        }";

        StringBuilder builder = new();

        var groupings = registrationSets.GroupBy(rg => rg.Target);

        if (groupings.Any() is false)
        {
            builder.Append(method.Replace("{RegisterTarget}", context.Compilation.AssemblyName?.Split('.').LastOrDefault())
                .Replace("{ServiceList}", string.Empty));
        }
        else
        {
            List<string> methods = new();

            foreach (var registrationSet in groupings)
            {
                StringBuilder methodBuilder = new();

                foreach (var (@class, type, ignoreInterfaces, interfaces) in registrationSet)
                {
                    if (interfaces.Any() && ignoreInterfaces is false)
                    {
                        foreach (var @interface in interfaces)
                        {
                            methodBuilder.AppendLine(tabs + $"services.Add{type}<{@interface}, {@class}>();");
                        }
                    }
                    else methodBuilder.AppendLine(tabs + $"services.Add{type}<{@class}>();");
                }

                methods.Add(method.Replace("{RegisterTarget}", registrationSet.Key ?? context.Compilation.AssemblyName?.Split('.').LastOrDefault())
                    .Replace("{ServiceList}", methodBuilder.ToString()));
            }

            builder.Append(string.Join(Environment.NewLine + Environment.NewLine, methods));
        }

        var source = top.Replace("{ExtensionMethods}", builder.ToString());

        context.AddSource("CovidTracker.Generators.RegisterExtension", source);
    }
}