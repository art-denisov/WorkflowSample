using Microsoft.Agents.AI.Workflows;

namespace WorkflowSample;

public static class Logger {

    public record LoggerOptions {
        public bool Enabled { get; init; } = true;
        public bool SkipAgentUpdateEvents { get; init; } = true;
    }

    static Logger() {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
    }

    public static void PrintTableHeader() {
        Console.WriteLine(
            Pad("TIME", 15) +
            Pad("EVENT", 30) +
            Pad("ELAPSED", 12) +
            Pad("DELTA", 12) +
            Pad("EXECUTOR", 25) +
            Pad("DATA", 40)
        );

        Console.WriteLine(new string('-', 130));
    }

    public static void LogWorkflowEvent( WorkflowEvent evt, DateTime nowUtc, TimeSpan elapsed, TimeSpan delta) {
        const int TIME_W = 15;
        const int EVENT_W = 30;
        const int ELAPSED_W = 12;
        const int DELTA_W = 12;
        const int EXECUTOR_W = 25;
        const int DATA_W = 40;
        
        var time = $"[{nowUtc:HH:mm:ss.fff}]";
        var eventName = evt.GetType().Name;
        var elapsedStr = $"+{elapsed.TotalMilliseconds:0} ms";
        var deltaStr = $"Δ{delta.TotalMilliseconds:0} ms";
        var executor = TryGetExecutorId(evt) ?? "";
        var data = TryGetShortData(evt) ?? "";

        var timeCol = Pad(time, TIME_W);
        var eventCol = Pad(eventName, EVENT_W);
        var elapsedCol = Pad(elapsedStr, ELAPSED_W);
        var deltaCol = Pad(deltaStr, DELTA_W);
        var executorCol = Pad(executor, EXECUTOR_W);
        var dataCol = Pad(data, DATA_W);

        Console.Write($"{AnsiColors.Gray}{timeCol}{AnsiColors.Reset}");
        Console.Write($"{AnsiColors.Blue}{eventCol}{AnsiColors.Reset}");
        Console.Write($"{AnsiColors.Green}{elapsedCol}{AnsiColors.Reset}");
        Console.Write($"{AnsiColors.Yellow}{deltaCol}{AnsiColors.Reset}");
        Console.Write($"{AnsiColors.Cyan}{executorCol}{AnsiColors.Reset}");
        Console.Write($"{AnsiColors.Gray}{dataCol}{AnsiColors.Reset}");

        if (evt is WorkflowErrorEvent err)
            Console.Write($"{AnsiColors.Red} ERROR: {err.Exception?.Message}{AnsiColors.Reset}");

        Console.WriteLine();
    }

    public static string? TryGetExecutorId(WorkflowEvent evt) {
        return evt switch {
            AgentResponseUpdateEvent e => e.ExecutorId,
            AgentResponseEvent e => e.ExecutorId,
            ExecutorInvokedEvent e => e.ExecutorId,
            ExecutorCompletedEvent e => e.ExecutorId,
            ExecutorFailedEvent e => e.ExecutorId,
            WorkflowOutputEvent e => e.ExecutorId,
            _ => null
        };
    }

    public static string? TryGetShortData(WorkflowEvent evt) {
        var data = evt switch {
            WorkflowOutputEvent e => e.Data,
            ExecutorCompletedEvent e => e.Data,
            _ => null
        };

        if (data is null)
            return null;

        var text = data.ToString();
        if (string.IsNullOrWhiteSpace(text))
            return data.GetType().Name;

        text = text.Replace("\r\n", " ").Replace("\n", " ").Trim();

        const int MAX = 120;
        return text.Length <= MAX ? text : string.Concat(text.AsSpan(0, MAX), "...");
    }

    public static void PrintLogHeader(string title, DateTime startedUtc) {
        Console.WriteLine($"{AnsiColors.Magenta}=== RUN GENERATION ==={AnsiColors.Reset}");
        Console.WriteLine($"{AnsiColors.Magenta}=== {title} ==={AnsiColors.Reset}");
        Console.WriteLine($"{AnsiColors.Gray}[Started : {startedUtc:HH:mm:ss.fff} UTC]{AnsiColors.Reset}");
    }

    public static void PrintLogFooter( string title, DateTime startedAtUtc, TimeSpan elapsed) {
        Console.WriteLine($"{AnsiColors.Magenta}=== FINISH GENERATION ==={AnsiColors.Reset}");
        Console.WriteLine($"{AnsiColors.Magenta}=== {title} ==={AnsiColors.Reset}");
        Console.WriteLine($"{AnsiColors.Gray}Started : {startedAtUtc:yyyy-MM-dd HH:mm:ss.fff} UTC{AnsiColors.Reset}");
        Console.WriteLine($"{AnsiColors.Gray}Elapsed : {elapsed.TotalMilliseconds:0} ms{AnsiColors.Reset}");
    }

    static string Pad(string text, int width) {
        if (string.IsNullOrEmpty(text))
            return new string(' ', width);

        if (text.Length > width)
            return text[..(width - 3)] + "...";

        return text.PadRight(width);
    }
}