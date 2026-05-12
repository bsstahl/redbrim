namespace Redbrim.Core.Tests;

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
    public async Task InvokeAsync_WhenContinuingWithoutNextAgent_ReturnsLastResult()
    {
        var requirementsResult = new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, []);
        var requirementsAgent = new FakeAgent(requirementsResult, CodingAgentRole.Requirements);
        var orchestrator = new CodingAgentOrchestrator([requirementsAgent]);

        var result = await orchestrator.InvokeAsync(new AgentExecutionInput("prompt"));

        Assert.Equal(AgentStopSignal.Continue, result.StopSignal);
        Assert.Equal(1, requirementsAgent.ExecuteCallCount);
    }

    [Theory]
    [InlineData(AgentStopSignal.HardStop, AgentCompletion.Done, RecommendedAction.HaltAndEscalateToHuman)]
    [InlineData(AgentStopSignal.HardStop, AgentCompletion.NotDone, RecommendedAction.HaltAndEscalateToHuman)]
    [InlineData(AgentStopSignal.HardStop, AgentCompletion.Indeterminate, RecommendedAction.HaltAndEscalateToHuman)]
    [InlineData(AgentStopSignal.SoftStop, AgentCompletion.Done, RecommendedAction.RouteBackForRework)]
    [InlineData(AgentStopSignal.SoftStop, AgentCompletion.NotDone, RecommendedAction.RouteBackForRework)]
    [InlineData(AgentStopSignal.SoftStop, AgentCompletion.Indeterminate, RecommendedAction.RouteBackForRework)]
    [InlineData(AgentStopSignal.Continue, AgentCompletion.Done, RecommendedAction.ProceedToNextRole)]
    [InlineData(AgentStopSignal.Continue, AgentCompletion.NotDone, RecommendedAction.ProceedToNextRole)]
    [InlineData(AgentStopSignal.Continue, AgentCompletion.Indeterminate, RecommendedAction.ProceedToNextRole)]
    public void DetermineRecommendedAction_UsesStopSignal_AndIgnoresCompletion(
        AgentStopSignal stopSignal,
        AgentCompletion completion,
        RecommendedAction expectedAction)
    {
        var result = new AgentResult(stopSignal, completion, []);

        var action = CodingAgentOrchestrator.DetermineRecommendedAction(result);

        Assert.Equal(expectedAction, action);
    }

    // ── Test double ─────────────────────────────────────────────────────────

    private sealed class FakeAgent(AgentResult result, CodingAgentRole role = CodingAgentRole.Other) : ICodingAgent
    {
        public string Name => "fake";
        public CodingAgentRole Role => role;
        public IReadOnlyList<string> RequiredCapabilities => [];

        public int ExecuteCallCount { get; private set; }
        public AgentExecutionInput? LastReceivedInput { get; private set; }

        public Task<AgentResult> ExecuteAsync(AgentExecutionInput input)
        {
            ExecuteCallCount++;
            LastReceivedInput = input;
            return Task.FromResult(result);
        }
    }
}
