namespace Redbrim.Core.Tests;

public class CodingAgentOrchestratorTests
{
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

    private sealed class FakeAgent(AgentExecutionResult result) : ICodingAgent
    {
        public string Name => "fake";
        public string Role => "tester";
        public IReadOnlyList<string> RequiredCapabilities => [];
        public Task<AgentExecutionResult> ExecuteAsync(AgentExecutionInput input) =>
            Task.FromResult(result);
    }
}
