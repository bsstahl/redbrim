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
        var orchestrator = new CodingAgentOrchestrator([new FakeAgent(new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, []), CodingAgentRole.Requirements)]);
        var specification = new SystemSpecification("spec");
        var input = new AgentExecutionInput("prompt", specification);

        await orchestrator.InvokeAsync(input);
    }

    [Fact]
    public async Task InvokeAsync_SelectsSpecAgentInTeam_AndReturnsResult()
    {
        var nonSpecResult = new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, [new AgentActionLogEntry(DateTime.UtcNow, "fake", CodingAgentRole.Red, "non-spec")]);
        var expectedResult = new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, [new AgentActionLogEntry(DateTime.UtcNow, "fake", CodingAgentRole.Requirements, "spec")]);
        var input = new AgentExecutionInput("test prompt");
        var nonSpecAgent = new FakeAgent(nonSpecResult, CodingAgentRole.Red);
        var specAgent = new FakeAgent(expectedResult, CodingAgentRole.Requirements);
        var orchestrator = new CodingAgentOrchestrator([nonSpecAgent, specAgent]);

        var result = await orchestrator.InvokeAsync(input);

        Assert.Equal(expectedResult, result);
        Assert.Equal(0, nonSpecAgent.ExecuteCallCount);
        Assert.Equal(1, specAgent.ExecuteCallCount);
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
        var agentResult = new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, []);
        var input = new AgentExecutionInput("specific prompt");
        var agent = new FakeAgent(agentResult, CodingAgentRole.Requirements);
        var orchestrator = new CodingAgentOrchestrator([agent]);

        await orchestrator.InvokeAsync(input);

        Assert.Same(input, agent.LastReceivedInput);
    }

    [Fact]
    public async Task InvokeAsync_WithSystemSpecificationInInput_ForwardsInputToSpecAgent()
    {
        var agentResult = new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, []);
        var specification = new SystemSpecification("My initial system description");
        var agent = new FakeAgent(agentResult, CodingAgentRole.Requirements);
        var orchestrator = new CodingAgentOrchestrator([agent]);
        var input = new AgentExecutionInput("My prompt", specification);

        await orchestrator.InvokeAsync(input);

        Assert.NotNull(agent.LastReceivedInput);
        Assert.Same(specification, agent.LastReceivedInput.SystemSpecification);
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
