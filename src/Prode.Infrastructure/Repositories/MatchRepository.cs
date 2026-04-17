using Microsoft.EntityFrameworkCore;
using Prode.Domain.Entities;
using Prode.Application.Interfaces;
using Prode.Application.DTOs;
using Prode.Domain.Enums;
using System.Linq.Expressions;
using Prode.Infrastructure.Data;

namespace Prode.Infrastructure.Repositories
{
    public class MatchRepository : IMatchRepository
    {
        private readonly ApplicationDbContext _context;

        public MatchRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Match>> GetMatchesByStatusAsync(MatchFilterDto filter)
        {
            var query = _context.Matches.AsQueryable();

            // Aplicar filtro por país si se especifica
            if (!string.IsNullOrEmpty(filter.TeamNameSearch))
            {
                var search = filter.TeamNameSearch.ToLower();
                query = query.Where(m => 
                    m.HomeTeam.Name.ToLower().Contains(search) ||
                    m.AwayTeam.Name.ToLower().Contains(search)
                );
            }

            // Aplicar filtro por estado
            var now = DateTime.UtcNow;
            switch (filter.Status)
            {
                case MatchStatusFilter.Upcoming:
                    query = query.Where(m => !m.HomeScore.HasValue && !m.AwayScore.HasValue && now < m.MatchDate.AddMinutes(-filter.MinutesBeforeMatchToLock));
                    break;
                case MatchStatusFilter.InProgress:
                    query = query.Where(m => !m.HomeScore.HasValue && !m.AwayScore.HasValue && 
                        (now >= m.MatchDate || now >= m.MatchDate.AddMinutes(-filter.MinutesBeforeMatchToLock)));
                    break;
                case MatchStatusFilter.Finished:
                    query = query.Where(m => m.HomeScore.HasValue && m.AwayScore.HasValue);
                    break;
            }

            // Ordenar por fecha
            query = query.OrderBy(m => m.MatchDate);

            // Aplicar paginación
            var skip = (filter.PageNumber - 1) * filter.PageSize;
            query = query.Skip(skip).Take(filter.PageSize);

            return await query.ToListAsync();
        }

        public async Task<int> GetMatchesCountByStatusAsync(MatchFilterDto filter)
        {
            var query = _context.Matches.AsQueryable();

            // Aplicar filtro por país si se especifica
            if (!string.IsNullOrEmpty(filter.TeamNameSearch))
            {
                var search = filter.TeamNameSearch.ToLower();
                query = query.Where(m => 
                    m.HomeTeam.Name.ToLower().Contains(search) ||
                    m.AwayTeam.Name.ToLower().Contains(search)
                );
            }

            // Aplicar filtro por estado
            var now = DateTime.UtcNow;
            switch (filter.Status)
            {
                case MatchStatusFilter.Upcoming:
                    query = query.Where(m => !m.HomeScore.HasValue && !m.AwayScore.HasValue && now < m.MatchDate);
                    break;
                case MatchStatusFilter.InProgress:
                    query = query.Where(m => !m.HomeScore.HasValue && !m.AwayScore.HasValue && 
                        (now >= m.MatchDate || now >= m.MatchDate.AddMinutes(-filter.MinutesBeforeMatchToLock)));
                    break;
                case MatchStatusFilter.Finished:
                    query = query.Where(m => m.HomeScore.HasValue && m.AwayScore.HasValue);
                    break;
            }

            return await query.CountAsync();
        }

        public async Task<Match?> GetMatchByIdAsync(Guid id)
        {
            return await _context.Matches
                .Include(m => m.HomeTeam).ThenInclude(t => t.Country)
                .Include(m => m.AwayTeam).ThenInclude(t => t.Country)
                .Include(m => m.City).ThenInclude(c => c.Country)
                .Include(m => m.Country)
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);
        }

        public async Task<Match> CreateMatchAsync(Match match)
        {
            match.Id = Guid.NewGuid();
            match.IsActive = true;
            _context.Matches.Add(match);
            await _context.SaveChangesAsync();
            return match;
        }

        public async Task<Match> UpdateMatchAsync(Match match)
        {
            _context.Matches.Update(match);
            await _context.SaveChangesAsync();
            return match;
        }

        public async Task<bool> DeleteMatchAsync(Guid id)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null)
            {
                return false;
            }

            match.IsActive = false;
            _context.Matches.Update(match);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Match> UpdateMatchScoresAsync(Guid id, int? homeScore, int? awayScore)
        {
            var match = await _context.Matches
                .Include(m => m.Predictions)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (match == null)
            {
                throw new Exception("Partido no encontrado");
            }

            match.HomeScore = homeScore;
            match.AwayScore = awayScore;
            
            // Los puntos se calculan en el BackgroundService, no aquí
            
            _context.Matches.Update(match);
            await _context.SaveChangesAsync();
            return match;
        }

        public async Task<List<Match>> GetFinishedMatchesWithPendingPredictionsAsync()
        {
            // Obtener partidos finalizados que tienen al menos una predicción sin ResultType
            return await _context.Matches
                .Where(m => m.HomeScore.HasValue && m.AwayScore.HasValue)
                .ToListAsync();
        }

        private string CalculateResultTypeName(int predictedHome, int predictedAway, int actualHome, int actualAway)
        {
            // Exacto: acertó resultado y goles exactos
            if (predictedHome == actualHome && predictedAway == actualAway)
            {
                return "Exacto";
            }
            
            // Determinar resultados
            bool predictedHomeWins = predictedHome > predictedAway;
            bool predictedAwayWins = predictedAway > predictedHome;
            bool predictedDraw = predictedHome == predictedAway;
            
            bool actualHomeWins = actualHome > actualAway;
            bool actualAwayWins = actualAway > actualHome;
            bool actualDraw = actualHome == actualAway;
            
            // Verificar si acertó el resultado
            bool acertóResultado = (predictedHomeWins && actualHomeWins) || 
                                  (predictedAwayWins && actualAwayWins) || 
                                  (predictedDraw && actualDraw);
            
            if (acertóResultado)
            {
                // Parcial Fuerte: acertó resultado y diferencia de goles
                int predictedDiff = predictedHome - predictedAway;
                int actualDiff = actualHome - actualAway;
                
                if (predictedDiff == actualDiff)
                {
                    return "Parcial Fuerte";
                }
                
                // Parcial Débil: solo acertó resultado
                return "Parcial Débil";
            }
            
            // Error: no acertó resultado
            return "Error";
        }
    }
}
