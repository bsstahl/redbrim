namespace Redbrim.Core;

public interface ICodingAgent
{
    string Name { get; }

    CodingAgentRole Role { get; }

    IReadOnlyList<string> RequiredCapabilities { get; }

    Task<AgentExecutionResult> ExecuteAsync(AgentExecutionInput input);
}

public sealed record AgentExecutionInput(
    string Prompt,
    IReadOnlyDictionary<string, string>? Context = null);

public sealed record AgentExecutionResult(
    bool Succeeded,
    string Summary,
    IReadOnlyDictionary<string, string>? Data = null);
