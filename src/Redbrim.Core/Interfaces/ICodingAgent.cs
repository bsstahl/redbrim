namespace Redbrim.Core;

public interface ICodingAgent
{
    string Name { get; }

    CodingAgentRole Role { get; }

    IReadOnlyList<string> RequiredCapabilities { get; }

    Task<AgentResult> ExecuteAsync(AgentExecutionInput input);
}

public sealed record AgentExecutionInput(
    string Prompt,
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
    Unknown
}

public sealed record AgentActionLogEntry(
    DateTime Timestamp,
    string Description,
    string? Data = null);

public sealed record AgentResult(
    AgentStopSignal StopSignal,
    AgentCompletion Completion,
    IReadOnlyList<AgentActionLogEntry> Log);
