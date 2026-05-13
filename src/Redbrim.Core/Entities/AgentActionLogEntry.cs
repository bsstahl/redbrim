using Redbrim.Core.Enumerations;

namespace Redbrim.Core.Entities;

public sealed record AgentActionLogEntry(
    DateTime Timestamp,
    string AgentId,
    CodingAgentRole AgentRole,
    string Description,
    string? Data = null);
