using System.Reflection;

namespace WorkflowSample;

public static class AgentsPrompts {
    const string firstAgentSystem = "first_agent_prompt.md";
    const string secondAgentSystem = "second_agent_prompt.md";
    const string thirdAgentSystem = "third_agent_prompt.md";
    
    public static string FirstAgentSystem => GetPromptByName(firstAgentSystem);
    public static string SecondAgentSystem => GetPromptByName(secondAgentSystem);
    public static string ThirdAgentSystem => GetPromptByName(thirdAgentSystem);

    readonly static Assembly assembly = typeof(AgentsPrompts).Assembly;

    public static string GetPromptByName(string name) {
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(name, StringComparison.OrdinalIgnoreCase));

        if(resourceName is null)
            throw new InvalidOperationException($"Prompt '{name}' not found. Available: {string.Join(", ", assembly.GetManifestResourceNames())}");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}