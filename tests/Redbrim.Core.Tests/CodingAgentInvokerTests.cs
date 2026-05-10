namespace Redbrim.Core.Tests;

public class CodingAgentInvokerTests
{
    [Fact]
    public async Task InvokeAsync_CallsAgent_AndReturnsResult()
    {
        var expectedResult = new AgentExecutionResult(true, "done");
        var input = new AgentExecutionInput("test prompt");
        var agent = new FakeAgent(expectedResult);
        var invoker = new CodingAgentInvoker(agent);

        var result = await invoker.InvokeAsync(input);

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
