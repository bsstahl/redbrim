using Redbrim.Core.Enumerations;

namespace Redbrim.Core.Entities;

public sealed record AgentResult(
    AgentStopSignal StopSignal,
    AgentCompletion Completion,
    IReadOnlyList<AgentActionLogEntry> Log);
