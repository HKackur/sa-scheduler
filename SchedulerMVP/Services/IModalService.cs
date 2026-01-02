using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services;

public interface IModalService
{
    Task<List<Modal>> GetAllModalsAsync();
    Task<Modal?> GetModalByIdAsync(Guid id);
    Task<Modal> CreateModalAsync(Modal modal);
    Task<Modal> UpdateModalAsync(Modal modal);
    Task DeleteModalAsync(Guid id);
    Task<List<Modal>> GetActiveModalsForUserAsync(string userId);
    Task MarkModalAsReadAsync(Guid modalId, string userId);
    Task<int> GetReadCountAsync(Guid modalId);
}

