using LibGemcadFileReader.Abstract;

namespace TestHarness;

public class ConsoleLogger : ILoggerService
{
    public void Debug(string message)
    {
        Console.WriteLine($"[DEBUG] {message}");
    }

    public void Warn(string message)
    {
        Console.WriteLine($"[WARN] {message}");
    }
}