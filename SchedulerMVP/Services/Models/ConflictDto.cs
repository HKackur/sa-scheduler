namespace SchedulerMVP.Services.Models;

public record ConflictDto(
    string OtherGroupName,
    string OtherAreaName,
    int DayOfWeek,
    int StartMin,
    int EndMin
);

public record BookingCandidate(Guid AreaId, Guid GroupId, int DayOfWeek, int StartMin, int EndMin);


