using Abril_Backend.Infrastructure.Repositories;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Dtos;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Interfaces;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Services
{
    public class MilestoneScheduleService : IMilestoneScheduleService
    {
        private readonly IMilestoneScheduleRepository _repository;
        private readonly MilestoneRepository _milestoneRepository;

        public MilestoneScheduleService(
            IMilestoneScheduleRepository repository,
            MilestoneRepository milestoneRepository)
        {
            _repository = repository;
            _milestoneRepository = milestoneRepository;
        }

        public Task<List<MilestoneScheduleDTO>> GetAllByMilestoneScheduleHistoryId(int milestoneScheduleHistoryId)
            => _repository.GetAllByMilestoneScheduleHistoryIdFactory(milestoneScheduleHistoryId);

        public async Task<List<MilestoneScheduleFakeDataDTO>> BuildFakeSchedule()
        {
            var milestones = await _milestoneRepository.GetAllFactorySimple();

            var fakeScheduleConfig = new Dictionary<int, (DateTime start, DateTime? end)>
            {
                { 1,  (new DateTime(2025, 12, 22), null) },
                { 2,  (new DateTime(2025, 12, 22), new DateTime(2026, 4, 7)) },
                { 3,  (new DateTime(2026, 3, 28), new DateTime(2026, 5, 26)) },
                { 4,  (new DateTime(2026, 1, 30), null) },
                { 5,  (new DateTime(2026, 2, 20), null) },
                { 6,  (new DateTime(2026, 5, 21), new DateTime(2026, 7, 7)) },
                { 7,  (new DateTime(2026, 7, 2), null) },
                { 8,  (new DateTime(2026, 7, 7), new DateTime(2026, 11, 7)) },
                { 9,  (new DateTime(2026, 11, 10), null) },
                { 10, (new DateTime(2026, 6, 21), new DateTime(2026, 9, 3)) },
                { 11, (new DateTime(2026, 7, 21), new DateTime(2026, 10, 14)) },
                { 12, (new DateTime(2026, 8, 21), new DateTime(2026, 10, 31)) },
                { 13, (new DateTime(2026, 10, 7), new DateTime(2026, 12, 15)) },
                { 14, (new DateTime(2026, 9, 7), new DateTime(2027, 2, 6)) },
                { 15, (new DateTime(2026, 10, 7), new DateTime(2026, 10, 31)) },
                { 16, (new DateTime(2026, 10, 7), new DateTime(2027, 4, 12)) },
                { 17, (new DateTime(2026, 12, 7), new DateTime(2027, 1, 30)) },
                { 18, (new DateTime(2026, 10, 7), new DateTime(2027, 5, 3)) },
                { 19, (new DateTime(2026, 12, 28), new DateTime(2027, 3, 30)) },
                { 20, (new DateTime(2027, 4, 7), new DateTime(2027, 4, 30)) },
                { 21, (new DateTime(2027, 5, 3), null) }
            };

            int order = 1;
            return milestones
                .Where(m => fakeScheduleConfig.ContainsKey(m.MilestoneId))
                .Select(m =>
                {
                    var config = fakeScheduleConfig[m.MilestoneId];
                    return new MilestoneScheduleFakeDataDTO
                    {
                        MilestoneId = m.MilestoneId,
                        MilestoneDescription = m.MilestoneDescription,
                        PlannedStartDate = config.start,
                        PlannedEndDate = config.end,
                        Order = order++
                    };
                })
                .OrderBy(x => x.MilestoneId)
                .ToList();
        }

        public Task CulminarAsync(int milestoneScheduleId, DateOnly? fechaRealFin, int userId)
            => _repository.CulminarAsync(milestoneScheduleId, fechaRealFin, userId);

        public Task MarcarCriticoAsync(int milestoneScheduleId, bool esHitoCritico, int userId)
            => _repository.MarcarCriticoAsync(milestoneScheduleId, esHitoCritico, userId);
    }
}
