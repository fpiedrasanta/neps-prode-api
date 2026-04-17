using Prode.Application.Interfaces;
using Prode.Domain.Entities;

namespace Prode.Infrastructure.Services
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly IPredictionRepository _predictionRepository;
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly IPostRepository _postRepository;

        public MaintenanceService(
            IPredictionRepository predictionRepository,
            IFriendshipRepository friendshipRepository,
            IMatchRepository matchRepository,
            IPostRepository postRepository)
        {
            _predictionRepository = predictionRepository;
            _friendshipRepository = friendshipRepository;
            _matchRepository = matchRepository;
            _postRepository = postRepository;
        }

        public async Task<int> CalculatePointsForFinishedMatchesAsync()
        {
            int totalUpdated = 0;
            
            // Obtener todas las predicciones sin ResultType de partidos finalizados en una sola consulta
            var predictions = await _predictionRepository.GetPredictionsWithoutResultTypeForFinishedMatchesAsync();
            
            if (predictions.Count == 0)
            {
                return 0;
            }
            
            // Obtener todos los ResultType de una vez
            var resultTypes = await _predictionRepository.GetAllResultTypesAsync();

            foreach (var prediction in predictions)
            {
                var resultTypeName = CalculateResultTypeName(
                    prediction.HomeGoals,
                    prediction.AwayGoals,
                    prediction.Match.HomeScore!.Value,
                    prediction.Match.AwayScore!.Value);

                var resultType = resultTypes.FirstOrDefault(rt => rt.Name == resultTypeName);
                if (resultType != null)
                {
                    prediction.ResultType = resultType;
                    await _predictionRepository.UpdatePredictionAsync(prediction);
                    
                    // Actualizar TotalPoints del usuario
                    if (prediction.User != null)
                    {
                        await _predictionRepository.UpdateUserTotalPointsAsync(prediction.UserId, resultType.Points);
                    }
                    
                    totalUpdated++;
                }
            }

            return totalUpdated;
        }

        public async Task<int> CreatePostsForPredictionsAsync()
        {
            int totalCreated = 0;
            
            // Obtener todas las predicciones que tienen ResultType pero no tienen post
            // Esto se hace mediante una consulta que busca predicciones con ResultType
            // y que no tienen un post asociado
            var predictionsWithResultType = await _predictionRepository.GetPredictionsWithResultTypeWithoutPostAsync();
            
            foreach (var prediction in predictionsWithResultType)
            {
                // Verificar si ya existe un post para esta predicción
                if (await _postRepository.ExistsPostForPredictionAsync(prediction.Id))
                {
                    continue; // Ya existe, no crear otro
                }

                // Obtener el partido
                var match = await _matchRepository.GetMatchByIdAsync(prediction.MatchId);
                if (match == null)
                {
                    continue;
                }

                // Crear post
                await CreatePostForPredictionAsync(prediction, match, prediction.ResultType!.Points);
                totalCreated++;
            }

            return totalCreated;
        }

        private async Task CreatePostForPredictionAsync(Prediction prediction, Match match, int pointsEarned)
        {
            // Crear contenido del post
            var content = $@"{prediction.User?.FullName ?? "Usuario"} obtuvo {pointsEarned} puntos
Resultado: {match.HomeScore} - {match.AwayScore}
Pronóstico: {prediction.HomeGoals} - {prediction.AwayGoals}";

            var post = new Post
            {
                UserId = prediction.UserId,
                MatchId = match.Id,
                PredictionId = prediction.Id,
                Content = content,
                PointsEarned = pointsEarned
            };

            await _postRepository.CreatePostAsync(post);
        }

        public async Task<int> DeleteExpiredFriendRequestsAsync(int expirationDays)
        {
            var expirationDate = DateTime.UtcNow.AddDays(-expirationDays);
            return await _friendshipRepository.DeleteExpiredPendingRequestsAsync(expirationDate);
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