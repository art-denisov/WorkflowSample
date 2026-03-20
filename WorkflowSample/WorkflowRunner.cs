using System.Diagnostics;
using Microsoft.Agents.AI.Workflows;

namespace WorkflowSample;

public static class WorkflowRunner {
    public async static Task<string> RunWorkflowAsync<TInput>(Workflow workflow, TInput input, Logger.LoggerOptions options, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(workflow);

        string? result = null;
        
        var stopwatch = Stopwatch.StartNew();
        var lastElapsed = TimeSpan.Zero;
        
        await using var run = await InProcessExecution.RunStreamingAsync(workflow, input, cancellationToken: cancellationToken);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        Logger.PrintLogHeader(workflow.Name ?? "Workflow Run", DateTime.UtcNow);
        Logger.PrintTableHeader();
        
        await foreach (var evt in run.WatchStreamAsync(cancellationToken)) {
            if(evt is AgentResponseUpdateEvent && options.SkipForEvents.Contains(evt.GetType()))
                continue;
            
            var nowUtc = DateTime.UtcNow;
            var elapsed = stopwatch.Elapsed;
            var delta = elapsed - lastElapsed;
            lastElapsed = elapsed;
            
            Logger.LogWorkflowEvent(evt, nowUtc, elapsed, delta);

            switch (evt) {
                case AgentResponseEvent agentResponseEvent: 
                    break;
                case AgentResponseUpdateEvent agentResponseUpdateEvent:
                    break;
                case ExecutorInvokedEvent executorInvokedEvent:
                    break;
                case ExecutorCompletedEvent executorCompletedEvent:
                    break;
                case ExecutorFailedEvent executorFailedEvent:
                    break;
                case SuperStepCompletedEvent superStepCompletedEvent:
                    break;
                case RequestInfoEvent requestInfoEvent:
                    break;
                case SuperStepStartedEvent superStepStartedEvent:
                    break;
                case WorkflowStartedEvent workflowStartedEvent:
                    break;
                case WorkflowErrorEvent workflowErrorEvent:
                    break;
                case WorkflowWarningEvent workflowWarningEvent:
                    break;
                case WorkflowOutputEvent workflowOutputEvent:
                    result = workflowOutputEvent.Data as string;
                    break;
            }
        }

        Logger.PrintLogFooter(workflow.Name ?? "Workflow Run", DateTime.UtcNow, stopwatch.Elapsed);
        
        return result ?? "Work flow result is null";
    }
}