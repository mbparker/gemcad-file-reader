using LibGemcadFileReader.Abstract;

namespace TestViewer;

public class ViewerLogger : ILoggerService
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