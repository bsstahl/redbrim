namespace Redbrim.Core;

public sealed class CodingAgentOrchestrator
{
    private readonly IReadOnlyList<ICodingAgent> _team;

    public CodingAgentOrchestrator(IEnumerable<ICodingAgent> team)
    {
        ArgumentNullException.ThrowIfNull(team);
        _team = [.. team];
        if (_team.Count == 0)
            throw new ArgumentException("The team must contain at least one agent.", nameof(team));
    }

    public Task<AgentExecutionResult> InvokeAsync(AgentExecutionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return _team[0].ExecuteAsync(input);
    }
}
