using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

public static class AgentFactory {
        public static AIAgent CreateAgent(IChatClient chatClient, string agentName, string description, string agentPrompt, Type? responseType = null) {
            ArgumentNullException.ThrowIfNull(chatClient);
            ArgumentException.ThrowIfNullOrEmpty(agentName);

            ChatClientAgentOptions agentOptions = new() {
                Name = agentName,
                Description = description,
                ChatOptions = new ChatOptions {
                        Instructions = agentPrompt
                    }
            };

            return chatClient.AsAIAgent(agentOptions);
        }
        

        public static Executor CreateOutputExecutor() => new LastMessageExecutor();

        public class LastMessageExecutor() : Executor("LastMessageExecutor", GetExecutorOptions()) {
            private static ExecutorOptions GetExecutorOptions() {
                ExecutorOptions ops = ExecutorOptions.Default;
                ops.AutoSendMessageHandlerResultObject = false;
                return ops;
            }

        [MessageHandler]
        async ValueTask<string> HandleAsync(List<ChatMessage> messages, IWorkflowContext context, CancellationToken cancellationToken = default) {
            var lastMessage = FilterToLastMessage(messages);

            if(lastMessage == null) {
                await ValueTask.FromResult(string.Empty);
            }

            return lastMessage?.Text;
        }

        private static ChatMessage FilterToLastMessage(List<ChatMessage> messages) {
            return messages.LastOrDefault(m => m.Role == ChatRole.Assistant);
        }

        protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder) {
            protocolBuilder.ConfigureRoutes(routes => routes.AddHandler<List<ChatMessage>, string>(HandleAsync));
            return protocolBuilder;
        }
    }
}