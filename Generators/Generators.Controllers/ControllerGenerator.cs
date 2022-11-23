using CovidTracker.Generators.Commons;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CovidTracker.Generators.Controllerss;

[Generator]
class ControllerGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not ControllerSyntaxContextReceiver receiver) return;

        foreach (var partialControllerDefinition in receiver.PartialControllerDefinitions)
        {
            try
            {
                AddController(partialControllerDefinition, context);
            }
            catch { Debug.WriteLine($"Failed to generate controller for {partialControllerDefinition.Interface.Name}"); }
        }

        AddControllerDefinitions(receiver.PartialControllerDefinitions, context);

        AddExtension(receiver.ControllerDefinitions, context);
    }

    static IEnumerable<(IMethodSymbol, MethodRequestType, AttributeData)> GetAllControllerMethods(INamedTypeSymbol interfaceSymbol)
    {
        foreach (var methodSymbol in interfaceSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            foreach (var attribute in methodSymbol.GetAttributes())
            {
                if (Enum.TryParse(attribute.AttributeClass?.Name.Replace("Attribute", string.Empty), out MethodRequestType requestType))
                {
                    yield return (methodSymbol, requestType, attribute);
                }
            }
        }
    }

    void AddController(PartialControllerDefinition partialControllerDefinition, GeneratorExecutionContext context)
    {
        var (@interface, attributes, methodAttributes) = partialControllerDefinition;

        string source = $@"using System;
using Microsoft.AspNetCore.Mvc;

namespace CovidTracker.Generators.Controllers
{{
    {attributes.GetDeclaration()}
    public partial class {@interface.GetControllerNameFromInterface()}Controller
    {{
{GetMethods(@interface, @interface.GetFieldName(), methodAttributes)}
    }}
}}";

        context.AddGeneratedSource(@interface.Name + ".Controller", source);

        static string GetMethods(INamedTypeSymbol interfaceSymbol, string interfaceFieldName, MethodWithAttributes[] methodAttributes)
        {
            List<string> methods = new();

            foreach (var (methodSymbol, requestType, attribute) in GetAllControllerMethods(interfaceSymbol))
            {
                if (requestType is MethodRequestType.Ignore)
                    continue;

                var attributes = methodAttributes.FirstOrDefault(t => t.Name == methodSymbol.Name)?.Attributes
                    ?? Array.Empty<AttributeData>();
                var routeAttribute = string.Empty;

                if (attribute.TryGetFirstConstructorParameterValue(out var newRoute))
                {
                    routeAttribute = $", Microsoft.AspNetCore.Mvc.RouteAttribute(\"{newRoute}\")";
                }

                string parameters = methodSymbol.Parameters.GetParametersDeclaration();
                string arguments = methodSymbol.Parameters.GetArgumentsDeclaration();

                if (requestType.HasBody()
                    && methodSymbol.Parameters.Length is 1)
                    parameters = "[FromBody] " + parameters;

                if (requestType.HasBody()
                    && methodSymbol.Parameters.Length > 1)
                {
                    methods.Add($"        public record {methodSymbol.Name}Dto({parameters});");

                    parameters = methodSymbol.Name + "Dto dto";
                    arguments = string.Join(", ", methodSymbol.Parameters.Select(p => $"dto.{p.Name}"));
                }

                string returnType = methodSymbol.ReturnType.Name switch
                {
                    "System.Threading.Tasks.Task" => "Task<IActionResult>",
                    var name when name.StartsWith("System.Threading.Tasks.Task") => $"Task<ActionResult<{methodSymbol.ReturnType.GetTypeWithoutTask()}>>",
                    _ => "Task<IActionResult>"
                };

                var methodInvokation = GetMethodInvokation(interfaceFieldName, methodSymbol.Name, arguments);

                string methodBody;

                if (methodSymbol.ReturnType.HasTypeArguments())
                {
                    if (methodSymbol.ReturnType.TryGetStatusCode(out var name))
                    {
                        methodBody = @$"{{
            var result = await {methodInvokation};
            return StatusCode((int)result.{name}, result);
        }}";
                    }
                    else methodBody = $"    => Ok(await {methodInvokation});";
                }
                else
                {
                    methodBody = @$"{{
            await {methodInvokation};
            return Ok();
        }}";
                }

                methods.Add(GetMethod(
                    methodBody,
                    returnType,
                    requestType,
                    routeAttribute,
                    methodSymbol.Name,
                    parameters,
                    attributes));
            }

            return string.Join(Environment.NewLine + Environment.NewLine, methods);

            static string GetMethod(string methodBody,
                string returnType,
                MethodRequestType requestType,
                string routeAttribute,
                string methodName,
                string parameters,
                AttributeData[] attributes)
                    => $@"        [Http{requestType}{routeAttribute}{GetAttributes(attributes)}]
        public async {returnType} {methodName}({parameters})
        {methodBody}";

            static string GetMethodInvokation(string fieldName, string methodName, string arguments)
                => $"{fieldName}.{methodName}({arguments})";

            static string GetAttributes(AttributeData[] attributes)
                => attributes.Any() ? ", " + string.Join(", ", attributes.Select(t => t.ToString())) : string.Empty;
        }
    }

    void AddExtension(List<ControllerDefinition> interfaceSymbolGroups, GeneratorExecutionContext context)
    {
        const string top = @"using Microsoft.Extensions.DependencyInjection;
namespace CovidTracker.Generators.Controllers 
{
    public static partial class IServiceCollectionExtensions 
    {
        public static IServiceCollection Add{Target}GeneratedControllers(this IServiceCollection services)
        {
{ServiceList}
            return services;
        }
    }
}
";

        StringBuilder sourceBuilder = new();

        foreach (var (@class, interfaceSymbols) in interfaceSymbolGroups)
        {
            foreach (var interfaceSymbol in interfaceSymbols)
            {
                sourceBuilder.AppendLine($"            services.AddTransient<{interfaceSymbol}, {@class}>();");
            }
        }

        var source = top.Replace("{ServiceList}", sourceBuilder.ToString()).Replace("{Target}", context.Compilation.AssemblyName?.Split('.').LastOrDefault());

        context.AddGeneratedSource("IServiceCollectionExtensions", source);
    }

    void AddControllerDefinitions(IEnumerable<PartialControllerDefinition> partialControllerDefinitions, GeneratorExecutionContext context)
    {
        Dictionary<string, List<INamedTypeSymbol>> dict = new();

        foreach (var (@interface, _, _) in partialControllerDefinitions)
        {
            string controllerName = @interface.GetControllerNameFromInterface() + "Controller";
            if (dict.TryGetValue(controllerName, out var list))
            {
                list.Add(@interface);
            }
            else dict[controllerName] = new() { @interface };
        }

        StringBuilder sourceBuilder = new();

        const string top = @"using Microsoft.AspNetCore.Mvc;
namespace CovidTracker.Generators.Controllers
{
{Controllers}
}";

        foreach (var controllerName in dict.Keys)
        {
            var controllerInterfaceSymbols = dict[controllerName];
            string parameterDeclarions = GetParameterDeclarations(controllerInterfaceSymbols);
            string paramerterAssigments = GetParamerterAssigments(controllerInterfaceSymbols);
            string fieldDefinitions = GetFieldDefinitions(controllerInterfaceSymbols);
            sourceBuilder.AppendLine(@$"    [ApiController]
    [Route(""api/[controller]/[action]"")]
    public partial class {controllerName} : ControllerBase 
    {{
{fieldDefinitions}
        public {controllerName}({parameterDeclarions})
        {{
{paramerterAssigments}        
        }}
    }}");
        }

        string source = top.Replace("{Controllers}", sourceBuilder.ToString());

        context.AddGeneratedSource("ControllerDefinitions", source);

        string GetParameterDeclarations(List<INamedTypeSymbol> interfaceSymbols)
            => string.Join(", ", interfaceSymbols.Select(i => $"{i} {i.GetCamelCaseName()}"));

        string GetParamerterAssigments(List<INamedTypeSymbol> interfaceSymbols)
            => string.Join(Environment.NewLine, interfaceSymbols.Select(s => $"            {s.GetFieldName()} = {s.GetCamelCaseName()};"));

        string GetFieldDefinitions(List<INamedTypeSymbol> interfaceSymbols)
            => string.Join(Environment.NewLine, interfaceSymbols.Select(s => $"        private readonly {s} {s.GetFieldName()};"));
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        const string attributeText = @"using System;
namespace CovidTracker.Generators.Controllers
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateControllerAttribute : Attribute { }
}";

        context.AddPostInitializationSource("GenerateControllerAttribute", attributeText);

        context.RegisterForSyntaxNotifications(() => new ControllerSyntaxContextReceiver());
#if DEBUG
        //if (!Debugger.IsAttached) Debugger.Launch();
#endif
    }
}
