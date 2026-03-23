using Azure.AI.OpenAI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using WorkflowSample;

// Setup
AzureOpenAIClient azureOpenAIClient = new AzureOpenAIClient(
    new Uri("xxx"),
    new System.ClientModel.ApiKeyCredential("xxx"));
IChatClient chatClient = azureOpenAIClient.GetChatClient("gpt-4.1").AsIChatClient();

var firstAgent = AgentFactory.CreateAgent(chatClient, "FirstAgent", "FirstAgent", "You are a helpful assistant. Reply briefly."); 

var secondAgent = AgentFactory.CreateAgent(chatClient, "SecondAgent", "SecondAgent", "You are a string reverter. Reply with a reverted message");

var outputExecutor = AgentFactory.CreateOutputExecutor();

var workflow = new WorkflowBuilder(firstAgent)
    .AddEdge(firstAgent, secondAgent)
    .AddEdge(secondAgent, outputExecutor)
    .WithOutputFrom(outputExecutor)
    .Build();

var workflowResult = await WorkflowRunner.RunWorkflowAsync(workflow, "What is 2+2*2?", new Logger.LoggerOptions(){SkipForEvents = [typeof(AgentResponseUpdateEvent)]}).ConfigureAwait(false);

ResultPrinter.Print(workflowResult);