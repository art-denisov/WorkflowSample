# WorkflowSample
The sample to reproduce MS AI Workflow strange behaviour

<img width="783" height="670" alt="EgMPKJsYrW" src="https://github.com/user-attachments/assets/89e19b43-9958-416e-8c1b-60f2783a2aed" />

<img width="621" height="199" alt="hcxeE7jMRV" src="https://github.com/user-attachments/assets/54bd22d9-5ed4-446f-b26d-26c7e03524cf" />

As shown in the screenshots, each executor starts and immediately finishes its execution one or two times before the actual meaningful run begins.
This appears to be incorrect behavior.

NOTE:

You can filter logger messages with `LoggerOptions.SkipForEvents` property.

That way:
````cs
var workflowResult = await WorkflowRunner.RunWorkflowAsync(workflow, USER_PROMPT, new Logger.LoggerOptions(){SkipForEvents = [typeof(AgentResponseUpdateEvent)]}).ConfigureAwait(false);
````
