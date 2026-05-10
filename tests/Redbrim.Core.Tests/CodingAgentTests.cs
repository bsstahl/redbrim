using System.Reflection;

namespace Redbrim.Core.Tests;

public class CodingAgentTests
{
    [Fact]
    public void CodingAgent_Defines_Expected_Minimal_Surface()
    {
        var contractType = typeof(ICodingAgent);

        Assert.Equal(typeof(string), contractType.GetProperty(nameof(ICodingAgent.Name))?.PropertyType);
        Assert.Equal(typeof(string), contractType.GetProperty(nameof(ICodingAgent.Role))?.PropertyType);
        Assert.Equal(typeof(IReadOnlyList<string>), contractType.GetProperty(nameof(ICodingAgent.RequiredCapabilities))?.PropertyType);

        MethodInfo executeAsync = contractType.GetMethod(nameof(ICodingAgent.ExecuteAsync))
            ?? throw new Xunit.Sdk.XunitException("ExecuteAsync method was not found.");

        Assert.Equal(typeof(Task<AgentExecutionResult>), executeAsync.ReturnType);

        var parameters = executeAsync.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(AgentExecutionInput), parameters[0].ParameterType);
    }
}
