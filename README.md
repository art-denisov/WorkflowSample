# WorkflowSample
The sample to reproduce MS AI Workflow strange behaviour

<img width="783" height="670" alt="fpx1D6YMKl" src="https://github.com/user-attachments/assets/4eff9115-5561-4c1e-939e-3857be792396" />

<img width="621" height="199" alt="hcxeE7jMRV" src="https://github.com/user-attachments/assets/54bd22d9-5ed4-446f-b26d-26c7e03524cf" />

As shown in the screenshots, each executor starts and immediately finishes its execution one or two times before the actual meaningful run begins.
This appears to be incorrect behavior.

NOTE:

You can filter logger messages with `LoggerOptions.SkipForEvents` property.

That way:
````cs
var workflowResult = await WorkflowRunner.RunWorkflowAsync(workflow, USER_PROMPT, new Logger.LoggerOptions(){SkipForEvents = [typeof(AgentResponseUpdateEvent)]}).ConfigureAwait(false);
````



### Issue description

### Each executor fires spurious `ExecutorInvokedEvent` / `ExecutorCompletedEvent` cycles before performing actual work

### Describe the bug

When running a multi-agent workflow, each executor emits one or two "empty" `ExecutorInvokedEvent` / `ExecutorCompletedEvent` pairs with near-zero delta (Δ0–Δ1 ms) **before** the actual invocation that does real work. The genuine call only appears as the last `ExecutorCompletedEvent` with a delta proportional to LLM response time.

### Expected behavior

Each executor should produce exactly one lifecycle per invocation:
```
ExecutorInvokedEvent  →  (work happens)  →  ExecutorCompletedEvent
```

### Actual behavior

Each executor produces 1–2 instantaneous Invoked→Completed pairs first, then the real one:
```
ExecutorInvokedEvent   Δ4ms     FirstAgent   ← spurious
ExecutorCompletedEvent Δ12ms    FirstAgent   ← spurious
ExecutorInvokedEvent   Δ1ms     FirstAgent   ← spurious
ExecutorCompletedEvent Δ2758ms  FirstAgent   ← real LLM call
```

### Steps to reproduce

Minimal reproduction — two `AIAgent`s wired in a linear chain plus a custom `LastMessageExecutor`:

```csharp
// Program.cs
var firstAgent  = AgentFactory.CreateAgent(chatClient, "FirstAgent",  "FirstAgent",  "You are a helpful assistant. Reply briefly.");
var secondAgent = AgentFactory.CreateAgent(chatClient, "SecondAgent", "SecondAgent", "You are a string reverter. Reply with a reverted message.");
var outputExecutor = AgentFactory.CreateOutputExecutor();

var workflow = new WorkflowBuilder(firstAgent)
    .AddEdge(firstAgent,  secondAgent)
    .AddEdge(secondAgent, outputExecutor)
    .WithOutputFrom(outputExecutor)
    .Build();

await WorkflowRunner.RunWorkflowAsync(workflow, "What is 2+2*2?");
```

```csharp
// LastMessageExecutor — no LLM call, pure message passthrough
public class LastMessageExecutor() : Executor("LastMessageExecutor", GetExecutorOptions()) {
    [MessageHandler]
    async ValueTask<string> HandleAsync(List<ChatMessage> messages, IWorkflowContext context, CancellationToken ct = default) {
        return messages.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text ?? string.Empty;
    }

    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder pb) {
        pb.ConfigureRoutes(r => r.AddHandler<List<ChatMessage>, string>(HandleAsync));
        return pb;
    }
}
```

Full reproducible sample: [WorkflowSample](https://github.com/…/WorkflowSample)

### Log evidence

`FirstAgent` — 1 spurious Invoked→Completed cycle before real work:
```
[11:48:56.429] ExecutorInvokedEvent   +62ms    Δ4ms     FirstAgent_02082627b99...
[11:48:56.442] ExecutorCompletedEvent +75ms    Δ12ms    FirstAgent_02082627b99...
[11:48:56.443] ExecutorInvokedEvent   +76ms    Δ1ms     FirstAgent_02082627b99...
[11:48:59.201] ExecutorCompletedEvent +2834ms  Δ2758ms  FirstAgent_02082627b99...  ← real
```

`SecondAgent` and `LastMessageExecutor` show the same pattern. Notably, `LastMessageExecutor` contains **no LLM call** and no async I/O — yet it still produces spurious cycles. This rules out LLM latency or network behaviour as the root cause.

#### Environment

| | |
|---|---|
| `Microsoft.Agents.AI.Workflows` | `1.0.0-rc4` |
| `Microsoft.Agents.AI` | `1.0.0-rc4` |
| `Azure.AI.OpenAI` | `2.9.0-beta.1` |
| Target framework | `net10.0` |
| Model | Azure OpenAI `gpt-4.1` |

#### Hypothesis

The spurious cycles appear to be internal route-resolution or message-handler dispatch probes (via `ProtocolBuilder`) that are being surfaced as public `ExecutorInvokedEvent`s rather than kept as internal implementation details. The fact that even a pure-passthrough `LastMessageExecutor` (no LLM, no async I/O) reproduces the behaviour strongly supports this.

#### Impact

- Per-executor latency measurements are incorrect — spurious events carry real wall-clock timestamps and pollute timing metrics.
- UI/tooling that uses `ExecutorInvokedEvent` to show "agent is thinking…" indicators will flicker before actual work begins.
- The event stream becomes misleading and harder to reason about.

Any consumer subscribing to ExecutorInvokedEvent to measure per-executor latency will get incorrect results (the spurious events have wall-clock timestamps and will skew timing).
Tooling or UX that uses ExecutorInvokedEvent to show "agent is thinking…" indicators will flicker on/off before the real work begins.
Makes it hard to reason about the execution model from the event stream alone.

Possible cause hypothesis
The spurious cycles look like internal routing/protocol negotiation steps (e.g., ProtocolBuilder-registered route resolution or message-handler dispatch probing) that are being surfaced as public ExecutorInvokedEvents rather than suppressed as internal implementation details. The fact that even LastMessageExecutor (which has no LLM call at all) shows the same pattern supports this — it's not agent-specific.
