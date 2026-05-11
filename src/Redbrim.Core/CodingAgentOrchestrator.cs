namespace Redbrim.Core;

public sealed class CodingAgentOrchestrator
{
    private const string SpecAgentRole = "Spec";
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

        var selectedAgent = _team.FirstOrDefault(agent => agent.Role == SpecAgentRole)
            ?? throw new InvalidOperationException($"No agent with role '{SpecAgentRole}' is available.");

        return selectedAgent.ExecuteAsync(input);
    }

    public Task<AgentExecutionResult> InvokeAsync(SystemSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        return InvokeAsync(new AgentExecutionInput(specification.Description));
    }
}
