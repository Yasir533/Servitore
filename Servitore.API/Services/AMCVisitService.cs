using Servitore.API.Repositories;
using Servitore.Database.Entities;

namespace Servitore.API.Services;

public interface IAMCVisitService
{
    Task<AMCVisit?> GetByIdAsync(int id);
    Task<List<AMCVisit>> GetByContractIdAsync(int contractId);
    Task<AMCVisit> AddVisitAsync(int contractId, AMCVisit visit);
    Task UpdateVisitAsync(int visitId, AMCVisit visit);
    Task DeleteVisitAsync(int visitId);
}

public class AMCVisitService : IAMCVisitService
{
    private readonly IAMCVisitRepository _visitRepository;

    public AMCVisitService(IAMCVisitRepository visitRepository)
    {
        _visitRepository = visitRepository;
    }

    public Task<AMCVisit?> GetByIdAsync(int id) => _visitRepository.GetByIdAsync(id);

    public Task<List<AMCVisit>> GetByContractIdAsync(int contractId) => _visitRepository.GetByContractIdAsync(contractId);

    public async Task<AMCVisit> AddVisitAsync(int contractId, AMCVisit visit)
    {
        visit.AMCContractId = contractId;
        // Schedule date defaults to today if not provided
        if (visit.ScheduledDate == default)
        {
            visit.ScheduledDate = DateTime.UtcNow;
        }
        return await _visitRepository.AddAsync(visit);
    }

    public async Task UpdateVisitAsync(int visitId, AMCVisit visit)
    {
        var existing = await _visitRepository.GetByIdAsync(visitId)
            ?? throw new KeyNotFoundException($"AMC Visit with ID {visitId} not found.");

        existing.ScheduledDate = visit.ScheduledDate;
        existing.VisitDate = visit.VisitDate;
        existing.Status = visit.Status;
        existing.Remarks = visit.Remarks;
        existing.EngineerId = visit.EngineerId;

        await _visitRepository.UpdateAsync(existing);
    }

    public async Task DeleteVisitAsync(int visitId)
    {
        await _visitRepository.DeleteAsync(visitId);
    }
}
