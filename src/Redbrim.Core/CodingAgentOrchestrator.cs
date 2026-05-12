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

    public async Task<AgentResult> InvokeAsync(AgentExecutionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var selectedAgent = _team.FirstOrDefault(agent => agent.Role == CodingAgentRole.Requirements)
            ?? throw new InvalidOperationException($"No agent with role '{CodingAgentRole.Requirements}' is available.");

        var orderedAgents = _team
            .OrderBy(agent => agent.Role)
            .ToList();
        var startIndex = orderedAgents.FindIndex(agent => ReferenceEquals(agent, selectedAgent));
        if (startIndex < 0)
            throw new InvalidOperationException($"No agent with role '{CodingAgentRole.Requirements}' is available.");

        List<AgentActionLogEntry> accumulatedLog = [.. (input.Log ?? [])];

        for (var index = startIndex; index < orderedAgents.Count; index++)
        {
            var currentAgent = orderedAgents[index];
            var currentInput = input with { Log = accumulatedLog };
            var result = await currentAgent.ExecuteAsync(currentInput).ConfigureAwait(false);

            accumulatedLog.AddRange(result.Log);

            var resultWithAccumulatedLog = result with { Log = accumulatedLog };
            var action = DetermineRecommendedAction(resultWithAccumulatedLog);

            if (action is RecommendedAction.HaltAndEscalateToHuman or RecommendedAction.RouteBackForRework)
                return resultWithAccumulatedLog;

            if (index == orderedAgents.Count - 1)
                return resultWithAccumulatedLog;
        }

        throw new InvalidOperationException("No agent result was produced.");
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
