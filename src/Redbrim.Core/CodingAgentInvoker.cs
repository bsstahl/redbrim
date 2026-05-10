namespace Redbrim.Core;

public sealed class CodingAgentInvoker(ICodingAgent agent)
{
    private readonly ICodingAgent _agent = agent ?? throw new ArgumentNullException(nameof(agent));

    public Task<AgentExecutionResult> InvokeAsync(AgentExecutionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return _agent.ExecuteAsync(input);
    }
}
