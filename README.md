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



## Issue description

Title: Executor fires spurious Invoked→Completed cycles before performing actual work

#### Describe the bug
When running a multi-agent workflow, each AIAgent executor produces one or two "empty" `ExecutorInvokedEvent` / `ExecutorCompletedEvent` pairs with near-zero delta (Δ0–Δ1 ms) before the genuine invocation that does actual LLM work. The real call is only visible as a final ExecutorCompletedEvent with a large delta (hundreds to thousands of ms, proportional to LLM response time).

#### Expected behavior
Each executor should produce exactly one logical lifecycle:
ExecutorInvokedEvent  → (LLM call happens) → ExecutorCompletedEvent

#### Actual behavior
Each executor produces 1–2 instantaneous Invoked→Completed pairs first, then the real one:
````
ExecutorInvokedEvent   Δ0 ms   FirstAgent   ← spurious
ExecutorCompletedEvent Δ0 ms   FirstAgent   ← spurious
ExecutorInvokedEvent   Δ1 ms   FirstAgent   ← spurious
ExecutorCompletedEvent Δ2758ms FirstAgent   ← real work
````

#### Reproduction steps

Clone WorkflowSample (the official sample repo)
Set AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_API_KEY
Run the project as-is (3-agent linear workflow: FirstAgent → SecondAgent → ThirdAgent → LastMessageExecutor)
Observe the streamed events via run.WatchStreamAsync()

Log evidence
````
First agent — 1 spurious cycle before real work:
[11:48:56.429] ExecutorInvokedEvent   +62ms    Δ4ms     FirstAgent_02082627b99...
[11:48:56.442] ExecutorCompletedEvent +75ms    Δ12ms    FirstAgent_02082627b99...
[11:48:56.443] ExecutorInvokedEvent   +76ms    Δ1ms     FirstAgent_02082627b99...
[11:48:59.201] ExecutorCompletedEvent +2834ms  Δ2758ms  FirstAgent_02082627b99...  ← real
````
Second and third agents — same pattern (Δ0 ms spurious pairs visible in both screenshots).
LastMessageExecutor (custom Executor subclass, no LLM call) shows the same pattern, ruling out LLM latency as the cause.
Environment

Package: Microsoft.Agents.AI.Workflows (version from WorkflowSample as of 2026-03-20)
Runtime: .NET 9
Model: Azure OpenAI gpt-4.1
Workflow topology: linear chain, InProcessExecution.RunStreamingAsync

#### Impact

Any consumer subscribing to ExecutorInvokedEvent to measure per-executor latency will get incorrect results (the spurious events have wall-clock timestamps and will skew timing).
Tooling or UX that uses ExecutorInvokedEvent to show "agent is thinking…" indicators will flicker on/off before the real work begins.
Makes it hard to reason about the execution model from the event stream alone.

Possible cause hypothesis
The spurious cycles look like internal routing/protocol negotiation steps (e.g., ProtocolBuilder-registered route resolution or message-handler dispatch probing) that are being surfaced as public ExecutorInvokedEvents rather than suppressed as internal implementation details. The fact that even LastMessageExecutor (which has no LLM call at all) shows the same pattern supports this — it's not agent-specific.
