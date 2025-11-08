using SchedulerMVP.Data.Entities;
using System.Threading;

namespace SchedulerMVP.Services;

public class UIState
{
    public event Action? OnChanged;
    public event Action? OnTestOpenModal;
    
    // Removed debouncing - caused threading issues (Dispatcher crash)
    // Components handle state changes correctly without debouncing
    
    private Guid? _selectedPlaceId;
    private Guid? _selectedAreaId;
    private Guid? _selectedGroupId;
    private Guid? _selectedTemplateId;
    private bool _isDayView = false;
    
    public Guid? SelectedPlaceId
    {
        get => _selectedPlaceId;
        set
        {
            if (_selectedPlaceId != value)
            {
                _selectedPlaceId = value;
                OnChanged?.Invoke();
            }
        }
    }
    
    public Guid? SelectedAreaId
    {
        get => _selectedAreaId;
        set
        {
            if (_selectedAreaId != value)
            {
                _selectedAreaId = value;
                OnChanged?.Invoke();
            }
        }
    }
    
    public Guid? SelectedGroupId
    {
        get => _selectedGroupId;
        set
        {
            if (_selectedGroupId != value)
            {
                _selectedGroupId = value;
                OnChanged?.Invoke();
            }
        }
    }
    
    public Guid? SelectedTemplateId
    {
        get => _selectedTemplateId;
        set
        {
            if (_selectedTemplateId != value)
            {
                _selectedTemplateId = value;
                OnChanged?.Invoke();
            }
        }
    }
    
    public bool ShouldOpenBookingModal { get; set; }
    public bool ShouldOpenCopyTemplateModal { get; set; }
    public bool ShouldOpenPlaceCreateModal { get; set; }
    
    private bool _isGroupViewMode = false;
    private Guid? _groupViewGroupId = null;
    
    // Calendar view mode
    private bool _isCalendarViewMode = false;
    private DateOnly _currentWeekStart = GetCurrentWeekStart();
    private string _pageTitle = "Veckoschema";
    private Guid? _filteredGroupId = null;
    private bool _isAdminPage = false;
    private Guid? _savedPlaceIdForGroupFilter;
    private Guid? _savedAreaIdForGroupFilter;
    
    public bool IsGroupViewMode
    {
        get => _isGroupViewMode;
        set
        {
            if (_isGroupViewMode != value)
            {
                _isGroupViewMode = value;
                if (!value)
                {
                    _groupViewGroupId = null;
                }
                OnChanged?.Invoke();
            }
        }
    }
    
    public Guid? GroupViewGroupId
    {
        get => _groupViewGroupId;
        set
        {
            if (_groupViewGroupId != value)
            {
                _groupViewGroupId = value;
                _isGroupViewMode = value.HasValue;
                OnChanged?.Invoke();
            }
        }
    }
    
    public void RaiseChanged() => OnChanged?.Invoke();
    
    public void NotifyChanged() => OnChanged?.Invoke();
    
    public void TestOpenModal()
    {
        OnTestOpenModal?.Invoke();
    }
    
    public bool IsCalendarViewMode
    {
        get => _isCalendarViewMode;
        set
        {
            if (_isCalendarViewMode != value)
            {
                _isCalendarViewMode = value;
                OnChanged?.Invoke();
            }
        }
    }
    
    public bool IsDayView
    {
        get => _isDayView;
        set
        {
            if (_isDayView != value)
            {
                _isDayView = value;
                OnChanged?.Invoke();
            }
        }
    }
    
    public DateOnly CurrentWeekStart
    {
        get => _currentWeekStart;
        set
        {
            if (_currentWeekStart != value)
            {
                _currentWeekStart = value;
                OnChanged?.Invoke();
            }
        }
    }
    
    public DateOnly CurrentWeekEnd => _currentWeekStart.AddDays(6);
    
    public string PageTitle
    {
        get => _pageTitle;
        set
        {
            if (_pageTitle != value)
            {
                _pageTitle = value;
                OnChanged?.Invoke();
            }
        }
    }

    public bool IsAdminPage
    {
        get => _isAdminPage;
        set
        {
            if (_isAdminPage != value)
            {
                _isAdminPage = value;
                OnChanged?.Invoke();
            }
        }
    }
    
    public Guid? FilteredGroupId
    {
        get => _filteredGroupId;
        set
        {
            if (_filteredGroupId != value)
            {
                var enteringGroupFilter = !_filteredGroupId.HasValue && value.HasValue;
                var exitingGroupFilter = _filteredGroupId.HasValue && !value.HasValue;

                _filteredGroupId = value;

                if (enteringGroupFilter)
                {
                    _savedPlaceIdForGroupFilter = SelectedPlaceId;
                    _savedAreaIdForGroupFilter = SelectedAreaId;

                    if (SelectedAreaId.HasValue)
                    {
                        SelectedAreaId = null;
                    }

                    if (SelectedPlaceId.HasValue)
                    {
                        SelectedPlaceId = null;
                    }
                }
                else if (exitingGroupFilter)
                {
                    if (_savedPlaceIdForGroupFilter.HasValue)
                    {
                        SelectedPlaceId = _savedPlaceIdForGroupFilter;
                    }

                    if (_savedAreaIdForGroupFilter.HasValue)
                    {
                        SelectedAreaId = _savedAreaIdForGroupFilter;
                    }

                    _savedPlaceIdForGroupFilter = null;
                    _savedAreaIdForGroupFilter = null;
                }

                OnChanged?.Invoke();
            }
        }
    }
    
    public bool IsGroupFilterActive => _filteredGroupId.HasValue;
    
    public void NavigateToPreviousWeek()
    {
        CurrentWeekStart = _currentWeekStart.AddDays(-7);
    }
    
    public void NavigateToNextWeek()
    {
        CurrentWeekStart = _currentWeekStart.AddDays(7);
    }
    
    public void NavigateToCurrentWeek()
    {
        CurrentWeekStart = GetCurrentWeekStart();
    }
    
    public void NavigateToPreviousDay()
    {
        CurrentWeekStart = _currentWeekStart.AddDays(-1);
    }
    
    public void NavigateToNextDay()
    {
        CurrentWeekStart = _currentWeekStart.AddDays(1);
    }
    
    public void NavigateToCurrentDay()
    {
        CurrentWeekStart = DateOnly.FromDateTime(DateTime.Today);
    }
    
    private static DateOnly GetCurrentWeekStart()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7; // Convert Sunday=0 to Monday=0
        return today.AddDays(-daysSinceMonday);
    }

    // In-memory clipboard for week copy/paste in calendar view
    public List<CalendarBooking>? ClipboardWeek { get; set; }
    public DateOnly? ClipboardWeekStart { get; set; }
    
    // Force refresh flag - when set to true, WeekGrid will reload data even if state hasn't changed
    // Used when bookings are created/updated to ensure they appear immediately
    public bool ForceRefresh { get; set; } = false;
}


