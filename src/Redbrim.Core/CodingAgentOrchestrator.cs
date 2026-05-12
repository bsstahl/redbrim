namespace Redbrim.Core;

public enum RecommendedAction
{
    ProceedToNextRole,
    RouteBackForRework,
    HaltAndEscalateToHuman
}

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

    public Task<AgentResult> InvokeAsync(AgentExecutionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var selectedAgent = _team.FirstOrDefault(agent => agent.Role == CodingAgentRole.Requirements)
            ?? throw new InvalidOperationException($"No agent with role '{CodingAgentRole.Requirements}' is available.");

        return selectedAgent.ExecuteAsync(input);
    }

    public static RecommendedAction DetermineRecommendedAction(AgentResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.StopSignal switch
        {
            AgentStopSignal.HardStop => RecommendedAction.HaltAndEscalateToHuman,
            AgentStopSignal.SoftStop => RecommendedAction.RouteBackForRework,
            AgentStopSignal.Continue => RecommendedAction.ProceedToNextRole,
            _ => throw new InvalidOperationException($"Unhandled stop signal '{result.StopSignal}'.")
        };
    }
}
