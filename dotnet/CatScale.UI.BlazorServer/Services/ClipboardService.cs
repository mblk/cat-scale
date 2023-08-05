using Microsoft.JSInterop;

namespace CatScale.UI.BlazorServer.Services;

public class ClipboardService
{
    private readonly IJSRuntime _jsRuntime;

    public ClipboardService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string> ReadText()
    {
        return await _jsRuntime.InvokeAsync<string>("navigator.clipboard.readText");
    }

    public async Task WriteText(string text)
    {
        await _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
    }
}