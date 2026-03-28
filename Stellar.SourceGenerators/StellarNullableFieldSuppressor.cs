using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Stellar.SourceGenerators
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class StellarNullableFieldSuppressor : DiagnosticSuppressor
    {
        private const string IStellarViewInterfaceName = "IStellarView";
        private const string SetupUserInterfaceMethodName = "SetupUserInterface";

        // Justification: SetupUserInterface() is called synchronously during construction via
        // InitializeStellarComponent(), so all fields assigned there are guaranteed non-null before use.
        private static readonly SuppressionDescriptor SuppressCS8618Rule = new SuppressionDescriptor(
            id: "STELLAR0001",
            suppressedDiagnosticId: "CS8618",
            justification: "Fields in Stellar view types are initialized in SetupUserInterface(), which is called synchronously during construction via InitializeStellarComponent().");

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions =>
            ImmutableArray.Create(SuppressCS8618Rule);

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            foreach (var diagnostic in context.ReportedDiagnostics)
            {
                TrySuppress(context, diagnostic);
            }
        }

        private static void TrySuppress(SuppressionAnalysisContext context, Diagnostic diagnostic)
        {
            var location = diagnostic.Location;
            if (location.SourceTree is null)
            {
                return;
            }

            var semanticModel = context.GetSemanticModel(location.SourceTree);
            var root = location.SourceTree.GetRoot(context.CancellationToken);
            var node = root.FindNode(location.SourceSpan);

            var memberSymbol = FindMemberSymbol(node, semanticModel, context);
            if (memberSymbol is null || memberSymbol.IsStatic)
            {
                return;
            }

            var containingType = memberSymbol.ContainingType;
            if (containingType is null)
            {
                return;
            }

            if (ImplementsIStellarView(containingType) && HasSetupUserInterfaceOverride(containingType))
            {
                context.ReportSuppression(Suppression.Create(SuppressCS8618Rule, diagnostic));
            }
        }

        private static ISymbol FindMemberSymbol(
            SyntaxNode node,
            SemanticModel semanticModel,
            SuppressionAnalysisContext context)
        {
            // Try direct declaration at the node (e.g. field declarator, property declaration)
            var symbol = semanticModel.GetDeclaredSymbol(node, context.CancellationToken);
            if (symbol is IFieldSymbol || symbol is IPropertySymbol)
            {
                return symbol;
            }

            // Walk up to handle cases where the diagnostic lands on an identifier inside the declaration
            var current = node.Parent;
            while (current != null)
            {
                symbol = semanticModel.GetDeclaredSymbol(current, context.CancellationToken);
                if (symbol is IFieldSymbol || symbol is IPropertySymbol)
                {
                    return symbol;
                }

                // Stop at type boundary — don't walk past the class declaration
                if (current is TypeDeclarationSyntax)
                {
                    break;
                }

                current = current.Parent;
            }

            return null;
        }

        private static bool ImplementsIStellarView(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.AllInterfaces.Any(i => i.Name == IStellarViewInterfaceName);
        }

        private static bool HasSetupUserInterfaceOverride(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.GetMembers(SetupUserInterfaceMethodName)
                .OfType<IMethodSymbol>()
                .Any(m => !m.IsAbstract && m.Parameters.IsEmpty);
        }
    }
}
