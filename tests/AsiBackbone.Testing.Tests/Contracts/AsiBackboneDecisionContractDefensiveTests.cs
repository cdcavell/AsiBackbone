using System.Reflection;
using AsiBackbone.Core.Actors;
using AsiBackbone.Core.Audit;
using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Results;
using AsiBackbone.Testing.Contracts;
using Xunit;

namespace AsiBackbone.Testing.Tests.Contracts;

/// <summary>
/// Covers defensive decision and audit-residue contract branches.
/// </summary>
public sealed class AsiBackboneDecisionContractDefensiveTests
