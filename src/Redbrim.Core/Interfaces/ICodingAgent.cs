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
