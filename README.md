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

`ICodingAgent` is the contract that all agents implement. Each agent exposes a `Name`, a `Role` (`CodingAgentRole`), a list of `RequiredCapabilities`, and an `ExecuteAsync` method that accepts an `AgentExecutionInput` and returns an `AgentExecutionResult`.

### CodingAgentRole

`CodingAgentRole` is an enum that identifies the responsibility of each agent in the team:

| Value | Responsibility |
|---|---|
| `Requirements` | Captures and refines system requirements |
| `Red` | Writes failing tests (TDD red phase) |
| `Green` | Writes minimal code to pass tests (TDD green phase) |
| `Refactor` | Improves code without changing behaviour (TDD refactor phase) |
| `Explain` | Generates human-readable explanations of code |
| `Summarize` | Produces high-level summaries |
| `Analyze` | Performs static or dynamic code analysis |
| `Optimize` | Improves performance or resource usage |
| `Document` | Produces or updates documentation |
| `WorkPlan` | Breaks work into tasks or milestones |
| `Resilience` | Adds error handling, retries, and fault tolerance |
| `Security` | Identifies and remediates security issues |
| `Integrate` | Handles system or service integration |
| `Configure` | Manages configuration and environment setup |
| `Validate` | Validates correctness, contracts, and constraints |
| `Review` | Performs code review |
| `Other` | Catch-all for agents with no specific built-in role |

### CodingAgentOrchestrator

`CodingAgentOrchestrator` coordinates a team of `ICodingAgent` instances. It selects the agent with the `Requirements` role and invokes it with the provided input. The orchestrator accepts either a raw `AgentExecutionInput` or a `SystemSpecification` (whose `Description` is forwarded as the agent prompt).

```csharp
var orchestrator = new CodingAgentOrchestrator(team);
var result = await orchestrator.InvokeAsync(new SystemSpecification("Describe the system here."));
```
