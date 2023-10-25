using System.Text;

namespace CatScale.Service.Tests.Utils;

internal class ConsoleOutputRedirector : TextWriter, IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly StringBuilder _buffer = new();
    private bool _disposed;
    
    public override Encoding Encoding => Encoding.UTF8;
    
    public ConsoleOutputRedirector(ITestOutputHelper output)
    {
        _output = output;
    }

    protected override void Dispose(bool disposing)
    {
        _disposed = true; // Must no longer use the ITestOutputHelper
        base.Dispose(disposing);
    }

    public override void WriteLine(string? message)
    {
        if (_disposed) return;
        _output.WriteLine(message);
    }
    
    public override void WriteLine(string format, params object?[] args)
    {
        if (_disposed) return;
        _output.WriteLine(format, args);
    }

    public override void Write(char value)
    {
        if (_disposed) return;
        if (value is '\n' or '\r')
        {
            if (_buffer.Length > 0)
                _output.WriteLine(_buffer.ToString());
            _buffer.Clear();
        }
        else
        {
            _buffer.Append(value);
        }
    }
}