using System.Text;
using Xunit.Abstractions;

namespace CatScale.Service.Tests.Utils;

internal class ConsoleOutputRedirector : TextWriter
{
    private readonly ITestOutputHelper _output;

    private readonly StringBuilder _buffer = new();
    
    public override Encoding Encoding => Encoding.UTF8;
    
    public ConsoleOutputRedirector(ITestOutputHelper output)
    {
        _output = output;
    }
    
    public override void WriteLine(string? message)
    {
        _output.WriteLine(message);
    }
    
    public override void WriteLine(string format, params object?[] args)
    {
        _output.WriteLine(format, args);
    }

    public override void Write(char value)
    {
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