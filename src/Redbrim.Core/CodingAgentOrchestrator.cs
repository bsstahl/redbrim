using Redbrim.Core.Enumerations;
using Redbrim.Core.Interfaces;

namespace Redbrim.Core;

public enum RecommendedAction
{
    ProceedToNextRole,
    RouteBackForRework,
    HaltAndEscalateToHuman
}

public sealed record AgentRoutingDecision(
    bool ShouldHalt,
    ICodingAgent? NextAgent,
    RecommendedAction Action);

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

        var orderedAgents = _team.OrderBy(agent => agent.Role).ToList();
        var currentAgent = _team.FirstOrDefault(agent => agent.Role == CodingAgentRole.Requirements)
            ?? throw new InvalidOperationException($"No agent with role '{CodingAgentRole.Requirements}' is available.");
        List<AgentActionLogEntry> accumulatedLog = [.. (input.Log ?? [])];

        while (true)
        {
            var currentInput = input with { Log = accumulatedLog };
            var result = await currentAgent.ExecuteAsync(currentInput).ConfigureAwait(false);

            accumulatedLog.AddRange(result.Log);

            var resultWithAccumulatedLog = result with { Log = accumulatedLog };
            var decision = DetermineRecommendedAction(resultWithAccumulatedLog, currentAgent, orderedAgents);

            if (decision.ShouldHalt)
                return resultWithAccumulatedLog;

            currentAgent = decision.NextAgent
                ?? throw new InvalidOperationException("A continuation decision must provide a next agent.");
        }
    }

    public static AgentRoutingDecision DetermineRecommendedAction(
        AgentResult result,
        ICodingAgent currentAgent,
        IReadOnlyList<ICodingAgent> orderedAgents)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(currentAgent);
        ArgumentNullException.ThrowIfNull(orderedAgents);
        if (orderedAgents.Count == 0)
            throw new ArgumentException("The ordered agent list must contain at least one agent.", nameof(orderedAgents));

        return result.StopSignal switch
        {
            AgentStopSignal.HardStop => new AgentRoutingDecision(
                ShouldHalt: true,
                NextAgent: null,
                Action: RecommendedAction.HaltAndEscalateToHuman),
            AgentStopSignal.SoftStop => new AgentRoutingDecision(
                ShouldHalt: true,
                NextAgent: null,
                Action: RecommendedAction.RouteBackForRework),
            AgentStopSignal.Continue => CreateContinueDecision(currentAgent, orderedAgents),
            _ => throw new InvalidOperationException($"Unhandled stop signal '{result.StopSignal}'.")
        };
    }

    private static AgentRoutingDecision CreateContinueDecision(
        ICodingAgent currentAgent,
        IReadOnlyList<ICodingAgent> orderedAgents)
    {
        var currentIndex = -1;
        for (var index = 0; index < orderedAgents.Count; index++)
        {
            if (!ReferenceEquals(orderedAgents[index], currentAgent))
                continue;

            currentIndex = index;
            break;
        }

        if (currentIndex < 0)
            throw new InvalidOperationException("The current agent must exist in the ordered agent list.");

        var nextIndex = (currentIndex + 1) % orderedAgents.Count;
        return new AgentRoutingDecision(
            ShouldHalt: false,
            NextAgent: orderedAgents[nextIndex],
            Action: RecommendedAction.ProceedToNextRole);
    }
}
