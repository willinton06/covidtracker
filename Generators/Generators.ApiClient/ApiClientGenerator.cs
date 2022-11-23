using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using CovidTracker.Generators.Commons;

namespace CovidTracker.Generators.ApiClient;

[Generator]
class ApiClientGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not ApiClientGeneratorSyntaxContextReceiver receiver) return;

        foreach (var @interface in receiver.Interfaces)
        {
            try
            {
                AddApiClient(@interface, context);
            }
            catch
            {
                Debug.WriteLine($"Failed to generate api implementation for {@interface?.Name ?? "No Name"}");
            }
        }

        AddExtension(receiver.Interfaces, context);
    }

    static IEnumerable<(IMethodSymbol, MethodRequestType, AttributeData)> GetAllControllerMethods(INamedTypeSymbol @interface)
    {
        foreach (var methodDeclaration in @interface.GetMembers().OfType<IMethodSymbol>())
        {
            foreach (var attribute in methodDeclaration.GetAttributes())
            {
                var attributeName = attribute.AttributeClass?.Name.Replace("Attribute", string.Empty);

                if (Enum.TryParse(attributeName, out MethodRequestType requestType))
                {
                    yield return (methodDeclaration, requestType, attribute);
                    break;
                }
            }
        }
    }

    void AddApiClient(INamedTypeSymbol @interface, GeneratorExecutionContext context)
    {
        string template = GetTemplate(@interface);

        var source = template.Replace("{Methods}", GetMethods(@interface));

        context.AddGeneratedSource(@interface.Name + ".ApiClient", source);

        static string GetTemplate(INamedTypeSymbol @interface)
        {
            string serviceName = "Api" + @interface.Name.Substring(1);
            return @$"using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CovidTracker.Generators.ApiClient
{{
    public partial class {serviceName} : {@interface}
    {{
        private readonly HttpClient _httpClient;

        public {serviceName}(HttpClient httpClient)
            => _httpClient = httpClient;

{{Methods}}
    }}
}}";
        }

        static string GetMethods(INamedTypeSymbol @interface)
        {
            List<string> methods = new();

            foreach (var (methodSymbol, requestType, attribute) in GetAllControllerMethods(@interface))
            {
                var parameters = methodSymbol.Parameters;
                var apiRoute = $"api/{@interface.GetControllerNameFromInterface()}/{methodSymbol.Name.Replace("Async", string.Empty)}";
                var returnTypeNoTask = methodSymbol.ReturnType.GetTypeWithoutTask();

                if (attribute.TryGetFirstConstructorParameterValue(out var newRoute))
                    apiRoute = newRoute;

                var method = BuilMethod(methodSymbol, parameters, apiRoute, returnTypeNoTask, requestType);

                if (string.IsNullOrWhiteSpace(method) is false)
                    methods.Add(method);
            }

            return string.Join(Environment.NewLine + Environment.NewLine, methods);

            static string BuilMethod(IMethodSymbol methodSymbol, ImmutableArray<IParameterSymbol> parameters, string apiRoute, string noTaskReturnType, MethodRequestType httpMethodType)
            {
                if (httpMethodType is MethodRequestType.Ignore)
                    return GetIgnoredMethod(
                        methodSymbol.ReturnType,
                        methodSymbol.Name,
                        parameters.GetParametersDeclaration());

                string methodParams = (httpMethodType.HasBody(), parameters.Length) switch
                {
                    (true, 1) => $", {parameters[0].Name}",
                    (true, > 1) => $", new {{ {parameters.GetArgumentsDeclaration()} }}",
                    (false, > 0) => $", {parameters.GetArgumentsAsTuples()}",
                    _ => string.Empty
                };

                string methodGenericParams = "SendAsAsync" + (methodSymbol.ReturnType.HasTypeArguments(), httpMethodType.HasBody(), parameters.Length) switch
                {
                    (true, true, > 1) => $"<object, {noTaskReturnType}>",
                    (true, true, 1) => $"<{parameters[0].Type}, {noTaskReturnType}>",
                    (true, _, _) => $"<{noTaskReturnType}>",
                    _ => string.Empty
                };

                string httpMethod = $"HttpMethod.{httpMethodType}"; ;

                string method = GetMethod(
                    methodSymbol.ReturnType,
                    methodSymbol.Name,
                    methodGenericParams,
                    parameters.GetParametersDeclaration(),
                    httpMethod,
                    apiRoute,
                    methodParams);

                return method;
            }

            static string GetMethod(ITypeSymbol returnType, string methodName, string methodNameWithGenericParams, string parametersDeclaration, string httpMehod, string route, string arguments)
               => $@"        public async {returnType} {methodName}({parametersDeclaration})
            => await _httpClient.{methodNameWithGenericParams}({httpMehod}, ""{route}""{arguments});";

            static string GetIgnoredMethod(ITypeSymbol returnType, string methodName, string parametersDeclaration)
               => $@"        public {returnType} {methodName}({parametersDeclaration}) => Task.FromResult<{returnType.GetTypeWithoutTask()}>(default!);";
        }
    }

    void AddExtension(List<INamedTypeSymbol> interfaces, GeneratorExecutionContext context)
    {
        const string top = @"using Microsoft.Extensions.DependencyInjection;

namespace CovidTracker.Generators.ApiClient
{
    public static partial class IServiceCollectionExtensions 
    {
        public static IServiceCollection AddGeneratedApiClients(this IServiceCollection services)
        {
{ServiceList}
            return services;
        }
    }
}";

        StringBuilder sourceBuilder = new();

        foreach (var @interface in interfaces)
        {
            sourceBuilder.AppendLine($"            services.AddScoped<{@interface}, Api{@interface.Name.Substring(1)}>();");
        }

        var source = top.Replace("{ServiceList}", sourceBuilder.ToString());

        context.AddGeneratedSource("IServiceCollectionExtensions", source);
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        const string attributesText = @"using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;

namespace CovidTracker.Generators.ApiClient
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class GenerateApiClientAttribute : Attribute { }

{attributes}
    internal static class GeneratedExtensions
    {
        public static async Task<R?> SendAsAsync<P, R>(this HttpClient client, HttpMethod method, string route, P @object)
        {
            try
            {
                var request = await client.SendAsync(new HttpRequestMessage(method, route)
                {
                    Content = JsonContent.Create(@object)
                });

                return await request.Content.ReadFromJsonAsync<R>();
            }
            catch
            {
                return default;
            }
        }

        public static async Task<R?> SendAsAsync<R>(this HttpClient client, HttpMethod method, string route, params (string Key, string? Value)[] args)
        {
            try
            {
                var request = await client.SendAsync(new HttpRequestMessage(method, route + GetParamCollection(args)));

                return await request.Content.ReadFromJsonAsync<R>();
            }
            catch
            {
                return default;
            }
        }

        public static async Task SendAsAsync<P>(this HttpClient client, HttpMethod method, string route, P @object)
            => await client.SendAsync(new HttpRequestMessage(method, route)
            {
                Content = JsonContent.Create(@object)
            });

        public static async Task SendAsAsync(this HttpClient client, HttpMethod method, string route, params (string Key, string? Value)[] args)
            => await client.SendAsync(new HttpRequestMessage(method, route + GetParamCollection(args)));

        static string GetParamCollection(params (string Key, string? Value)[] args)
        {
            if (args.Any())
            {
                List<string> list = new(args.Length);

                foreach (var (key, value) in args)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    list.Add($""{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}"");
                }

                var output = string.Join('&', list);

                if (string.IsNullOrWhiteSpace(output) is false)
                    return ""?"" + output;
            }

            return string.Empty;
        }
    }
}";

        string attributes = GetAttributes();

        context.AddPostInitializationSource("GenerateApiClientAttribute", attributesText.Replace("{attributes}", attributes));

        context.RegisterForSyntaxNotifications(() => new ApiClientGeneratorSyntaxContextReceiver());
#if DEBUG
        //if (!Debugger.IsAttached) Debugger.Launch();
#endif

        static string GetAttributes()
        {
            List<string> output = new();

            foreach (var status in Enum.GetNames(typeof(MethodRequestType)))
            {
                var template = @$"    [AttributeUsage(AttributeTargets.Method)]
    public class {status}Attribute : Attribute
    {{
        public {status}Attribute(string? route = null) {{ }}
    }}";
                output.Add(template);
            }

            return string.Join(Environment.NewLine + Environment.NewLine,
                output);
        }
    }
}
