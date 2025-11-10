namespace SchedulerMVP.Services;

public class ToastService
{
    public event Action<ToastMessage>? OnShowToast;
    
    public void ShowSuccess(string title, string? message = null)
    {
        OnShowToast?.Invoke(new ToastMessage
        {
            Type = ToastType.Success,
            Title = title,
            Message = message
        });
    }
    
    public void ShowError(string title, string? message = null)
    {
        OnShowToast?.Invoke(new ToastMessage
        {
            Type = ToastType.Error,
            Title = title,
            Message = message
        });
    }
    
    public void ShowInfo(string title, string? message = null)
    {
        OnShowToast?.Invoke(new ToastMessage
        {
            Type = ToastType.Info,
            Title = title,
            Message = message
        });
    }
}

public class ToastMessage
{
    public ToastType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
}

public enum ToastType
{
    Success,
    Error,
    Info
}

