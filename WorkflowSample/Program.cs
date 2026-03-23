using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using WorkflowSample;

// Setup
const string DEPLOYMENT_GPT_4_1 = "gpt-4.1";
const string DEPLOYMENT_GPT_5_MINI = "gpt-5-mini";

Console.WriteLine($"{AnsiColors.Magenta}=== START ==={AnsiColors.Reset}");

var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", EnvironmentVariableTarget.User)
                          ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var azureOpenAIApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY", EnvironmentVariableTarget.User)
                        ?? throw new InvalidOperationException("AZURE_OPENAI_API_KEY is not set.");

Console.WriteLine($"{AnsiColors.Green}Using Azure OpenAI Endpoint: {azureOpenAIEndpoint}{AnsiColors.Reset}");
Console.WriteLine($"{AnsiColors.Green}Using Deployment: {DEPLOYMENT_GPT_4_1}{AnsiColors.Reset}");

var azureClient = new AzureOpenAIClient(new Uri(azureOpenAIEndpoint), new ApiKeyCredential(azureOpenAIApiKey)); 
var chatClient = azureClient.GetChatClient(DEPLOYMENT_GPT_4_1).AsIChatClient();

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