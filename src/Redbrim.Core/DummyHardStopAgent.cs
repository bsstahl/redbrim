namespace Redbrim.Core;

public sealed class DummyHardStopAgent : ICodingAgent
{
    public string Name => "dummy-hard-stop";

    public CodingAgentRole Role => CodingAgentRole.Requirements;

    public IReadOnlyList<string> RequiredCapabilities => [];

    public Task<AgentResult> ExecuteAsync(AgentExecutionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return Task.FromResult(new AgentResult(
            AgentStopSignal.HardStop,
            AgentCompletion.Unknown,
            [
                new AgentActionLogEntry(DateTime.UtcNow, "Dummy agent invoked")
            ]));
    }
}
