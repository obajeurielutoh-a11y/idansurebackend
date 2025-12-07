using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Domain.Entities;
namespace SubscriptionSystem.Application.Services
{
    public class PredictionService : IPredictionService
    {
    private readonly IPredictionRepository _predictionRepository;
    private readonly IDomainEventPublisher _eventPublisher;

        public PredictionService(IPredictionRepository predictionRepository, IDomainEventPublisher eventPublisher)
        {
            _predictionRepository = predictionRepository;
            _eventPublisher = eventPublisher;
        }
        public async Task<ServiceResult<bool>> UpdatePredictionOutcomeAsync(string id, Domain.Entities.MatchOutcome outcome)
        {
            var existingPrediction = await _predictionRepository.GetPredictionByIdAsync(id);
            if (existingPrediction == null)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "Prediction not found." };
            }

            if (outcome != Domain.Entities.MatchOutcome.Win && outcome != Domain.Entities.MatchOutcome.Loss)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "Invalid outcome. Must be Win or Loss." };
            }

            existingPrediction.Outcome = outcome;
            await _predictionRepository.UpdatePredictionAsync(existingPrediction);
            return new ServiceResult<bool> { IsSuccess = true, Data = true };
        }

        public async Task<ServiceResult<string>> CreateDetailedPredictionAsync(DetailedPredictionDto predictionDto)
        {
            var prediction = new Prediction
            {
                Tournament = predictionDto.Tournament,
                Team1 = predictionDto.Team1,
                Team2 = predictionDto.Team2,
                MatchDate = predictionDto.MatchDate,
                MatchDetails = predictionDto.MatchDetails,
                NonAlphanumericDetails = string.IsNullOrEmpty(predictionDto.NonAlphanumericDetails) ? "Default Value" : predictionDto.NonAlphanumericDetails,
                IsDetailed = true,
                IsPromotional = false,
                CreatedAt = DateTime.UtcNow,
                Outcome = Domain.Entities.MatchOutcome.Pending, // Set initial outcome to Pending
                Team1Performance = new TeamPerformance
                {
                    RecentWins = predictionDto.Team1Performance.RecentWins,
                    RecentLosses = predictionDto.Team1Performance.RecentLosses,
                    AverageGoalsScored = predictionDto.Team1Performance.AverageGoalsScored,
                    AverageGoalsConceded = predictionDto.Team1Performance.AverageGoalsConceded,
                    KeyPlayersStatus = predictionDto.Team1Performance.KeyPlayersStatus
                },
                Team2Performance = new TeamPerformance
                {
                    RecentWins = predictionDto.Team2Performance.RecentWins,
                    RecentLosses = predictionDto.Team2Performance.RecentLosses,
                    AverageGoalsScored = predictionDto.Team2Performance.AverageGoalsScored,
                    AverageGoalsConceded = predictionDto.Team2Performance.AverageGoalsConceded,
                    KeyPlayersStatus = predictionDto.Team2Performance.KeyPlayersStatus
                },
                ConfidenceLevel = predictionDto.ConfidenceLevel,
                PredictedOutcome = predictionDto.PredictedOutcome
            };

            var result = await _predictionRepository.CreatePredictionAsync(prediction);
            await _eventPublisher.PublishAsync(new SubscriptionSystem.Domain.Events.TipPostedEvent(
                prediction.Id,
                prediction.IsDetailed,
                prediction.IsPromotional,
                prediction.MatchDate,
                prediction.Tournament ?? string.Empty,
                prediction.Team1 ?? string.Empty,
                prediction.Team2 ?? string.Empty
            ));
            return new ServiceResult<string> { IsSuccess = true, Data = result };
        }

        public async Task<ServiceResult<string>> CreateSimplePredictionAsync(SimplePredictionDto predictionDto)
        {
            var prediction = new Prediction
            {
                MatchDetails = predictionDto.AlphanumericPrediction,
                IsDetailed = false,
                IsPromotional = predictionDto.IsPromotional,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _predictionRepository.CreatePredictionAsync(prediction);
            await _eventPublisher.PublishAsync(new SubscriptionSystem.Domain.Events.TipPostedEvent(
                prediction.Id,
                prediction.IsDetailed,
                prediction.IsPromotional,
                prediction.MatchDate,
                prediction.Tournament ?? string.Empty,
                prediction.Team1 ?? string.Empty,
                prediction.Team2 ?? string.Empty
            ));
            return new ServiceResult<string> { IsSuccess = true, Data = result };
        }

        public async Task<PaginatedResult<PredictionResponseDto>> GetPredictionsAsync(int page, int pageSize)
        {
            var (predictions, totalCount) = await _predictionRepository.GetPredictionsAsync(page, pageSize);

            var predictionDtos = predictions.Select(p => new PredictionResponseDto
            {
                Id = p.Id,
                Tournament = p.Tournament,
                Team1 = p.Team1,
                Team2 = p.Team2,
                MatchDate = p.MatchDate,
                MatchDetails = p.MatchDetails,
                NonAlphanumericDetails = p.NonAlphanumericDetails ?? string.Empty,
                IsDetailed = p.IsDetailed,
                IsPromotional = p.IsPromotional,
                CreatedAt = p.CreatedAt,
          
            }).ToList();

            return new PaginatedResult<PredictionResponseDto>
            {
                Items = predictionDtos,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        public async Task<PaginatedResult<PastPredictionDto>> GetPastPredictionsAsync(int page, int pageSize)
        {
            var (predictions, totalCount) = await _predictionRepository.GetPastPredictionsAsync(page, pageSize);

            var predictionDtos = predictions.Select(p => new PastPredictionDto
            {
                Id = p.Id,
                Tournament = p.Tournament,
                Team1 = p.Team1,
                Team2 = p.Team2,
                MatchDate = p.MatchDate,
                MatchDetails = p.MatchDetails,
                IsDetailed = p.IsDetailed,
                IsPromotional = p.IsPromotional,
                Outcome = (Application.DTOs.MatchOutcome?)p.Outcome,
                CreatedAt = p.CreatedAt
            }).ToList();

            return new PaginatedResult<PastPredictionDto>
            {
                Items = predictionDtos,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        //public async Task<ServiceResult<bool>> AddMatchResultAsync(MatchResultDto resultDto)
        //{
        //    var prediction = await _predictionRepository.GetPredictionByIdAsync(resultDto.PredictionId);
        //    if (prediction == null)
        //        return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "Prediction not found." };

        //    prediction.Outcome = (Domain.Entities.MatchOutcome)resultDto.Outcome;
        //    await _predictionRepository.UpdatePredictionAsync(prediction);

        //    return new ServiceResult<bool> { IsSuccess = true, Data = true };
        //}
        public async Task<ServiceResult<bool>> AddMatchResultAsync(MatchResultDto resultDto)
        {
            var prediction = await _predictionRepository.GetPredictionByIdAsync(resultDto.PredictionId);
            if (prediction == null)
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "Prediction not found." };

            prediction.Outcome = (Domain.Entities.MatchOutcome)resultDto.Outcome;
            await _predictionRepository.UpdatePredictionAsync(prediction);

            return new ServiceResult<bool> { IsSuccess = true, Data = true };
        }

        public async Task<PredictionStatisticsDto> GetPredictionStatisticsAsync()
        {
            var stats = await _predictionRepository.GetPredictionStatisticsAsync();

            return new PredictionStatisticsDto
            {
                TotalPredictions = stats.TotalPredictions,
                WinningPredictions = stats.WinningPredictions,
                LosingPredictions = stats.LosingPredictions,
                WinPercentage = stats.TotalPredictions > 0
                    ? (double)stats.WinningPredictions / stats.TotalPredictions * 100
                    : 0
            };
        }
        public async Task<ServiceResult<bool>> UpdatePredictionAsync(string id, DetailedPredictionDto predictionDto)
        {
            var existingPrediction = await _predictionRepository.GetPredictionByIdAsync(id);
            if (existingPrediction == null)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "Prediction not found." };
            }

            UpdatePredictionFromDto(existingPrediction, predictionDto);

            await _predictionRepository.UpdatePredictionAsync(existingPrediction);
            return new ServiceResult<bool> { IsSuccess = true, Data = true };
        }

     
        public async Task<ServiceResult<bool>> DeletePredictionAsync(string id)
        {
            var existingPrediction = await _predictionRepository.GetPredictionByIdAsync(id);
            if (existingPrediction == null)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "Prediction not found." };
            }

            await _predictionRepository.DeletePredictionAsync(id);
            return new ServiceResult<bool> { IsSuccess = true, Data = true };
        }

        private void UpdatePredictionFromDto(Prediction prediction, DetailedPredictionDto dto)
        {
            prediction.Tournament = dto.Tournament;
            prediction.Team1 = dto.Team1;
            prediction.Team2 = dto.Team2;
            prediction.MatchDate = dto.MatchDate;
            prediction.MatchDetails = dto.MatchDetails;
            prediction.NonAlphanumericDetails = dto.NonAlphanumericDetails ?? "Default Value";
            prediction.Team1Performance = new TeamPerformance
            {
                RecentWins = dto.Team1Performance.RecentWins,
                RecentLosses = dto.Team1Performance.RecentLosses,
                AverageGoalsScored = dto.Team1Performance.AverageGoalsScored,
                AverageGoalsConceded = dto.Team1Performance.AverageGoalsConceded,
                KeyPlayersStatus = dto.Team1Performance.KeyPlayersStatus
            };
            prediction.Team2Performance = new TeamPerformance
            {
                RecentWins = dto.Team2Performance.RecentWins,
                RecentLosses = dto.Team2Performance.RecentLosses,
                AverageGoalsScored = dto.Team2Performance.AverageGoalsScored,
                AverageGoalsConceded = dto.Team2Performance.AverageGoalsConceded,
                KeyPlayersStatus = dto.Team2Performance.KeyPlayersStatus
            };
            prediction.ConfidenceLevel = dto.ConfidenceLevel;
            prediction.PredictedOutcome = dto.PredictedOutcome;
        }

        private DetailedPredictionDto ConvertToDetailedPredictionDto(Prediction prediction)
        {
            return new DetailedPredictionDto
            {
                Tournament = prediction.Tournament,
                Team1 = prediction.Team1,
                Team2 = prediction.Team2,
                MatchDate = prediction.MatchDate,
                MatchDetails = prediction.MatchDetails,
                NonAlphanumericDetails = prediction.NonAlphanumericDetails ?? "Default Value",
                Team1Performance = new TeamPerformanceDto
                {
                    RecentWins = prediction.Team1Performance.RecentWins,
                    RecentLosses = prediction.Team1Performance.RecentLosses,
                    AverageGoalsScored = prediction.Team1Performance.AverageGoalsScored,
                    AverageGoalsConceded = prediction.Team1Performance.AverageGoalsConceded,
                    KeyPlayersStatus = prediction.Team1Performance.KeyPlayersStatus
                },
                Team2Performance = new TeamPerformanceDto
                {
                    RecentWins = prediction.Team2Performance.RecentWins,
                    RecentLosses = prediction.Team2Performance.RecentLosses,
                    AverageGoalsScored = prediction.Team2Performance.AverageGoalsScored,
                    AverageGoalsConceded = prediction.Team2Performance.AverageGoalsConceded,
                    KeyPlayersStatus = prediction.Team2Performance.KeyPlayersStatus
                },
                ConfidenceLevel = prediction.ConfidenceLevel,
                PredictedOutcome = prediction.PredictedOutcome
            };
        }
    }
}

