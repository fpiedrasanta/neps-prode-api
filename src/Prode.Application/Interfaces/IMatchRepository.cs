using Prode.Application.DTOs;
using Prode.Domain.Entities;
using Prode.Domain.Enums;

namespace Prode.Application.Interfaces
{
    public interface IMatchRepository
    {
        // Métodos existentes para filtrado de partidos
        Task<List<Match>> GetMatchesByStatusAsync(MatchFilterDto filter);
        Task<int> GetMatchesCountByStatusAsync(MatchFilterDto filter);
        
        // Nuevos métodos para CRUD
        Task<Match?> GetMatchByIdAsync(Guid id);
        Task<Match> CreateMatchAsync(Match match);
        Task<Match> UpdateMatchAsync(Match match);
        Task<bool> DeleteMatchAsync(Guid id);
        Task<Match> UpdateMatchScoresAsync(Guid id, int? homeScore, int? awayScore);
        
        // Obtener partidos finalizados que tienen predicciones sin puntos
        Task<List<Match>> GetFinishedMatchesWithPendingPredictionsAsync();

        // Obtener partidos para enviar recordatorio de apuestas
        Task<IEnumerable<Match>> GetMatchesForReminderAsync(int minutesBefore);

        // Obtener partidos que acaban de empezar
        Task<IEnumerable<Match>> GetMatchesJustStartedAsync();

        // Obtener partidos que acaban de finalizar
        Task<IEnumerable<Match>> GetMatchesJustFinishedAsync();
    }
}
