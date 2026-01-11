using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services;

public interface IClubService
{
    Task<List<Club>> GetClubsAsync();
    Task<Club?> GetClubAsync(Guid id);
    Task<Club> CreateClubAsync(Club club);
    Task<Club> UpdateClubAsync(Club club);
    Task DeleteClubAsync(Guid id);
    Task MigrateUserDataToClubAsync(string userId, Guid clubId);
    Task AssignUserToClubAsync(string userId, Guid clubId);
    Task<List<ApplicationUser>> GetUsersInClubAsync(Guid clubId);
}

