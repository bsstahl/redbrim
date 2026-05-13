using Redbrim.Core.Enumerations;
using Redbrim.Core.Interfaces;

namespace Redbrim.Core.Entities;

public sealed record AgentRoutingDecision(
    bool ShouldHalt,
    ICodingAgent? NextAgent,
    RecommendedAction Action);
