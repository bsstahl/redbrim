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
    public async Task InvokeAsync_NullSystemSpecification_ThrowsArgumentNullException()
    {
        var orchestrator = new CodingAgentOrchestrator([new FakeAgent(new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, []), CodingAgentRole.Requirements)]);
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            orchestrator.InvokeAsync((SystemSpecification)null!));
    }

    [Fact]
    public async Task InvokeAsync_SelectsSpecAgentInTeam_AndReturnsResult()
    {
        var nonSpecResult = new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, [new AgentActionLogEntry(DateTime.UtcNow, "non-spec")]);
        var expectedResult = new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, [new AgentActionLogEntry(DateTime.UtcNow, "spec")]);
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
        var firstAgent = new FakeAgent(new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, [new AgentActionLogEntry(DateTime.UtcNow, "first")]), CodingAgentRole.Red);
        var secondAgent = new FakeAgent(new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, [new AgentActionLogEntry(DateTime.UtcNow, "second")]), CodingAgentRole.Green);
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
    public async Task InvokeAsync_WithSystemSpecification_PassesDescriptionToSpecAgent()
    {
        var agentResult = new AgentResult(AgentStopSignal.Continue, AgentCompletion.Done, []);
        var specification = new SystemSpecification("My initial system description");
        var agent = new FakeAgent(agentResult, CodingAgentRole.Requirements);
        var orchestrator = new CodingAgentOrchestrator([agent]);

        await orchestrator.InvokeAsync(specification);

        Assert.NotNull(agent.LastReceivedInput);
        Assert.Equal(specification.Description, agent.LastReceivedInput.Prompt);
    }

    [Theory]
    [InlineData(AgentStopSignal.HardStop, AgentCompletion.Done, OrchestratorAction.HaltAndEscalateToHuman)]
    [InlineData(AgentStopSignal.HardStop, AgentCompletion.NotDone, OrchestratorAction.HaltAndEscalateToHuman)]
    [InlineData(AgentStopSignal.HardStop, AgentCompletion.Unknown, OrchestratorAction.HaltAndEscalateToHuman)]
    [InlineData(AgentStopSignal.SoftStop, AgentCompletion.Done, OrchestratorAction.RouteBackForRework)]
    [InlineData(AgentStopSignal.SoftStop, AgentCompletion.NotDone, OrchestratorAction.RouteBackForRework)]
    [InlineData(AgentStopSignal.SoftStop, AgentCompletion.Unknown, OrchestratorAction.RouteBackForRework)]
    [InlineData(AgentStopSignal.Continue, AgentCompletion.Done, OrchestratorAction.ProceedToNextRole)]
    [InlineData(AgentStopSignal.Continue, AgentCompletion.NotDone, OrchestratorAction.ProceedToNextRole)]
    [InlineData(AgentStopSignal.Continue, AgentCompletion.Unknown, OrchestratorAction.ProceedToNextRole)]
    public void DetermineNextAction_UsesStopSignal_AndIgnoresCompletion(
        AgentStopSignal stopSignal,
        AgentCompletion completion,
        OrchestratorAction expectedAction)
    {
        var result = new AgentResult(stopSignal, completion, []);

        var action = CodingAgentOrchestrator.DetermineNextAction(result);

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
