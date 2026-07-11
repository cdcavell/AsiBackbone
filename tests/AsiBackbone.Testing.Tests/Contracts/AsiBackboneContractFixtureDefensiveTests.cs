using System.Reflection;
using System.Runtime.CompilerServices;
using AsiBackbone.AspNetCore.Endpoints;
using AsiBackbone.Core.Actors;
using AsiBackbone.Core.Audit;
using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Evaluation;
using AsiBackbone.Core.Results;
using AsiBackbone.Testing.Contracts;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AsiBackbone.Testing.Tests.Contracts;

/// <summary>
/// Covers defensive behavior in the reusable contract fixtures.
/// </summary>
public sealed class AsiBackboneContractFixtureDefensiveTests
{
    /// <inheritdoc/>
    [Fact]
    public async Task PolicyEvaluatorContractRejectsNullFactories()
    {
        var nullEvaluator = new PolicyEvaluatorContract(null, CreateContext());
        var nullContext = new PolicyEvaluatorContract(new AllowingPolicyEvaluator(), null);

        AsiBackboneContractViolationException evaluatorException =
            await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await nullEvaluator.VerifyEvaluatorReturnsSafeDecisionAsync(
                    TestContext.Current.CancellationToken));
        AsiBackboneContractViolationException contextException =
            await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await nullContext.VerifyEvaluatorReturnsSafeDecisionAsync(
                    TestContext.Current.CancellationToken));

        Assert.Contains("provide an evaluator instance", evaluatorException.Message, StringComparison.Ordinal);
        Assert.Contains("provide an evaluation context", contextException.Message, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    [Fact]
    public async Task PolicyEvaluatorContractPreservesCancellationAndContractViolations()
    {
        var canceling = new PolicyEvaluatorContract(new CancelingPolicyEvaluator(), CreateContext());
        _ = await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await canceling.VerifyEvaluatorReturnsSafeDecisionAsync(
                TestContext.Current.CancellationToken));

        var expected = new AsiBackboneContractViolationException("contract evaluator failure");
        var violating = new PolicyEvaluatorContract(
            new ContractViolatingPolicyEvaluator(expected),
            CreateContext());

        AsiBackboneContractViolationException actual =
            await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await violating.VerifyEvaluatorReturnsSafeDecisionAsync(
                    TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    /// <inheritdoc/>
    [Fact]
    public async Task PolicyEvaluatorContractWrapsImplementationFailure()
    {
        var contract = new PolicyEvaluatorContract(new ThrowingPolicyEvaluator(), CreateContext());

        AsiBackboneContractViolationException exception =
            await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await contract.VerifyEvaluatorReturnsSafeDecisionAsync(
                    TestContext.Current.CancellationToken));

        Assert.Contains("must not fail open", exception.Message, StringComparison.Ordinal);
        _ = Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    /// <inheritdoc/>
    [Fact]
    public async Task DecisionPolicyContractRejectsNullFactories()
    {
        var nullPolicy = new DecisionPolicyContract(null, CreateContext());
        var nullContext = new DecisionPolicyContract(new PassthroughDecisionPolicy(), null);
        var nullDecision = new DecisionPolicyContract(
            new PassthroughDecisionPolicy(),
            CreateContext(),
            returnNullDecision: true);
        var nullResults = new DecisionPolicyContract(
            new PassthroughDecisionPolicy(),
            CreateContext(),
            returnNullResults: true);

        Assert.Contains(
            "provide a decision policy instance",
            (await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await nullPolicy.VerifyDecisionPolicyReturnsSafeDecisionAsync(
                    TestContext.Current.CancellationToken))).Message,
            StringComparison.Ordinal);
        Assert.Contains(
            "provide an evaluation context",
            (await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await nullContext.VerifyDecisionPolicyReturnsSafeDecisionAsync(
                    TestContext.Current.CancellationToken))).Message,
            StringComparison.Ordinal);
        Assert.Contains(
            "provide a composed decision",
            (await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await nullDecision.VerifyDecisionPolicyReturnsSafeDecisionAsync(
                    TestContext.Current.CancellationToken))).Message,
            StringComparison.Ordinal);
        Assert.Contains(
            "provide a constraint-result collection",
            (await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await nullResults.VerifyDecisionPolicyReturnsSafeDecisionAsync(
                    TestContext.Current.CancellationToken))).Message,
            StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    [Fact]
    public async Task DecisionPolicyContractPreservesCancellationAndContractViolations()
    {
        var canceling = new DecisionPolicyContract(new CancelingDecisionPolicy(), CreateContext());
        _ = await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await canceling.VerifyDecisionPolicyReturnsSafeDecisionAsync(
                TestContext.Current.CancellationToken));

        var expected = new AsiBackboneContractViolationException("contract policy failure");
        var violating = new DecisionPolicyContract(
            new ContractViolatingDecisionPolicy(expected),
            CreateContext());

        AsiBackboneContractViolationException actual =
            await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await violating.VerifyDecisionPolicyReturnsSafeDecisionAsync(
                    TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    /// <inheritdoc/>
    [Fact]
    public async Task DecisionPolicyContractWrapsImplementationFailure()
    {
        var contract = new DecisionPolicyContract(new ThrowingDecisionPolicy(), CreateContext());

        AsiBackboneContractViolationException exception =
            await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await contract.VerifyDecisionPolicyReturnsSafeDecisionAsync(
                    TestContext.Current.CancellationToken));

        Assert.Contains("must return safe decisions", exception.Message, StringComparison.Ordinal);
        _ = Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    /// <inheritdoc/>
    [Fact]
    public async Task ConstraintContractRejectsNullFactories()
    {
        var nullConstraint = new ConstraintContract(null, CreateContext());
        var nullContext = new ConstraintContract(new AllowingConstraint(), null);

        AsiBackboneContractViolationException constraintException =
            await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await nullConstraint.VerifyConstraintReturnsSafeResultAsync(
                    TestContext.Current.CancellationToken));
        AsiBackboneContractViolationException contextException =
            await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await nullContext.VerifyConstraintReturnsSafeResultAsync(
                    TestContext.Current.CancellationToken));

        Assert.Contains("provide a constraint instance", constraintException.Message, StringComparison.Ordinal);
        Assert.Contains("provide an evaluation context", contextException.Message, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    [Theory]
    [InlineData(ConstraintEvaluationOutcome.Denied)]
    [InlineData(ConstraintEvaluationOutcome.Warning)]
    public async Task ConstraintContractRejectsReasonRequiredResultWithoutReasons(
        ConstraintEvaluationOutcome outcome)
    {
        ConstraintEvaluationResult result = CreateConstraintResult(outcome, Array.Empty<OperationReason>());
        var contract = new ConstraintContract(new ReturningConstraint(result), CreateContext());

        AsiBackboneContractViolationException exception =
            await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await contract.VerifyConstraintReturnsSafeResultAsync(
                    TestContext.Current.CancellationToken));

        Assert.Contains("without a reason code", exception.Message, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    [Fact]
    public async Task ConstraintContractRejectsNullReason()
    {
        ConstraintEvaluationResult result = CreateConstraintResult(
            ConstraintEvaluationOutcome.Denied,
            [OperationReason.Create("contract.reason", "Reason.")]);
        SetAutoProperty(
            result,
            nameof(ConstraintEvaluationResult.Reasons),
            new OperationReason?[] { null });
        var contract = new ConstraintContract(new ReturningConstraint(result), CreateContext());

        AsiBackboneContractViolationException exception =
            await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await contract.VerifyConstraintReturnsSafeResultAsync(
                    TestContext.Current.CancellationToken));

        Assert.Contains("null reason", exception.Message, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConstraintContractRejectsBlankReasonMembers(bool blankCode)
    {
        OperationReason reason = CreateMalformedReason(
            blankCode ? " " : "contract.reason",
            blankCode ? "Reason." : " ");
        ConstraintEvaluationResult result = CreateConstraintResult(
            ConstraintEvaluationOutcome.Warning,
            [reason]);
        var contract = new ConstraintContract(new ReturningConstraint(result), CreateContext());

        AsiBackboneContractViolationException exception =
            await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await contract.VerifyConstraintReturnsSafeResultAsync(
                    TestContext.Current.CancellationToken));

        Assert.Contains(
            blankCode ? "empty reason code" : "empty reason message",
            exception.Message,
            StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    [Fact]
    public async Task ConstraintContractPreservesCancellationAndContractViolations()
    {
        var canceling = new ConstraintContract(new CancelingConstraint(), CreateContext());
        _ = await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await canceling.VerifyConstraintReturnsSafeResultAsync(
                TestContext.Current.CancellationToken));

        var expected = new AsiBackboneContractViolationException("constraint contract failure");
        var violating = new ConstraintContract(
            new ContractViolatingConstraint(expected),
            CreateContext());

        AsiBackboneContractViolationException actual =
            await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await violating.VerifyConstraintReturnsSafeResultAsync(
                    TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    /// <inheritdoc/>
    [Fact]
    public async Task AuditSinkContractRejectsNullFactories()
    {
        var nullSink = new AuditSinkContract(null, new TestAuditResidue());
        var nullResidue = new AuditSinkContract(new AcceptingAuditSink(), null);

        AsiBackboneContractViolationException sinkException =
            await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await nullSink.VerifyAuditSinkAcceptsValidResidueAsync(
                    TestContext.Current.CancellationToken));
        AsiBackboneContractViolationException residueException =
            await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await nullResidue.VerifyAuditSinkAcceptsValidResidueAsync(
                    TestContext.Current.CancellationToken));

        Assert.Contains("provide an audit sink instance", sinkException.Message, StringComparison.Ordinal);
        Assert.Contains("provide audit residue", residueException.Message, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    [Fact]
    public async Task AuditSinkContractPreservesCancellationAndContractViolations()
    {
        var canceling = new AuditSinkContract(new CancelingAuditSink(), new TestAuditResidue());
        _ = await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await canceling.VerifyAuditSinkAcceptsValidResidueAsync(
                TestContext.Current.CancellationToken));

        var expected = new AsiBackboneContractViolationException("audit contract failure");
        var violating = new AuditSinkContract(
            new ContractViolatingAuditSink(expected),
            new TestAuditResidue());

        AsiBackboneContractViolationException actual =
            await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await violating.VerifyAuditSinkAcceptsValidResidueAsync(
                    TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    /// <inheritdoc/>
    [Fact]
    public async Task CapabilityContractRejectsNullFactories()
    {
        var nullValidator = new CapabilityContract(null);
        var nullContext = new CapabilityContract(
            new DenyingCapabilityValidator(),
            returnNullContext: true);
        var nullDescriptor = new CapabilityContract(
            new DenyingCapabilityValidator(),
            returnNullDescriptor: true);
        var nullDecision = new CapabilityContract(
            new DenyingCapabilityValidator(),
            returnNullDecision: true);

        Assert.Contains(
            "provide a validator instance",
            (await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await nullValidator.VerifyKnownInvalidCapabilityGrantDoesNotAllowAsync(
                    TestContext.Current.CancellationToken))).Message,
            StringComparison.Ordinal);
        Assert.Contains(
            "provide an HTTP context",
            (await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await nullContext.VerifyKnownInvalidCapabilityGrantDoesNotAllowAsync(
                    TestContext.Current.CancellationToken))).Message,
            StringComparison.Ordinal);
        Assert.Contains(
            "provide an endpoint descriptor",
            (await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await nullDescriptor.VerifyKnownInvalidCapabilityGrantDoesNotAllowAsync(
                    TestContext.Current.CancellationToken))).Message,
            StringComparison.Ordinal);
        Assert.Contains(
            "provide a current decision",
            (await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await nullDecision.VerifyKnownInvalidCapabilityGrantDoesNotAllowAsync(
                    TestContext.Current.CancellationToken))).Message,
            StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    [Fact]
    public async Task CapabilityContractPreservesCancellationAndContractViolations()
    {
        var canceling = new CapabilityContract(new CancelingCapabilityValidator());
        _ = await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await canceling.VerifyKnownInvalidCapabilityGrantDoesNotAllowAsync(
                TestContext.Current.CancellationToken));

        var expected = new AsiBackboneContractViolationException("capability contract failure");
        var violating = new CapabilityContract(
            new ContractViolatingCapabilityValidator(expected));

        AsiBackboneContractViolationException actual =
            await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await violating.VerifyKnownInvalidCapabilityGrantDoesNotAllowAsync(
                    TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    /// <inheritdoc/>
    [Fact]
    public async Task CapabilityContractWrapsImplementationFailure()
    {
        var contract = new CapabilityContract(new ThrowingCapabilityValidator());

        AsiBackboneContractViolationException exception =
            await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
                async () => await contract.VerifyKnownInvalidCapabilityGrantDoesNotAllowAsync(
                    TestContext.Current.CancellationToken));

        Assert.Contains("must fail closed", exception.Message, StringComparison.Ordinal);
        _ = Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    private static AsiBackboneConstraintEvaluationContext CreateContext()
    {
        return new AsiBackboneConstraintEvaluationContext(
            correlationId: "contract-correlation",
            policyVersion: "policy-v1",
            policyHash: "policy-hash");
    }

    private static ConstraintEvaluationResult CreateConstraintResult(
        ConstraintEvaluationOutcome outcome,
        IReadOnlyList<OperationReason> reasons)
    {
        ConstructorInfo constructor = typeof(ConstraintEvaluationResult).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(ConstraintEvaluationOutcome), typeof(IReadOnlyList<OperationReason>)],
            modifiers: null)
            ?? throw new InvalidOperationException("Constraint result constructor was not found.");

        return (ConstraintEvaluationResult)constructor.Invoke([outcome, reasons]);
    }

    private static OperationReason CreateMalformedReason(string code, string message)
    {
        var reason = (OperationReason)RuntimeHelpers.GetUninitializedObject(typeof(OperationReason));
        SetAutoProperty(reason, nameof(OperationReason.Code), code);
        SetAutoProperty(reason, nameof(OperationReason.Message), message);
        SetAutoProperty(
            reason,
            nameof(OperationReason.Metadata),
            new Dictionary<string, string>(StringComparer.Ordinal));
        return reason;
    }

    private static void SetAutoProperty<TTarget, TValue>(
        TTarget target,
        string propertyName,
        TValue value)
    {
        FieldInfo field = typeof(TTarget).GetField(
            $"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Backing field for '{propertyName}' was not found.");

        field.SetValue(target, value);
    }

    private sealed class PolicyEvaluatorContract(
        IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>? evaluator,
        AsiBackboneConstraintEvaluationContext? context)
        : AsiBackbonePolicyEvaluatorContract<AsiBackboneConstraintEvaluationContext>
    {
        protected override IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext> CreateEvaluator()
        {
            return evaluator!;
        }

        protected override AsiBackboneConstraintEvaluationContext CreateEvaluationContext()
        {
            return context!;
        }
    }

    private sealed class DecisionPolicyContract(
        IAsiBackboneDecisionPolicy<AsiBackboneConstraintEvaluationContext>? policy,
        AsiBackboneConstraintEvaluationContext? context,
        bool returnNullDecision = false,
        bool returnNullResults = false)
        : AsiBackboneDecisionPolicyContract<AsiBackboneConstraintEvaluationContext>
    {
        protected override IAsiBackboneDecisionPolicy<AsiBackboneConstraintEvaluationContext> CreateDecisionPolicy()
        {
            return policy!;
        }

        protected override AsiBackboneConstraintEvaluationContext CreateEvaluationContext()
        {
            return context!;
        }

        protected override GovernanceDecision CreateComposedDecision(
            AsiBackboneConstraintEvaluationContext evaluationContext)
        {
            return returnNullDecision ? null! : base.CreateComposedDecision(evaluationContext);
        }

        protected override IReadOnlyList<ConstraintEvaluationResult> CreateConstraintResults()
        {
            return returnNullResults ? null! : base.CreateConstraintResults();
        }
    }

    private sealed class ConstraintContract(
        IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext>? constraint,
        AsiBackboneConstraintEvaluationContext? context)
        : AsiBackboneConstraintContract<AsiBackboneConstraintEvaluationContext>
    {
        protected override IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext> CreateConstraint()
        {
            return constraint!;
        }

        protected override AsiBackboneConstraintEvaluationContext CreateEvaluationContext()
        {
            return context!;
        }
    }

    private sealed class AuditSinkContract(
        IAsiBackboneAuditSink? sink,
        IAsiBackboneAuditResidue? residue)
        : AsiBackboneAuditSinkContract
    {
        protected override IAsiBackboneAuditSink CreateAuditSink()
        {
            return sink!;
        }

        protected override IAsiBackboneAuditResidue CreateAuditResidue()
        {
            return residue!;
        }
    }

    private sealed class CapabilityContract(
        IAsiBackboneEndpointCapabilityGrantValidator? validator,
        bool returnNullContext = false,
        bool returnNullDescriptor = false,
        bool returnNullDecision = false)
        : AsiBackboneEndpointCapabilityGrantValidatorContract
    {
        protected override IAsiBackboneEndpointCapabilityGrantValidator CreateValidator()
        {
            return validator!;
        }

        protected override HttpContext CreateHttpContext()
        {
            return returnNullContext ? null! : base.CreateHttpContext();
        }

        protected override AsiBackboneEndpointGovernanceDescriptor CreateCapabilityDescriptor()
        {
            return returnNullDescriptor ? null! : base.CreateCapabilityDescriptor();
        }

        protected override GovernanceDecision CreateCurrentDecision()
        {
            return returnNullDecision ? null! : base.CreateCurrentDecision();
        }
    }

    private sealed class AllowingPolicyEvaluator
        : IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>
    {
        public ValueTask<GovernanceDecision> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(GovernanceDecision.Allow(
                context.CorrelationId,
                policyVersion: context.PolicyVersion,
                policyHash: context.PolicyHash));
        }
    }

    private sealed class CancelingPolicyEvaluator
        : IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>
    {
        public ValueTask<GovernanceDecision> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException<GovernanceDecision>(new OperationCanceledException());
        }
    }

    private sealed class ThrowingPolicyEvaluator
        : IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>
    {
        public ValueTask<GovernanceDecision> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException<GovernanceDecision>(
                new InvalidOperationException("Policy evaluator failed."));
        }
    }

    private sealed class ContractViolatingPolicyEvaluator(
        AsiBackboneContractViolationException exception)
        : IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>
    {
        public ValueTask<GovernanceDecision> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException<GovernanceDecision>(exception);
        }
    }

    private sealed class PassthroughDecisionPolicy
        : IAsiBackboneDecisionPolicy<AsiBackboneConstraintEvaluationContext>
    {
        public ValueTask<GovernanceDecision> ApplyAsync(
            AsiBackboneConstraintEvaluationContext context,
            GovernanceDecision composedDecision,
            IReadOnlyList<ConstraintEvaluationResult> constraintResults,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(composedDecision);
        }
    }

    private sealed class CancelingDecisionPolicy
        : IAsiBackboneDecisionPolicy<AsiBackboneConstraintEvaluationContext>
    {
        public ValueTask<GovernanceDecision> ApplyAsync(
            AsiBackboneConstraintEvaluationContext context,
            GovernanceDecision composedDecision,
            IReadOnlyList<ConstraintEvaluationResult> constraintResults,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException<GovernanceDecision>(new OperationCanceledException());
        }
    }

    private sealed class ThrowingDecisionPolicy
        : IAsiBackboneDecisionPolicy<AsiBackboneConstraintEvaluationContext>
    {
        public ValueTask<GovernanceDecision> ApplyAsync(
            AsiBackboneConstraintEvaluationContext context,
            GovernanceDecision composedDecision,
            IReadOnlyList<ConstraintEvaluationResult> constraintResults,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException<GovernanceDecision>(
                new InvalidOperationException("Decision policy failed."));
        }
    }

    private sealed class ContractViolatingDecisionPolicy(
        AsiBackboneContractViolationException exception)
        : IAsiBackboneDecisionPolicy<AsiBackboneConstraintEvaluationContext>
    {
        public ValueTask<GovernanceDecision> ApplyAsync(
            AsiBackboneConstraintEvaluationContext context,
            GovernanceDecision composedDecision,
            IReadOnlyList<ConstraintEvaluationResult> constraintResults,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException<GovernanceDecision>(exception);
        }
    }

    private sealed class AllowingConstraint
        : IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext>
    {
        public string Name => "contract.allow";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(ConstraintEvaluationResult.Allow());
        }
    }

    private sealed class ReturningConstraint(ConstraintEvaluationResult result)
        : IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext>
    {
        public string Name => "contract.returning";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(result);
        }
    }

    private sealed class CancelingConstraint
        : IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext>
    {
        public string Name => "contract.cancel";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException<ConstraintEvaluationResult>(new OperationCanceledException());
        }
    }

    private sealed class ContractViolatingConstraint(
        AsiBackboneContractViolationException exception)
        : IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext>
    {
        public string Name => "contract.violation";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException<ConstraintEvaluationResult>(exception);
        }
    }

    private sealed class AcceptingAuditSink : IAsiBackboneAuditSink
    {
        public ValueTask WriteAsync(
            IAsiBackboneAuditResidue residue,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CancelingAuditSink : IAsiBackboneAuditSink
    {
        public ValueTask WriteAsync(
            IAsiBackboneAuditResidue residue,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException(new OperationCanceledException());
        }
    }

    private sealed class ContractViolatingAuditSink(
        AsiBackboneContractViolationException exception)
        : IAsiBackboneAuditSink
    {
        public ValueTask WriteAsync(
            IAsiBackboneAuditResidue residue,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException(exception);
        }
    }

    private sealed class DenyingCapabilityValidator
        : IAsiBackboneEndpointCapabilityGrantValidator
    {
        public ValueTask<GovernanceDecision> ValidateAsync(
            HttpContext httpContext,
            AsiBackboneEndpointGovernanceDescriptor descriptor,
            GovernanceDecision currentDecision,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(GovernanceDecision.Deny(
                "contract.denied",
                "Capability denied."));
        }
    }

    private sealed class CancelingCapabilityValidator
        : IAsiBackboneEndpointCapabilityGrantValidator
    {
        public ValueTask<GovernanceDecision> ValidateAsync(
            HttpContext httpContext,
            AsiBackboneEndpointGovernanceDescriptor descriptor,
            GovernanceDecision currentDecision,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException<GovernanceDecision>(new OperationCanceledException());
        }
    }

    private sealed class ThrowingCapabilityValidator
        : IAsiBackboneEndpointCapabilityGrantValidator
    {
        public ValueTask<GovernanceDecision> ValidateAsync(
            HttpContext httpContext,
            AsiBackboneEndpointGovernanceDescriptor descriptor,
            GovernanceDecision currentDecision,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException<GovernanceDecision>(
                new InvalidOperationException("Capability validator failed."));
        }
    }

    private sealed class ContractViolatingCapabilityValidator(
        AsiBackboneContractViolationException exception)
        : IAsiBackboneEndpointCapabilityGrantValidator
    {
        public ValueTask<GovernanceDecision> ValidateAsync(
            HttpContext httpContext,
            AsiBackboneEndpointGovernanceDescriptor descriptor,
            GovernanceDecision currentDecision,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException<GovernanceDecision>(exception);
        }
    }

    private sealed class TestAuditResidue : IAsiBackboneAuditResidue
    {
        public string EventId => "contract-event";
        public DateTimeOffset OccurredUtc => DateTimeOffset.UtcNow;
        public string ActorId => "contract-actor";
        public AsiBackboneActorType ActorType => AsiBackboneActorType.System;
        public string? ActorDisplayName => "Contract Actor";
        public string OperationName => "contract.operation";
        public string Outcome => "Allowed";
        public IReadOnlyList<string> ReasonCodes => Array.Empty<string>();
        public string? CorrelationId => "contract-correlation";
        public string? TraceId => "contract-trace";
        public string? PolicyVersion => "policy-v1";
        public string? PolicyHash => "policy-hash";
        public IReadOnlyDictionary<string, string> Metadata =>
            new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
