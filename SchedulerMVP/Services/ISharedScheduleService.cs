using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services;

public interface ISharedScheduleService
{
    Task<SharedScheduleLink?> GetByTokenAsync(string token);
    Task<SharedScheduleLink?> GetByTemplateIdAsync(Guid templateId);
    Task<SharedScheduleLink> CreateOrUpdateAsync(Guid templateId);
    Task<SharedScheduleLink> UpdateViewSettingsAsync(Guid templateId, bool allowWeekView, bool allowDayView, bool allowListView);
    Task ToggleActiveAsync(Guid templateId);
    Task DeleteAsync(Guid templateId);
    Task<string> GenerateShareTokenAsync(); // Kryptografiskt säker
}
