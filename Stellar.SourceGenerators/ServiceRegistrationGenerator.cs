using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Stellar.SourceGenerators
{
    [Generator]
    public class ServiceRegistrationGenerator : IIncrementalGenerator
    {
        private const string AttributeMetadataName = "Stellar.ServiceRegistrationAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // The transform yields plain strings rather than INamedTypeSymbol so the
            // pipeline model compares by value. Holding symbols here would root the
            // compilation and defeat caching.
            var registrations =
                context.SyntaxProvider
                    .ForAttributeWithMetadataName(
                        AttributeMetadataName,
                        predicate: static (node, _) => node is ClassDeclarationSyntax,
                        transform: static (ctx, _) => Render(ctx))
                    .Where(static text => !string.IsNullOrEmpty(text))
                    .Collect();

            var assemblyName =
                context.CompilationProvider
                    .Select(static (compilation, _) => compilation.AssemblyName ?? "Registered");

            context.RegisterSourceOutput(
                assemblyName.Combine(registrations),
                static (productionContext, pair) => Emit(productionContext, pair.Left, pair.Right));
        }

        private static string Render(GeneratorAttributeSyntaxContext context)
        {
            if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            foreach (var attribute in context.Attributes)
            {
                // default values
                int lifetimeValue = 0; // Transient
                bool registerInterfaces = false;

                // constructor args
                if (attribute.ConstructorArguments.Length >= 1 && attribute.ConstructorArguments[0].Value is int lv)
                {
                    lifetimeValue = lv;
                }

                if (attribute.ConstructorArguments.Length == 2 && attribute.ConstructorArguments[1].Value is bool ri)
                {
                    registerInterfaces = ri;
                }

                // named args
                foreach (var named in attribute.NamedArguments)
                {
                    if (named.Key == nameof(Stellar.ServiceRegistrationAttribute.ServiceRegistrationType) &&
                        named.Value.Value is int nlv)
                    {
                        lifetimeValue = nlv;
                    }

                    if (named.Key == nameof(Stellar.ServiceRegistrationAttribute.RegisterInterfaces) &&
                        named.Value.Value is bool nri)
                    {
                        registerInterfaces = nri;
                    }
                }

                string method = lifetimeValue switch
                {
                    (int)Lifetime.Scoped => "AddScoped",
                    (int)Lifetime.Singleton => "AddSingleton",
                    _ => "AddTransient",
                };

                var implementation = classSymbol.ToDisplayString();

                if (registerInterfaces)
                {
                    foreach (var iface in classSymbol.Interfaces)
                    {
                        builder.AppendLine(
                            $"            services.{method}(typeof({iface.ToDisplayString()}), typeof({implementation}));");
                    }
                }

                builder.AppendLine($"            services.{method}(typeof({implementation}), typeof({implementation}));");
            }

            return builder.ToString();
        }

        private static void Emit(SourceProductionContext context, string assemblyName, ImmutableArray<string> registrations)
        {
            // derive assembly-based extension method name
            var cleanName = new string(assemblyName.Where(char.IsLetterOrDigit).ToArray());
            var methodName = $"AddRegisteredServicesFor{cleanName}";

            // build source
            var sb = new StringBuilder();
            sb
                .AppendLine("using Microsoft.Extensions.DependencyInjection;")
                .AppendLine("namespace Stellar")
                .AppendLine("{")
                .AppendLine("    public static class RegisteredServiceRegistrations")
                .AppendLine("    {")
                .AppendLine($"        public static IServiceCollection {methodName}(this IServiceCollection services)")
                .AppendLine("        {");

            // Sorted so the emitted file does not depend on the order the driver
            // happens to visit syntax trees in.
            foreach (var registration in registrations.Sort(StringComparer.Ordinal))
            {
                sb.Append(registration);
            }

            sb
                .AppendLine("            return services;")
                .AppendLine("        }")
                .AppendLine("    }")
                .AppendLine("}");

            context.AddSource($"{methodName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }
}
