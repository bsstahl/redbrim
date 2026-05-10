using System.Reflection;

namespace Redbrim.Core.Tests;

public class AgentContractTests
{
    [Fact]
    public void AgentContract_Defines_Expected_Minimal_Surface()
    {
        var contractType = typeof(IAgentContract);

        Assert.Equal(typeof(string), contractType.GetProperty(nameof(IAgentContract.Name))?.PropertyType);
        Assert.Equal(typeof(string), contractType.GetProperty(nameof(IAgentContract.Role))?.PropertyType);
        Assert.Equal(typeof(IReadOnlyList<string>), contractType.GetProperty(nameof(IAgentContract.RequiredCapabilities))?.PropertyType);

        MethodInfo? executeAsync = contractType.GetMethod(nameof(IAgentContract.ExecuteAsync));
        Assert.NotNull(executeAsync);

        Assert.Equal(typeof(Task<AgentExecutionResult>), executeAsync!.ReturnType);

        var parameters = executeAsync.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(AgentExecutionInput), parameters[0].ParameterType);
    }
}
