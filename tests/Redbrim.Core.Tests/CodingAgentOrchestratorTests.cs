using System.Diagnostics.CodeAnalysis;

namespace Redbrim.Core.Tests;

[ExcludeFromCodeCoverage]
public class CodingAgentOrchestratorTests
{
    // ── Constructor ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullTeam_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CodingAgentOrchestrator(null!));
    }

    [Fact]
    public void Constructor_EmptyTeam_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new CodingAgentOrchestrator([]));
        Assert.Equal("team", ex.ParamName);
    }

    [Fact]
    public void Constructor_ValidSingleAgentTeam_DoesNotThrow()
    {
        var orchestrator = new CodingAgentOrchestrator([new FakeAgent(new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, []))]);
        Assert.NotNull(orchestrator);
    }

    [Fact]
    public void Constructor_ValidMultiAgentTeam_DoesNotThrow()
    {
        var result = new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, []);
        var orchestrator = new CodingAgentOrchestrator(
            [new FakeAgent(result), new FakeAgent(result)]);
        Assert.NotNull(orchestrator);
    }

    // ── InvokeAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_NullInput_ThrowsArgumentNullException()
    {
        var orchestrator = new CodingAgentOrchestrator([new FakeAgent(new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, []))]);
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            orchestrator.InvokeAsync((AgentExecutionInput)null!));
    }

    [Fact]
    public async Task InvokeAsync_InputCanIncludeSystemSpecification()
    {
        var agent = new FakeAgent(new AgentResult(AgentStopSignal.HardStop, AgentCompletion.Done, []), CodingAgentRole.Requirements);
        var orchestrator = new CodingAgentOrchestrator([agent]);
        var specification = new SystemSpecification("spec");
        var input = new AgentExecutionInput("prompt", specification);

        await orchestrator.InvokeAsync(input);

        Assert.NotNull(agent.LastReceivedInput);
        Assert.Same(specification, agent.LastReceivedInput.SystemSpecification);
    }

    [Fact]
    public async Task InvokeAsync_StartsWithRequirements_ThenRunsNextRoleUntilStopSignal()
    {
        var requirementsResult = new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, [new AgentActionLogEntry(DateTime.UtcNow, "req", CodingAgentRole.Requirements, "requirements")]);
        var redResult = new AgentResult(AgentStopSignal.SoftStop, AgentCompletion.NotDone, [new AgentActionLogEntry(DateTime.UtcNow, "red", CodingAgentRole.Red, "red")]);
        var input = new AgentExecutionInput("test prompt");
        var requirementsAgent = new FakeAgent(requirementsResult, CodingAgentRole.Requirements);
        var redAgent = new FakeAgent(redResult, CodingAgentRole.Red);
        var greenAgent = new FakeAgent(new AgentResult(AgentStopSignal.HardStop, AgentCompletion.Indeterminate, []), CodingAgentRole.Green);
        var orchestrator = new CodingAgentOrchestrator([redAgent, requirementsAgent, greenAgent]);

        var result = await orchestrator.InvokeAsync(input);

        Assert.Equal(AgentStopSignal.SoftStop, result.StopSignal);
        Assert.Equal(AgentCompletion.NotDone, result.Completion);
        Assert.Equal(2, result.Log.Count);
        Assert.Equal(1, requirementsAgent.ExecuteCallCount);
        Assert.Equal(1, redAgent.ExecuteCallCount);
        Assert.Equal(0, greenAgent.ExecuteCallCount);
    }

    [Fact]
    public async Task InvokeAsync_WhenNoSpecAgentExists_ThrowsInvalidOperationException()
    {
        var input = new AgentExecutionInput("test prompt");
        var firstAgent = new FakeAgent(new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, [new AgentActionLogEntry(DateTime.UtcNow, "fake", CodingAgentRole.Red, "first")]), CodingAgentRole.Red);
        var secondAgent = new FakeAgent(new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, [new AgentActionLogEntry(DateTime.UtcNow, "fake", CodingAgentRole.Green, "second")]), CodingAgentRole.Green);
        var orchestrator = new CodingAgentOrchestrator([firstAgent, secondAgent]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.InvokeAsync(input));
    }

    [Fact]
    public async Task InvokeAsync_ForwardsInputToAgent()
    {
        var agentResult = new AgentResult(AgentStopSignal.HardStop, AgentCompletion.Done, []);
        var input = new AgentExecutionInput("specific prompt");
        var agent = new FakeAgent(agentResult, CodingAgentRole.Requirements);
        var orchestrator = new CodingAgentOrchestrator([agent]);

        await orchestrator.InvokeAsync(input);

        Assert.NotNull(agent.LastReceivedInput);
        Assert.Equal(input.Prompt, agent.LastReceivedInput.Prompt);
        Assert.Equal(input.SystemSpecification, agent.LastReceivedInput.SystemSpecification);
        Assert.Equal(input.Context, agent.LastReceivedInput.Context);
    }

    [Fact]
    public async Task InvokeAsync_OnContinue_PassesAccumulatedLogToNextAgent()
    {
        var firstLog = new AgentActionLogEntry(DateTime.UtcNow, "requirements", CodingAgentRole.Requirements, "first");
        var initialLog = new AgentActionLogEntry(DateTime.UtcNow, "seed", CodingAgentRole.Other, "seed");
        var requirementsResult = new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, [firstLog]);
        var redResult = new AgentResult(AgentStopSignal.HardStop, AgentCompletion.Indeterminate, []);
        var specification = new SystemSpecification("My initial system description");
        var requirementsAgent = new FakeAgent(requirementsResult, CodingAgentRole.Requirements);
        var redAgent = new FakeAgent(redResult, CodingAgentRole.Red);
        var orchestrator = new CodingAgentOrchestrator([requirementsAgent, redAgent]);
        var input = new AgentExecutionInput("My prompt", specification, [initialLog]);

        var result = await orchestrator.InvokeAsync(input);

        Assert.NotNull(redAgent.LastReceivedInput);
        Assert.NotNull(redAgent.LastReceivedInput.Log);
        Assert.Equal(2, redAgent.LastReceivedInput.Log.Count);
        Assert.Equal(2, result.Log.Count);
        Assert.Same(specification, requirementsAgent.LastReceivedInput?.SystemSpecification);
    }

    [Fact]
    public async Task InvokeAsync_WhenSingleAgentContinues_KeepsLoopingUntilStopSignal()
    {
        var firstResult = new AgentResult(AgentStopSignal.Continue, AgentCompletion.NotDone, []);
        var secondResult = new AgentResult(AgentStopSignal.SoftStop, AgentCompletion.Done, []);
        var requirementsAgent = new FakeAgent([firstResult, secondResult], CodingAgentRole.Requirements);
        var orchestrator = new CodingAgentOrchestrator([requirementsAgent]);

        var result = await orchestrator.InvokeAsync(new AgentExecutionInput("prompt"));

        Assert.Equal(AgentStopSignal.SoftStop, result.StopSignal);
        Assert.Equal(2, requirementsAgent.ExecuteCallCount);
    }

    [Theory]
    [InlineData(AgentStopSignal.HardStop, AgentCompletion.Done, RecommendedAction.HaltAndEscalateToHuman, true)]
    [InlineData(AgentStopSignal.HardStop, AgentCompletion.NotDone, RecommendedAction.HaltAndEscalateToHuman, true)]
    [InlineData(AgentStopSignal.HardStop, AgentCompletion.Indeterminate, RecommendedAction.HaltAndEscalateToHuman, true)]
    [InlineData(AgentStopSignal.SoftStop, AgentCompletion.Done, RecommendedAction.RouteBackForRework, true)]
    [InlineData(AgentStopSignal.SoftStop, AgentCompletion.NotDone, RecommendedAction.RouteBackForRework, true)]
    [InlineData(AgentStopSignal.SoftStop, AgentCompletion.Indeterminate, RecommendedAction.RouteBackForRework, true)]
    [InlineData(AgentStopSignal.Continue, AgentCompletion.Done, RecommendedAction.ProceedToNextRole, false)]
    [InlineData(AgentStopSignal.Continue, AgentCompletion.NotDone, RecommendedAction.ProceedToNextRole, false)]
    [InlineData(AgentStopSignal.Continue, AgentCompletion.Indeterminate, RecommendedAction.ProceedToNextRole, false)]
    public void DetermineRecommendedAction_UsesStopSignal_AndIgnoresCompletion(
        AgentStopSignal stopSignal,
        AgentCompletion completion,
        RecommendedAction expectedAction,
        bool expectedShouldHalt)
    {
        var requirementsAgent = new FakeAgent(new AgentResult(AgentStopSignal.HardStop, AgentCompletion.Done, []), CodingAgentRole.Requirements);
        var redAgent = new FakeAgent(new AgentResult(AgentStopSignal.HardStop, AgentCompletion.Done, []), CodingAgentRole.Red);
        var orderedAgents = new ICodingAgent[] { requirementsAgent, redAgent };
        var result = new AgentResult(stopSignal, completion, []);

        var decision = CodingAgentOrchestrator.DetermineRecommendedAction(result, requirementsAgent, orderedAgents);

        Assert.Equal(expectedAction, decision.Action);
        Assert.Equal(expectedShouldHalt, decision.ShouldHalt);
        Assert.Equal(expectedShouldHalt ? null : redAgent, decision.NextAgent);
    }

    [Fact]
    public void DetermineRecommendedAction_Continue_FromLastAgent_WrapsToFirstAgent()
    {
        var requirementsAgent = new FakeAgent(new AgentResult(AgentStopSignal.HardStop, AgentCompletion.Done, []), CodingAgentRole.Requirements);
        var redAgent = new FakeAgent(new AgentResult(AgentStopSignal.HardStop, AgentCompletion.Done, []), CodingAgentRole.Red);
        var orderedAgents = new ICodingAgent[] { requirementsAgent, redAgent };
        var result = new AgentResult(AgentStopSignal.Continue, AgentCompletion.NotDone, []);

        var decision = CodingAgentOrchestrator.DetermineRecommendedAction(result, redAgent, orderedAgents);

        Assert.False(decision.ShouldHalt);
        Assert.Equal(RecommendedAction.ProceedToNextRole, decision.Action);
        Assert.Equal(requirementsAgent, decision.NextAgent);
    }

    [Fact]
    public void DetermineRecommendedAction_NullOrderedAgents_ThrowsArgumentNullException()
    {
        var requirementsAgent = CreateRequirementsAgent();
        var result = new AgentResult(AgentStopSignal.Continue, AgentCompletion.NotDone, []);

        Assert.Throws<ArgumentNullException>(() =>
            CodingAgentOrchestrator.DetermineRecommendedAction(result, requirementsAgent, null!));
    }

    [Fact]
    public void DetermineRecommendedAction_EmptyOrderedAgents_ThrowsArgumentException()
    {
        var requirementsAgent = CreateRequirementsAgent();
        var result = new AgentResult(AgentStopSignal.Continue, AgentCompletion.NotDone, []);

        var ex = Assert.Throws<ArgumentException>(() =>
            CodingAgentOrchestrator.DetermineRecommendedAction(result, requirementsAgent, []));

        Assert.Equal("orderedAgents", ex.ParamName);
    }

    [Fact]
    public void DetermineRecommendedAction_Continue_WhenCurrentAgentNotInOrderedList_ThrowsInvalidOperationException()
    {
        var requirementsAgent = CreateRequirementsAgent();
        var redAgent = new FakeAgent(new AgentResult(AgentStopSignal.HardStop, AgentCompletion.Done, []), CodingAgentRole.Red);
        var result = new AgentResult(AgentStopSignal.Continue, AgentCompletion.NotDone, []);

        Assert.Throws<InvalidOperationException>(() =>
            CodingAgentOrchestrator.DetermineRecommendedAction(result, requirementsAgent, [redAgent]));
    }

    // ── Test double ─────────────────────────────────────────────────────────

    private static FakeAgent CreateRequirementsAgent()
        => new(new AgentResult(AgentStopSignal.Continue, AgentCompletion.NotDone, []), CodingAgentRole.Requirements);

    private sealed class FakeAgent : ICodingAgent
    {
        private readonly IReadOnlyList<AgentResult> _results;

        public FakeAgent(AgentResult result, CodingAgentRole role = CodingAgentRole.Other)
            : this([result], role)
        {
        }

        public FakeAgent(IReadOnlyList<AgentResult> results, CodingAgentRole role = CodingAgentRole.Other)
        {
            ArgumentNullException.ThrowIfNull(results);
            if (results.Count == 0)
                throw new ArgumentException("At least one result is required.", nameof(results));

            _results = results;
            Role = role;
        }

        public string Name => "fake";
        public CodingAgentRole Role { get; }
        public IReadOnlyList<string> RequiredCapabilities => [];

        public int ExecuteCallCount { get; private set; }
        public AgentExecutionInput? LastReceivedInput { get; private set; }

        public Task<AgentResult> ExecuteAsync(AgentExecutionInput input)
        {
            ExecuteCallCount++;
            LastReceivedInput = input;
            var resultIndex = Math.Min(ExecuteCallCount - 1, _results.Count - 1);
            return Task.FromResult(_results[resultIndex]);
        }
    }
}
