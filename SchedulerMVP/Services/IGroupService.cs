using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services;

public interface IGroupService
{
    Task<List<Group>> GetGroupsAsync();
    Task<Group?> GetGroupAsync(Guid id);
    Task<Group> CreateGroupAsync(Group group);
    Task<Group> UpdateGroupAsync(Group group);
    Task DeleteGroupAsync(Guid groupId);
}

