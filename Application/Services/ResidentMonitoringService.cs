using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Application.Interfaces;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Application.Services
{
    public class ResidentMonitoringService : IResidentMonitoringService
    {
        private static readonly DateTime TrackingStartDate = new DateTime(2026, 1, 1);
        private readonly IResidentMonitoringRepository _repository;
        private readonly IProjectResidentRepository _projecResidentRepository;
        private readonly IUserRepository _userRepository;
        private const int ExpectedIvtFiles = 4;
        private const int ExpectedConstructionLogFiles = 4;
        public ResidentMonitoringService(
            IResidentMonitoringRepository repository,
            IProjectResidentRepository projecResidentRepository,
            IUserRepository userRepository
            )
        {
            _repository = repository;
            _projecResidentRepository = projecResidentRepository;
            _userRepository = userRepository;
        }

        public async Task<TrackingResultDto> GetTrackingAsync(TrackingQueryDto query)
        {
            var rawData = await _repository.GetTrackingDataAsync(
                query.ProjectId,
                query.ResidentUserId,
                query.Month,
                query.Year
            );

            bool isAllPeriods = !query.Month.HasValue || !query.Year.HasValue;

            int monthCount = isAllPeriods
                ? CountMonthsFromStart()
                : 1;

            var items = rawData.Select(r => new TrackingItemDto
            {
                ProjectId = r.ProjectId,
                ProjectDescription = r.ProjectDescription,
                ResidentUserId = r.ResidentUserId,
                ResidentFullName = r.ResidentFullName,
                Month = query.Month,
                Year = query.Year,
                IsAllPeriods = isAllPeriods,

                Schedule = new TrackingScheduleDto
                {
                    Expected = monthCount,
                    Reported = r.ScheduleReportedCount,
                    Completed = r.ScheduleReportedCount >= monthCount
                },

                Ivts = new TrackingFileDto
                {
                    Expected = ExpectedIvtFiles * monthCount,
                    Uploaded = r.IvtsUploaded
                },

                ConstructionLog = new TrackingFileDto
                {
                    Expected = ExpectedConstructionLogFiles * monthCount,
                    Uploaded = r.ConstructionLogsUploaded
                },

                Incidences = new TrackingIncidencesDto
                {
                    Total = r.TotalIncidences,
                    Answered = r.AnsweredIncidences
                },

                CompliancePercentage = CalculateCompliance(
                    r.ScheduleReportedCount,
                    r.IvtsUploaded,
                    r.ConstructionLogsUploaded,
                    r.TotalIncidences,
                    r.AnsweredIncidences,
                    monthCount
                )
            }).ToList();

            return new TrackingResultDto
            {
                Items = items,
                Summary = CalculateSummary(items)
            };
        }

        private static int CountMonthsFromStart()
        {
            var now = DateTime.Now;
            return ((now.Year - TrackingStartDate.Year) * 12)
                   + now.Month - TrackingStartDate.Month + 1;
        }

        public async Task<TrackingFiltersDto> GetFilters()
        {
            var projects = await _projecResidentRepository.GetProjectsDescription();
            var residents = await _userRepository.GetResidentsFullName();

            return new TrackingFiltersDto
            {
                Projects = projects,
                Residents = residents,
            };
        }

        private static TrackingSummaryDto CalculateSummary(List<TrackingItemDto> items)
        {
            if (!items.Any())
                return new TrackingSummaryDto();

            return new TrackingSummaryDto
            {
                AverageCompliance = Math.Round(items.Average(i => i.CompliancePercentage), 2),

                FullyCompleted = items.Count(i => i.CompliancePercentage == 100),

                CriticalPending = items.Count(i => i.CompliancePercentage <= 49),

                PendingDeliverables = items.Sum(i =>
                    Math.Max(0, i.Schedule.Expected - i.Schedule.Reported)
                    + Math.Max(0, i.Ivts.Expected - i.Ivts.Uploaded)
                    + Math.Max(0, i.ConstructionLog.Expected - i.ConstructionLog.Uploaded)
                    + (i.Incidences.NoPending ? 0 : Math.Max(0, i.Incidences.Total - i.Incidences.Answered))
                )
            };
        }

        private static decimal CalculateCompliance(
            int scheduleReportedCount,
            int ivtsUploaded,
            int constructionLogsUploaded,
            int totalIncidences,
            int answeredIncidences,
            int monthCount)
        {
            int expectedIvts = ExpectedIvtFiles * monthCount;
            int expectedCuaderno = ExpectedConstructionLogFiles * monthCount;

            bool hasIncidences = totalIncidences > 0;

            decimal weight = hasIncidences ? 25m : 100m / 3m;

            decimal points = 0;

            points += weight * Math.Min(scheduleReportedCount, monthCount) / monthCount;

            if (expectedIvts > 0)
                points += weight * Math.Min(ivtsUploaded, expectedIvts) / expectedIvts;

            if (expectedCuaderno > 0)
                points += weight * Math.Min(constructionLogsUploaded, expectedCuaderno) / expectedCuaderno;

            if (hasIncidences)
                points += 25m * answeredIncidences / totalIncidences;

            return Math.Round(points, 2);
        }
    }
}