using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using WorkflowSample;

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

const string USER_PROMPT = """
                          I need a standard invoice for a client. At the top — the company logo, a large “Invoice” title, invoice number, issue date, and due date. On the left — client information (bill to), on the right — shipping address if different. 
                          The main section should contain a table of line items: description of product or service, quantity, unit price, and line total. At the bottom, show subtotal, tax, shipping if applicable, and prominently highlight the total amount due. 
                          At the end, add payment terms and a short message such as “Thank you for your business.” Style — professional and clean.
                          """;

var workflow = WorkflowFactory.CreateWorkflow(chatClient, "Report Generation Workflow");
var workflowResult = await WorkflowRunner.RunWorkflowAsync(workflow, USER_PROMPT, new Logger.LoggerOptions() {Enabled = true, SkipAgentUpdateEvents = true}).ConfigureAwait(false);

ResultPrinter.Print(workflowResult);