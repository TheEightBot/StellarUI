using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Stellar.SourceGenerators
{
    [Generator]
    public class ServiceRegistrationGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // retrieve the populated receiver
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            {
                return;
            }

            var compilation = context.Compilation;
            var attributeSymbol = compilation.GetTypeByMetadataName("Stellar.ServiceRegistrationAttribute");
            if (attributeSymbol == null)
            {
                return;
            }

            var registrations = new List<RegistrationInfo>();

            // inspect each class with attributes
            foreach (var candidate in receiver.CandidateClasses)
            {
                var model = compilation.GetSemanticModel(candidate.SyntaxTree);
                if (model.GetDeclaredSymbol(candidate) is not INamedTypeSymbol classSymbol)
                {
                    continue;
                }

                // get ServiceRegistrationAttribute instances
                var attrs = classSymbol.GetAttributes()
                    .Where(ad => SymbolEqualityComparer.Default.Equals(ad.AttributeClass, attributeSymbol));

                foreach (var ad in attrs)
                {
                    // default values
                    int lifetimeValue = 0; // Transient
                    bool registerInterfaces = false;

                    // constructor args
                    if (ad.ConstructorArguments.Length >= 1 && ad.ConstructorArguments[0].Value is int lv)
                    {
                        lifetimeValue = lv;
                    }

                    if (ad.ConstructorArguments.Length == 2 && ad.ConstructorArguments[1].Value is bool ri)
                    {
                        registerInterfaces = ri;
                    }

                    // named args
                    foreach (var named in ad.NamedArguments)
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

                    registrations.Add(new RegistrationInfo(classSymbol, lifetimeValue, registerInterfaces));
                }
            }

            // derive assembly-based extension method name
            var asmName = context.Compilation.AssemblyName ?? "Registered";
            var cleanName = new string(asmName.Where(char.IsLetterOrDigit).ToArray());
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

            foreach (var reg in registrations)
            {
                string method = reg.LifetimeValue switch
                {
                    (int)Lifetime.Scoped => "AddScoped",
                    (int)Lifetime.Singleton => "AddSingleton",
                    _ => "AddTransient",
                };

                if (reg.RegisterInterfaces)
                {
                    foreach (var iface in reg.ClassSymbol.Interfaces)
                    {
                        sb.AppendLine(
                            $"            services.{method}(typeof({iface.ToDisplayString()}), typeof({reg.ClassSymbol.ToDisplayString()}));");
                    }
                }

                sb.AppendLine($"            services.{method}(typeof({reg.ClassSymbol.ToDisplayString()}), typeof({reg.ClassSymbol.ToDisplayString()}));");
            }

            sb
                .AppendLine("            return services;")
                .AppendLine("        }")
                .AppendLine("    }")
                .AppendLine("}");

            context.AddSource($"{methodName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // any class with attributes is a candidate
                if (syntaxNode is ClassDeclarationSyntax cls && cls.AttributeLists.Count > 0)
                {
                    CandidateClasses.Add(cls);
                }
            }
        }

        private class RegistrationInfo
        {
            public RegistrationInfo(INamedTypeSymbol classSymbol, int lifetimeValue, bool registerInterfaces)
            {
                ClassSymbol = classSymbol;
                LifetimeValue = lifetimeValue;
                RegisterInterfaces = registerInterfaces;
            }

            public INamedTypeSymbol ClassSymbol { get; }

            public int LifetimeValue { get; }

            public bool RegisterInterfaces { get; }

            public void Deconstruct(out INamedTypeSymbol classSymbol, out int lifetimeValue, out bool registerInterfaces)
            {
                classSymbol = this.ClassSymbol;
                lifetimeValue = this.LifetimeValue;
                registerInterfaces = this.RegisterInterfaces;
            }
        }
    }
}
