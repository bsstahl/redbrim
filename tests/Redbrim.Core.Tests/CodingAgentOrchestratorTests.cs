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
            orchestrator.InvokeAsync((AgentExecutionInput)null!));
    }

    [Fact]
    public async Task InvokeAsync_NullSystemSpecification_ThrowsArgumentNullException()
    {
        var orchestrator = new CodingAgentOrchestrator([new FakeAgent(new AgentExecutionResult(true, "ok"), "Spec")]);
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            orchestrator.InvokeAsync((SystemSpecification)null!));
    }

    [Fact]
    public async Task InvokeAsync_SelectsSpecAgentInTeam_AndReturnsResult()
    {
        var nonSpecResult = new AgentExecutionResult(true, "non-spec");
        var expectedResult = new AgentExecutionResult(true, "spec");
        var input = new AgentExecutionInput("test prompt");
        var nonSpecAgent = new FakeAgent(nonSpecResult, "Dev");
        var specAgent = new FakeAgent(expectedResult, "Spec");
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
        var firstAgent = new FakeAgent(new AgentExecutionResult(true, "first"), "Dev");
        var secondAgent = new FakeAgent(new AgentExecutionResult(true, "second"), "Ops");
        var orchestrator = new CodingAgentOrchestrator([firstAgent, secondAgent]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.InvokeAsync(input));
    }

    [Fact]
    public async Task InvokeAsync_ForwardsInputToAgent()
    {
        var agentResult = new AgentExecutionResult(true, "ok");
        var input = new AgentExecutionInput("specific prompt");
        var agent = new FakeAgent(agentResult, "Spec");
        var orchestrator = new CodingAgentOrchestrator([agent]);

        await orchestrator.InvokeAsync(input);

        Assert.Same(input, agent.LastReceivedInput);
    }

    [Fact]
    public async Task InvokeAsync_WithSystemSpecification_PassesDescriptionToSpecAgent()
    {
        var agentResult = new AgentExecutionResult(true, "ok");
        var specification = new SystemSpecification("My initial system description");
        var agent = new FakeAgent(agentResult, "Spec");
        var orchestrator = new CodingAgentOrchestrator([agent]);

        await orchestrator.InvokeAsync(specification);

        Assert.NotNull(agent.LastReceivedInput);
        Assert.Equal(specification.Description, agent.LastReceivedInput.Prompt);
    }

    // ── Test double ─────────────────────────────────────────────────────────

    private sealed class FakeAgent(AgentExecutionResult result, string role = "tester") : ICodingAgent
    {
        public string Name => "fake";
        public string Role => role;
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
