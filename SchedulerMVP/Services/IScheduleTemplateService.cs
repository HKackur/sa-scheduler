using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services;

public interface IScheduleTemplateService
{
    Task<List<ScheduleTemplate>> GetTemplatesForPlaceAsync(Guid placeId);
    Task<ScheduleTemplate?> GetByIdAsync(Guid templateId);
    Task<ScheduleTemplate> CreateAsync(Guid placeId, string name);
    Task<ScheduleTemplate> SaveAsNewAsync(Guid sourceTemplateId, string newName);
    Task<ScheduleTemplate> UpdateTemplateNameAsync(Guid templateId, string name);
    Task<BookingTemplate> CreateBookingAsync(Guid templateId, Guid areaId, Guid groupId, int dayOfWeek, int startMin, int endMin, string? notes);
    Task<BookingTemplate> UpdateBookingAsync(Guid bookingId, int? dayOfWeek, int? startMin, int? endMin, Guid? areaId, Guid? groupId, string? notes);
    Task DeleteBookingAsync(Guid bookingId);
    Task<BookingTemplate> DuplicateBookingAsync(Guid bookingId);
    Task DeleteTemplateAsync(Guid templateId);
    Task<List<ScheduleTemplate>> GetTemplatesAsync();
    Task<ScheduleTemplate> CreateGlobalAsync(string name);
    Task<ScheduleTemplate> CreateForUserAsync(string ownerUserId, string name);
}


