using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services;

public class BookingDialogRequest
{
    public Guid TemplateId { get; init; }
    public Guid PlaceId { get; init; }
    public Guid? AreaId { get; init; }
    public Guid? GroupId { get; init; }
    public int DayOfWeek { get; init; }
    public int StartMin { get; init; }
    public int EndMin { get; init; }
}

public class BookingDialogService
{
    public event Action<BookingDialogRequest>? OnOpen;

    public void Open(BookingDialogRequest req) => OnOpen?.Invoke(req);
}


