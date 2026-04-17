using Microsoft.EntityFrameworkCore;
using Prode.Domain.Entities;
using Prode.Application.Interfaces;
using Prode.Infrastructure.Data;
using Prode.Application.DTOs;

namespace Prode.Infrastructure.Repositories
{
    public class PredictionRepository : IPredictionRepository
    {
        private readonly ApplicationDbContext _context;

        public PredictionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Prediction?> GetUserPredictionAsync(Match match, string? userId)
        {
            return await _context.Predictions
                .FirstOrDefaultAsync(p => p.User != null && p.User.Id == userId && p.Match == match);
        }

        public async Task<PredictionStatsDto> GetPredictionStatsAsync(Match match)
        {
            var stats = await _context.Predictions
                .Where(p => p.Match == match) // 🔥 importante usar FK, no navegación
                .GroupBy(p => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    HomeWins = g.Count(p => p.HomeGoals > p.AwayGoals),
                    Draws = g.Count(p => p.HomeGoals == p.AwayGoals),
                    AwayWins = g.Count(p => p.HomeGoals < p.AwayGoals)
                })
                .FirstOrDefaultAsync();

            if (stats == null || stats.Total == 0)
            {
                return new PredictionStatsDto
                {
                    HomeWinPercentage = 0,
                    DrawPercentage = 0,
                    AwayWinPercentage = 0
                };
            }

            return new PredictionStatsDto
            {
                HomeWinPercentage = (float)Math.Round((double)stats.HomeWins / stats.Total * 100, 1),
                DrawPercentage = (float)Math.Round((double)stats.Draws / stats.Total * 100, 1),
                AwayWinPercentage = (float)Math.Round((double)stats.AwayWins / stats.Total * 100, 1)
            };
        }

        public async Task<Prediction?> GetPredictionByIdAsync(Guid id)
        {
            return await _context.Predictions
                .Include(p => p.Match)
                .Include(p => p.ResultType)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Prediction?> GetUserPredictionByMatchIdAsync(Guid matchId, string userId)
        {
            return await _context.Predictions
                .Include(p => p.Match)
                .FirstOrDefaultAsync(p => p.MatchId == matchId && p.User != null && p.User.Id == userId);
        }

        public async Task<Prediction> CreatePredictionAsync(Prediction prediction)
        {
            prediction.Id = Guid.NewGuid();
            prediction.CreatedAt = DateTime.UtcNow;
            prediction.UpdatedAt = DateTime.UtcNow;
            _context.Predictions.Add(prediction);
            await _context.SaveChangesAsync();
            return prediction;
        }

        public async Task<Prediction> UpdatePredictionAsync(Prediction prediction)
        {
            prediction.UpdatedAt = DateTime.UtcNow;
            _context.Predictions.Update(prediction);
            await _context.SaveChangesAsync();
            return prediction;
        }

        public async Task<bool> DeletePredictionAsync(Guid id)
        {
            var prediction = await _context.Predictions.FindAsync(id);
            if (prediction == null)
            {
                return false;
            }

            _context.Predictions.Remove(prediction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Prediction>> GetPredictionsWithoutResultTypeAsync(string userId)
        {
            return await _context.Predictions
                .Include(p => p.Match)
                .Include(p => p.ResultType)
                .Where(p => p.UserId == userId 
                           && p.ResultType == null 
                           && p.Match.HomeScore.HasValue 
                           && p.Match.AwayScore.HasValue)
                .ToListAsync();
        }

        public async Task<List<Prediction>> GetPredictionsWithoutResultTypeForFinishedMatchesAsync()
        {
            // Obtener todas las predicciones sin ResultType de partidos finalizados en una sola consulta
            return await _context.Predictions
                .Include(p => p.Match)
                .Include(p => p.User)
                .Include(p => p.ResultType)
                .Where(p => p.ResultType == null
                           && p.Match.HomeScore.HasValue
                           && p.Match.AwayScore.HasValue)
                .ToListAsync();
        }

        public async Task<List<Prediction>> GetPredictionsWithResultTypeWithoutPostAsync()
        {
            // Obtener predicciones que tienen ResultType pero no tienen post asociado
            // Usamos un LEFT JOIN con Posts y filtramos donde el post es NULL
            var predictionsWithResultType = await _context.Predictions
                .Include(p => p.Match)
                .Include(p => p.User)
                .Include(p => p.ResultType)
                .Where(p => p.ResultType != null)
                .ToListAsync();

            // Filtramos las que no tienen post (no podemos hacer el LEFT JOIN directamente en EF Core de manera fácil)
            var predictionIds = predictionsWithResultType.Select(p => p.Id).ToList();
            
            // Obtenemos los IDs de predicciones que ya tienen post
            var predictionIdsWithPost = await _context.Posts
                .Where(p => p.PredictionId != null)
                .Select(p => p.PredictionId!.Value)
                .ToListAsync();

            // Filtramos las predicciones que no tienen post
            return predictionsWithResultType
                .Where(p => !predictionIdsWithPost.Contains(p.Id))
                .ToList();
        }

        public async Task<List<ResultType>> GetAllResultTypesAsync()
        {
            return await _context.ResultTypes.ToListAsync();
        }

        public async Task UpdateUserTotalPointsAsync(string userId, int pointsToAdd)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.TotalPoints = (user.TotalPoints ?? 0) + pointsToAdd;
                await _context.SaveChangesAsync();
            }
        }
    }
}
