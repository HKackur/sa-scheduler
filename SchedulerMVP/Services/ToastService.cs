using System.Collections.Concurrent;

namespace SchedulerMVP.Services;

public class ToastService
{
    private readonly ConcurrentDictionary<Guid, ToastMessage> _toasts = new();
    public event Action? OnToastsChanged;
    
    public IReadOnlyList<ToastMessage> GetToasts()
    {
        return _toasts.Values.ToList().AsReadOnly();
    }
    
    private void NotifyChanged()
    {
        // Invoke event - subscribers will handle thread synchronization
        OnToastsChanged?.Invoke();
    }
    
    public void ShowSuccess(string title, string? message = null)
    {
        var toast = new ToastMessage
        {
            Type = ToastType.Success,
            Title = title,
            Message = message
        };
        
        _toasts.TryAdd(toast.Id, toast);
        NotifyChanged();
        
        // Auto-dismiss after 5 seconds - use Timer for better control
        var timer = new System.Threading.Timer(_ =>
        {
            if (_toasts.TryRemove(toast.Id, out ToastMessage? removed))
            {
                NotifyChanged();
            }
        }, null, 5000, Timeout.Infinite);
    }
    
    public void ShowError(string title, string? message = null)
    {
        var toast = new ToastMessage
        {
            Type = ToastType.Error,
            Title = title,
            Message = message
        };
        
        _toasts.TryAdd(toast.Id, toast);
        NotifyChanged();
        
        // Auto-dismiss after 5 seconds
        var timer = new System.Threading.Timer(_ =>
        {
            if (_toasts.TryRemove(toast.Id, out ToastMessage? removed))
            {
                NotifyChanged();
            }
        }, null, 5000, Timeout.Infinite);
    }
    
    public void ShowInfo(string title, string? message = null)
    {
        var toast = new ToastMessage
        {
            Type = ToastType.Info,
            Title = title,
            Message = message
        };
        
        _toasts.TryAdd(toast.Id, toast);
        NotifyChanged();
        
        // Auto-dismiss after 5 seconds
        var timer = new System.Threading.Timer(_ =>
        {
            if (_toasts.TryRemove(toast.Id, out ToastMessage? removed))
            {
                NotifyChanged();
            }
        }, null, 5000, Timeout.Infinite);
    }
    
    public void DismissToast(Guid toastId)
    {
        _toasts.TryRemove(toastId, out _);
        NotifyChanged();
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

