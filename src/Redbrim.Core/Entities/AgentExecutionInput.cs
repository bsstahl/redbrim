namespace Redbrim.Core.Entities;

public sealed record AgentExecutionInput(
    string Prompt,
    SystemSpecification? SystemSpecification = null,
    IReadOnlyList<AgentActionLogEntry>? Log = null,
    IReadOnlyDictionary<string, string>? Context = null);
