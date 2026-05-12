using Redbrim.Core.Entities;
using Redbrim.Core.Enumerations;

namespace Redbrim.Core.Interfaces;

public interface ICodingAgent
{
    string Name { get; }

    CodingAgentRole Role { get; }

    IReadOnlyList<string> RequiredCapabilities { get; }

    Task<AgentResult> ExecuteAsync(AgentExecutionInput input);
}

public sealed record AgentExecutionInput(
    string Prompt,
    SystemSpecification? SystemSpecification = null,
    IReadOnlyList<AgentActionLogEntry>? Log = null,
    IReadOnlyDictionary<string, string>? Context = null);

public enum AgentStopSignal
{
    Continue,
    SoftStop,
    HardStop
}

public enum AgentCompletion
{
    Done,
    NotDone,
    Indeterminate
}

public sealed record AgentActionLogEntry(
    DateTime Timestamp,
    string AgentId,
    CodingAgentRole AgentRole,
    string Description,
    string? Data = null);

public sealed record AgentResult(
    AgentStopSignal StopSignal,
    AgentCompletion Completion,
    IReadOnlyList<AgentActionLogEntry> Log);
