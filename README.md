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

`ICodingAgent` is the contract that all agents implement. Each agent exposes a `Name`, a `Role` (`CodingAgentRole`), a list of `RequiredCapabilities`, and an `ExecuteAsync` method that accepts an `AgentExecutionInput` and returns an `AgentResult`.

`AgentResult` carries:
- `StopSignal` (`Continue`, `SoftStop`, `HardStop`) for safety decisions
- `Completion` (`Done`, `NotDone`, `Unknown`) for workflow confidence
- `Log` (`IReadOnlyList<AgentActionLogEntry>`) for structured action visibility

### CodingAgentRole

`CodingAgentRole` is an enum that identifies the responsibility of each agent in the team:

#### Core TDD Roles

| Value | Responsibility |
|---|---|
| `Requirements` | Defines the system's intended behavior and constraints. |
| `Red` | Writes the next failing test that expresses desired behavior. |
| `Green` | Writes the minimal code needed to make the failing test pass. |
| `Refactor` | Improves internal structure without changing behavior. |

#### Understanding & Explanation

| Value | Responsibility |
|---|---|
| `Explain` | Describes why something happened or what it means. |
| `Summarize` | Condenses information without adding interpretation. |
| `Analyze` | Detects structural issues, risks, and architectural drift. |

#### System Evolution

| Value | Responsibility |
|---|---|
| `Optimize` | Improves performance or resource efficiency. |
| `Document` | Produces or updates documentation for code or behavior. |
| `WorkPlan` | Breaks goals into incremental, TDD‑safe tasks. |
| `Resilience` | Identifies and mitigates reliability and failure‑mode risks. |
| `Security` | Identifies and mitigates security vulnerabilities and unsafe patterns. |

#### Integration & Configuration

| Value | Responsibility |
|---|---|
| `Integrate` | Connects external APIs, libraries, or services. |
| `Configure` | Produces configuration, environment, or setup changes. |

#### Meta‑Agents

| Value | Responsibility |
|---|---|
| `Validate` | Ensures correctness, invariants, and safety; triggers hard stops. |
| `Review` | Evaluates quality, clarity, and design appropriateness. |

#### Fallback

| Value | Responsibility |
|---|---|
| `Other` | Represents any role outside the defined taxonomy. |

### CodingAgentOrchestrator

`CodingAgentOrchestrator` coordinates a team of `ICodingAgent` instances. It selects the agent with the `Requirements` role and invokes it with the provided input. The orchestrator accepts either a raw `AgentExecutionInput` or a `SystemSpecification` (whose `Description` is forwarded as the agent prompt). It also codifies stop-signal interpretation rules via `DetermineNextAction`:
- `HardStop` → halt and escalate to a human
- `SoftStop` → route back for rework
- `Continue` → proceed to the next role

```csharp
var orchestrator = new CodingAgentOrchestrator(team);
var result = await orchestrator.InvokeAsync(new SystemSpecification("Describe the system here."));
```
