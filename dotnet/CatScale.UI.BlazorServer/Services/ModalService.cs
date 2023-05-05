namespace CatScale.UI.BlazorServer.Services;

public interface IModalService
{
    event Action<string>? OnShowMessage;

    void ShowMessage(string message);
    
    // Task<bool> ShowYesNo(string message);
    // Task<bool?> ShowYesNoCancel(string message);
    // Task<string> ShowInput(string message);
}

public class ModalService : IModalService
{
    public event Action<string>? OnShowMessage;

    public void ShowMessage(string message)
    {
        OnShowMessage?.Invoke(message);
    }
}