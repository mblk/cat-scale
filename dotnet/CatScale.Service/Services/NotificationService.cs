namespace CatScale.Service.Services;

public interface INotificationService
{
    void NotifyScaleEventsChanged();

    event Action ScaleEventsChanged;
}

public class NotificationService : INotificationService
{
    public NotificationService()
    {
    }

    public void NotifyScaleEventsChanged()
    {
        ScaleEventsChanged?.Invoke();
    }

    public event Action? ScaleEventsChanged;
}