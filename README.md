# Redbrim

Redbrim is a .NET 10–native engine for orchestrating the Test-Driven Development loop using small, focused agents and validators. The system enforces the Red → Green → Refactor cycle and uses capability-based tooling for file editing,
test running, and code analysis.

## Core Concepts

### SystemSpecification

`SystemSpecification` is the initial input to the orchestration pipeline. It carries a plain-text description of the system to be built.

```csharp
var spec = new SystemSpecification("Users can authenticate via OAuth2.");
```

### ICodingAgent

`ICodingAgent` is the contract that all agents implement. Each agent exposes a `Name`, a `Role`, a list of `RequiredCapabilities`, and an `ExecuteAsync` method that accepts an `AgentExecutionInput` and returns an `AgentExecutionResult`.

### CodingAgentOrchestrator

`CodingAgentOrchestrator` coordinates a team of `ICodingAgent` instances. It selects the agent with the `"Spec"` role and invokes it with the provided input. The orchestrator accepts either a raw `AgentExecutionInput` or a `SystemSpecification` (whose `Description` is forwarded as the agent prompt).

```csharp
var orchestrator = new CodingAgentOrchestrator(team);
var result = await orchestrator.InvokeAsync(new SystemSpecification("Describe the system here."));
```
