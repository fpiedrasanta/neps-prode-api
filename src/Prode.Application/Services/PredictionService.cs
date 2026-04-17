using Prode.Application.DTOs;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration;

namespace Prode.Application.Services
{
    public class PredictionService : IPredictionService
    {
        private readonly IPredictionRepository _predictionRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly IConfiguration _configuration;

        public PredictionService(
            IPredictionRepository predictionRepository,
            IMatchRepository matchRepository,
            IConfiguration configuration)
        {
            _predictionRepository = predictionRepository;
            _matchRepository = matchRepository;
            _configuration = configuration;
        }

        public async Task<PredictionDto> CreatePredictionAsync(string userId, PredictionCreateDto createDto)
        {
            // 1. Verificar que el partido existe
            var match = await _matchRepository.GetMatchByIdAsync(createDto.MatchId);
            if (match == null)
            {
                throw new Exception("Partido no encontrado.");
            }

            // 2. Verificar que el usuario no haya predicho ya ese partido
            var existingPrediction = await _predictionRepository.GetUserPredictionByMatchIdAsync(createDto.MatchId, userId);
            if (existingPrediction != null)
            {
                throw new Exception("Ya realizaste una predicción para este partido.");
            }

            // 3. Verificar que el partido no tenga resultado cargado
            if (match.HomeScore.HasValue || match.AwayScore.HasValue)
            {
                throw new Exception("El partido ya tiene resultado cargado.");
            }

            // 4. Verificar que el partido no esté en juego (X minutos antes del inicio)
            var minutesBeforeMatchToLock = int.Parse(_configuration["MinutesBeforeMatchToLock"] ?? "15");
            var lockTime = match.MatchDate.AddMinutes(-minutesBeforeMatchToLock);
            if (DateTime.UtcNow >= lockTime)
            {
                throw new Exception("El partido ya está en juego o comenzó.");
            }

            // Crear la predicción
            var prediction = new Prediction
            {
                MatchId = createDto.MatchId,
                HomeGoals = createDto.HomeGoals,
                AwayGoals = createDto.AwayGoals,
                UserId = userId
            };

            var createdPrediction = await _predictionRepository.CreatePredictionAsync(prediction);

            return MapToDto(createdPrediction);
        }

        public async Task<PredictionDto> UpdatePredictionAsync(string userId, Guid id, PredictionUpdateDto updateDto)
        {
            // 1. Verificar que la predicción existe y pertenece al usuario
            var prediction = await _predictionRepository.GetPredictionByIdAsync(id);
            if (prediction == null)
            {
                throw new Exception("Predicción no encontrada.");
            }

            if (prediction.UserId != userId)
            {
                throw new Exception("No tenés permiso para modificar esta predicción.");
            }

            // 2. Verificar que el partido no tenga resultado cargado
            if (prediction.Match.HomeScore.HasValue || prediction.Match.AwayScore.HasValue)
            {
                throw new Exception("El partido ya tiene resultado cargado.");
            }

            // 3. Verificar que el partido no esté en juego
            var minutesBeforeMatchToLock = int.Parse(_configuration["MinutesBeforeMatchToLock"] ?? "15");
            var lockTime = prediction.Match.MatchDate.AddMinutes(-minutesBeforeMatchToLock);
            if (DateTime.UtcNow >= lockTime)
            {
                throw new Exception("El partido ya está en juego o comenzó.");
            }

            // Actualizar la predicción
            prediction.HomeGoals = updateDto.HomeGoals;
            prediction.AwayGoals = updateDto.AwayGoals;

            var updatedPrediction = await _predictionRepository.UpdatePredictionAsync(prediction);

            return MapToDto(updatedPrediction);
        }

        public async Task<bool> DeletePredictionAsync(string userId, Guid id)
        {
            // 1. Verificar que la predicción existe y pertenece al usuario
            var prediction = await _predictionRepository.GetPredictionByIdAsync(id);
            if (prediction == null)
            {
                throw new Exception("Predicción no encontrada.");
            }

            if (prediction.UserId != userId)
            {
                throw new Exception("No tenés permiso para eliminar esta predicción.");
            }

            // 2. Verificar que el partido no tenga resultado cargado
            if (prediction.Match.HomeScore.HasValue || prediction.Match.AwayScore.HasValue)
            {
                throw new Exception("No se puede eliminar una predicción de un partido finalizado.");
            }

            // 3. Verificar que el partido no esté en juego
            var minutesBeforeMatchToLock = int.Parse(_configuration["MinutesBeforeMatchToLock"] ?? "15");
            var lockTime = prediction.Match.MatchDate.AddMinutes(-minutesBeforeMatchToLock);
            if (DateTime.UtcNow >= lockTime)
            {
                throw new Exception("No se puede eliminar una predicción de un partido en juego.");
            }

            // Eliminar la predicción (DELETE real)
            return await _predictionRepository.DeletePredictionAsync(id);
        }

        public async Task<PredictionDto?> GetPredictionByIdAsync(string userId, Guid id)
        {
            var prediction = await _predictionRepository.GetPredictionByIdAsync(id);
            
            if (prediction == null)
            {
                return null;
            }

            // Verificar que la predicción pertenezca al usuario
            if (prediction.UserId != userId)
            {
                throw new Exception("No tenés permiso para ver esta predicción.");
            }

            return MapToDto(prediction);
        }

        private PredictionDto MapToDto(Prediction prediction)
        {
            return new PredictionDto
            {
                Id = prediction.Id,
                MatchId = prediction.MatchId,
                HomeGoals = prediction.HomeGoals,
                AwayGoals = prediction.AwayGoals,
                CreatedAt = prediction.CreatedAt,
                UpdatedAt = prediction.UpdatedAt,
                Points = prediction.GetResultTypePoints(),
                ResultTypeName = prediction.ResultType?.Name
            };
        }

        public async Task<List<PredictionDto>> GetPredictionsWithoutResultTypeAsync(string userId)
        {
            var predictions = await _predictionRepository.GetPredictionsWithoutResultTypeAsync(userId);
            return predictions.Select(MapToDto).ToList();
        }

        public async Task<int> AssignResultTypesToPredictionsAsync(string userId)
        {
            var predictions = await _predictionRepository.GetPredictionsWithoutResultTypeAsync(userId);
            var resultTypes = await _predictionRepository.GetAllResultTypesAsync();
            
            int updatedCount = 0;
            int totalPointsToAdd = 0;
            
            foreach (var prediction in predictions)
            {
                var resultTypeName = CalculateResultTypeName(
                    prediction.HomeGoals, 
                    prediction.AwayGoals, 
                    prediction.Match.HomeScore.Value, 
                    prediction.Match.AwayScore.Value);
                
                var resultType = resultTypes.FirstOrDefault(rt => rt.Name == resultTypeName);
                if (resultType != null)
                {
                    prediction.ResultType = resultType;
                    totalPointsToAdd += resultType.Points;
                    await _predictionRepository.UpdatePredictionAsync(prediction);
                    updatedCount++;
                }
            }
            
            // Actualizar TotalPoints del usuario si se actualizaron predicciones
            if (updatedCount > 0)
            {
                await _predictionRepository.UpdateUserTotalPointsAsync(userId, totalPointsToAdd);
            }
            
            return updatedCount;
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
