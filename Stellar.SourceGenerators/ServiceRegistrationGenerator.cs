using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Stellar.SourceGenerators
{
    [Generator(LanguageNames.CSharp)]
    public sealed class ServiceRegistrationGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var registrationGroups = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                    static (syntaxContext, _) => GetRegistrationsForClass(syntaxContext))
                .Where(static group => !group.IsDefaultOrEmpty);

            var registrations = registrationGroups
                .SelectMany(static (group, _) => group);

            var combined = context.CompilationProvider.Combine(registrations.Collect());

            context.RegisterSourceOutput(
                combined,
                static (spc, source) =>
                {
                    Compilation compilation = source.Left;
                    ImmutableArray<RegistrationInfo> regs = source.Right;

                    if (regs.IsDefaultOrEmpty)
                    {
                        return;
                    }

                    string asmName = compilation.AssemblyName ?? "Registered";
                    string cleanName = new string(asmName.Where(char.IsLetterOrDigit).ToArray());
                    string methodName = $"AddRegisteredServicesFor{cleanName}";

                    var sb = new StringBuilder();
                    sb.AppendLine("using Microsoft.Extensions.DependencyInjection;")
                      .AppendLine()
                      .AppendLine("namespace Stellar")
                      .AppendLine("{")
                      .AppendLine("    public static class RegisteredServiceRegistrations")
                      .AppendLine("    {")
                      .AppendLine($"        public static IServiceCollection {methodName}(this IServiceCollection services)")
                      .AppendLine("        {");

                    foreach (RegistrationInfo reg in regs)
                    {
                        string method = reg.LifetimeValue switch
                        {
                            (int)Lifetime.Scoped => "AddScoped",
                            (int)Lifetime.Singleton => "AddSingleton",
                            _ => "AddTransient",
                        };

                        string keyedMethod = reg.LifetimeValue switch
                        {
                            (int)Lifetime.Scoped => "AddKeyedScoped",
                            (int)Lifetime.Singleton => "AddKeyedSingleton",
                            _ => "AddKeyedTransient",
                        };

                        bool isKeyed = !string.IsNullOrEmpty(reg.Key);
                        string? keyLiteral = isKeyed ? $"\"{EscapeStringLiteral(reg.Key!)}\"" : null;

                        if (reg.ExplicitServiceType is not null)
                        {
                            string serviceType = GetTypeSyntax(reg.ExplicitServiceType);
                            string implType = GetTypeSyntax(reg.ClassSymbol);

                            sb.AppendLine(
                                isKeyed
                                    ? $"            services.{keyedMethod}(typeof({serviceType}), {keyLiteral}, typeof({implType}));"
                                    : $"            services.{method}(typeof({serviceType}), typeof({implType}));");
                        }

                        if (reg.RegisterInterfaces)
                        {
                            foreach (INamedTypeSymbol iface in reg.ClassSymbol.Interfaces)
                            {
                                string serviceType = GetTypeSyntax(iface);
                                string implType = GetTypeSyntax(reg.ClassSymbol);

                                sb.AppendLine(
                                    isKeyed
                                        ? $"            services.{keyedMethod}(typeof({serviceType}), {keyLiteral}, typeof({implType}));"
                                        : $"            services.{method}(typeof({serviceType}), typeof({implType}));");
                            }
                        }

                        bool shouldSelfRegister = reg.ExplicitServiceType is null && !reg.RegisterInterfaces;

                        if (shouldSelfRegister)
                        {
                            string selfType = GetTypeSyntax(reg.ClassSymbol);

                            sb.AppendLine(
                                isKeyed
                                    ? $"            services.{keyedMethod}(typeof({selfType}), {keyLiteral}, typeof({selfType}));"
                                    : $"            services.{method}(typeof({selfType}), typeof({selfType}));");
                        }
                    }

                    sb.AppendLine("            return services;")
                      .AppendLine("        }")
                      .AppendLine("    }")
                      .AppendLine("}");

                    spc.AddSource($"{methodName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
                });
        }

        private static ImmutableArray<RegistrationInfo> GetRegistrationsForClass(GeneratorSyntaxContext syntaxContext)
        {
            var classDecl = (ClassDeclarationSyntax)syntaxContext.Node;

            if (syntaxContext.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol classSymbol)
            {
                return ImmutableArray<RegistrationInfo>.Empty;
            }

            Compilation compilation = syntaxContext.SemanticModel.Compilation;

            INamedTypeSymbol? attributeSymbol =
                compilation.GetTypeByMetadataName("Stellar.ServiceRegistrationAttribute");

            if (attributeSymbol is null)
            {
                return ImmutableArray<RegistrationInfo>.Empty;
            }

            var builder = ImmutableArray.CreateBuilder<RegistrationInfo>();

            foreach (AttributeData ad in classSymbol.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(ad.AttributeClass, attributeSymbol))
                {
                    continue;
                }

                int lifetimeValue = (int)Lifetime.Transient;
                bool registerInterfaces = false;
                INamedTypeSymbol? explicitServiceType = null;
                string? key = null;

                if (ad.ConstructorArguments.Length >= 1 &&
                    ad.ConstructorArguments[0].Value is int lv)
                {
                    lifetimeValue = lv;
                }

                if (ad.ConstructorArguments.Length >= 2 &&
                    ad.ConstructorArguments[1].Value is bool ri)
                {
                    registerInterfaces = ri;
                }

                foreach (KeyValuePair<string, TypedConstant> named in ad.NamedArguments)
                {
                    switch (named.Key)
                    {
                        case nameof(ServiceRegistrationAttribute.ServiceRegistrationType)
                            when named.Value.Value is int nlv:
                            lifetimeValue = nlv;
                            break;

                        case nameof(ServiceRegistrationAttribute.RegisterInterfaces)
                            when named.Value.Value is bool nri:
                            registerInterfaces = nri;
                            break;

                        case nameof(ServiceRegistrationAttribute.ServiceType)
                            when named.Value.Value is ITypeSymbol typeSymbol:
                            explicitServiceType = typeSymbol as INamedTypeSymbol;
                            break;

                        case nameof(ServiceRegistrationAttribute.Key)
                            when named.Value.Value is string s:
                            key = s;
                            break;
                    }
                }

                builder.Add(new RegistrationInfo(
                    classSymbol: classSymbol,
                    explicitServiceType: explicitServiceType,
                    lifetimeValue: lifetimeValue,
                    registerInterfaces: registerInterfaces,
                    key: key));
            }

            return builder.ToImmutable();
        }

        private sealed class RegistrationInfo(
            INamedTypeSymbol classSymbol,
            INamedTypeSymbol? explicitServiceType,
            int lifetimeValue,
            bool registerInterfaces,
            string? key)
        {
            public INamedTypeSymbol ClassSymbol { get; } = classSymbol;

            public INamedTypeSymbol? ExplicitServiceType { get; } = explicitServiceType;

            public int LifetimeValue { get; } = lifetimeValue;

            public bool RegisterInterfaces { get; } = registerInterfaces;

            public string? Key { get; } = key;

            public void Deconstruct(out INamedTypeSymbol classSymbol, out INamedTypeSymbol? explicitServiceType, out int lifetimeValue, out bool registerInterfaces, out string? key)
            {
                classSymbol = this.ClassSymbol;
                explicitServiceType = this.ExplicitServiceType;
                lifetimeValue = this.LifetimeValue;
                registerInterfaces = this.RegisterInterfaces;
                key = this.Key;
            }
        }

        private static string EscapeStringLiteral(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        private static string GetTypeSyntax(INamedTypeSymbol typeSymbol)
        {
            var sb = new StringBuilder();

            if (!typeSymbol.ContainingNamespace.IsGlobalNamespace)
            {
                sb
                    .Append(typeSymbol.ContainingNamespace.ToDisplayString())
                    .Append('.');
            }

            var containingTypes = new Stack<INamedTypeSymbol>();
            INamedTypeSymbol? current = typeSymbol.ContainingType;
            while (current is not null)
            {
                containingTypes.Push(current);
                current = current.ContainingType;
            }

            while (containingTypes.Count > 0)
            {
                AppendTypeName(sb, containingTypes.Pop());
                sb.Append('.');
            }

            AppendTypeName(sb, typeSymbol);

            return sb.ToString();
        }

        private static void AppendTypeName(StringBuilder sb, INamedTypeSymbol typeSymbol)
        {
            sb.Append(typeSymbol.Name);

            if (typeSymbol.TypeParameters.Length > 0)
            {
                sb.Append('<');

                if (typeSymbol.TypeParameters.Length > 1)
                {
                    sb.Append(',', typeSymbol.TypeParameters.Length - 1);
                }

                sb.Append('>');
            }
        }
    }
}
