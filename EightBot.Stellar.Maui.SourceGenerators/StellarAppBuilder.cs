using System.Text;
using CodeGenHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Eightbot.Stellar.Maui.SourceGen
{
    [Generator]
    public class StellarAppBuilder : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            System.Diagnostics.Debug.WriteLine($"{context.Compilation.AssemblyName}");

            var builderClass =
                CodeBuilder.Create("EightBot.Stellar")
                    .AddClass("AppBuilderExtensions");

            builderClass
                .AddMethod("Test", Accessibility.Public);

            var sourceText = SourceText.From(builderClass.Build(), Encoding.UTF8);
            context.AddSource("AppBuilderExtensions.g.cs", sourceText);
        }
    }
}