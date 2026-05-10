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
        var orchestrator = new CodingAgentOrchestrator([new FakeAgent(new AgentExecutionResult(true, "ok"))]);
        Assert.NotNull(orchestrator);
    }

    [Fact]
    public void Constructor_ValidMultiAgentTeam_DoesNotThrow()
    {
        var result = new AgentExecutionResult(true, "ok");
        var orchestrator = new CodingAgentOrchestrator(
            [new FakeAgent(result), new FakeAgent(result)]);
        Assert.NotNull(orchestrator);
    }

    // ── InvokeAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_NullInput_ThrowsArgumentNullException()
    {
        var orchestrator = new CodingAgentOrchestrator([new FakeAgent(new AgentExecutionResult(true, "ok"))]);
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            orchestrator.InvokeAsync(null!));
    }

    [Fact]
    public async Task InvokeAsync_CallsFirstAgentInTeam_AndReturnsResult()
    {
        var expectedResult = new AgentExecutionResult(true, "done");
        var input = new AgentExecutionInput("test prompt");
        var firstAgent = new FakeAgent(expectedResult);
        var orchestrator = new CodingAgentOrchestrator([firstAgent]);

        var result = await orchestrator.InvokeAsync(input);

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task InvokeAsync_MultipleAgentsInTeam_CallsFirstAgentOnly()
    {
        var firstResult = new AgentExecutionResult(true, "first");
        var secondResult = new AgentExecutionResult(true, "second");
        var input = new AgentExecutionInput("test prompt");
        var firstAgent = new FakeAgent(firstResult);
        var secondAgent = new FakeAgent(secondResult);
        var orchestrator = new CodingAgentOrchestrator([firstAgent, secondAgent]);

        var result = await orchestrator.InvokeAsync(input);

        Assert.Equal(firstResult, result);
        Assert.Equal(1, firstAgent.ExecuteCallCount);
        Assert.Equal(0, secondAgent.ExecuteCallCount);
    }

    [Fact]
    public async Task InvokeAsync_ForwardsInputToAgent()
    {
        var agentResult = new AgentExecutionResult(true, "ok");
        var input = new AgentExecutionInput("specific prompt");
        var agent = new FakeAgent(agentResult);
        var orchestrator = new CodingAgentOrchestrator([agent]);

        await orchestrator.InvokeAsync(input);

        Assert.Same(input, agent.LastReceivedInput);
    }

    // ── Test double ─────────────────────────────────────────────────────────

    private sealed class FakeAgent(AgentExecutionResult result) : ICodingAgent
    {
        public string Name => "fake";
        public string Role => "tester";
        public IReadOnlyList<string> RequiredCapabilities => [];

        public int ExecuteCallCount { get; private set; }
        public AgentExecutionInput? LastReceivedInput { get; private set; }

        public Task<AgentExecutionResult> ExecuteAsync(AgentExecutionInput input)
        {
            ExecuteCallCount++;
            LastReceivedInput = input;
            return Task.FromResult(result);
        }
    }
}
