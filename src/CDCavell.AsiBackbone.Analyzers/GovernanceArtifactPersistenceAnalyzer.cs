using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CDCavell.AsiBackbone.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GovernanceArtifactPersistenceAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ASIB001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Persist or continue AsiBackbone governance artifact",
        "AsiBackbone governance artifact '{0}' is created or returned and then discarded; persist audit/outbox residue or pass it to a safe continuation before execution",
        "AsiBackbone.GovernanceSafety",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Governance decisions, audit residue, capability grants, handshake outcomes, and outbox artifacts should not be created and discarded without persistence, outbox emission, audit recording, or a deliberate host-owned continuation path.");

    private static readonly ImmutableHashSet<string> GovernanceArtifactTypeNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "CDCavell.AsiBackbone.Core.Audit.AuditLedgerRecord",
        "CDCavell.AsiBackbone.Core.Audit.AuditResidue",
        "CDCavell.AsiBackbone.Core.CapabilityTokens.CapabilityGrantUseResult",
        "CDCavell.AsiBackbone.Core.CapabilityTokens.CapabilityGrantValidationResult",
        "CDCavell.AsiBackbone.Core.CapabilityTokens.CapabilityTokenGrant",
        "CDCavell.AsiBackbone.Core.Decisions.GovernanceDecision",
        "CDCavell.AsiBackbone.Core.Emissions.GovernanceEmissionEnvelope",
        "CDCavell.AsiBackbone.Core.Emissions.GovernanceEmissionResult",
        "CDCavell.AsiBackbone.Core.Handshakes.LiabilityHandshakeAcknowledgment",
        "CDCavell.AsiBackbone.Core.Outbox.GovernanceOutboxEntry");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeExpressionStatement, OperationKind.ExpressionStatement);
        context.RegisterOperationAction(AnalyzeDiscardAssignment, OperationKind.SimpleAssignment);
    }

    private static void AnalyzeExpressionStatement(OperationAnalysisContext context)
    {
        var expressionStatement = (IExpressionStatementOperation)context.Operation;

        if (IsSuppressedByHostMarker(context.ContainingSymbol) || expressionStatement.Operation is ISimpleAssignmentOperation)
        {
            return;
        }

        IOperation expression = UnwrapAwait(expressionStatement.Operation);
        ITypeSymbol? artifactType = GetGovernanceArtifactType(expression.Type);
        if (artifactType is null)
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                Rule,
                expression.Syntax.GetLocation(),
                artifactType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
    }

    private static void AnalyzeDiscardAssignment(OperationAnalysisContext context)
    {
        var assignment = (ISimpleAssignmentOperation)context.Operation;

        if (IsSuppressedByHostMarker(context.ContainingSymbol) || assignment.Target is not IDiscardOperation)
        {
            return;
        }

        IOperation value = UnwrapAwait(assignment.Value);
        ITypeSymbol? artifactType = GetGovernanceArtifactType(value.Type);
        if (artifactType is null)
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                Rule,
                assignment.Syntax.GetLocation(),
                artifactType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
    }

    private static IOperation UnwrapAwait(IOperation operation)
    {
        return operation is IAwaitOperation awaitOperation
            ? awaitOperation.Operation
            : operation;
    }

    private static ITypeSymbol? GetGovernanceArtifactType(ITypeSymbol? type)
    {
        ITypeSymbol? candidate = UnwrapKnownWrapper(type);
        if (candidate is null)
        {
            return null;
        }

        string candidateName = candidate.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        return GovernanceArtifactTypeNames.Contains(candidateName) ? candidate : null;
    }

    private static ITypeSymbol? UnwrapKnownWrapper(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType || namedType.TypeArguments.Length != 1)
        {
            return type;
        }

        string namespaceName = namedType.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? string.Empty;

        return namespaceName == "System.Threading.Tasks" && (namedType.Name == "Task" || namedType.Name == "ValueTask")
            ? namedType.TypeArguments[0]
            : namespaceName == "CDCavell.AsiBackbone.Core.Results" && namedType.Name == "OperationResult"
            ? namedType.TypeArguments[0]
            : type;
    }

    private static bool IsSuppressedByHostMarker(ISymbol? symbol)
    {
        for (ISymbol? current = symbol; current is not null; current = current.ContainingSymbol)
        {
            foreach (AttributeData attribute in current.GetAttributes())
            {
                string? attributeName = attribute.AttributeClass?.Name;
                if (attributeName is "AsiBackbonePersistenceHandled" or "AsiBackbonePersistenceHandledAttribute")
                {
                    return true;
                }
            }

            if (current is INamedTypeSymbol)
            {
                break;
            }
        }

        return false;
    }
}
