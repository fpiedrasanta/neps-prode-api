using Microsoft.Extensions.Configuration;
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
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IConfiguration _configuration;

        public MaintenanceService(
            IPredictionRepository predictionRepository,
            IFriendshipRepository friendshipRepository,
            IMatchRepository matchRepository,
            IPostRepository postRepository,
            IPushNotificationService pushNotificationService,
            IConfiguration configuration)
        {
            _predictionRepository = predictionRepository;
            _friendshipRepository = friendshipRepository;
            _matchRepository = matchRepository;
            _postRepository = postRepository;
            _pushNotificationService = pushNotificationService;
            _configuration = configuration;
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
            bool actualAwayWins = actualAway > actualHome; // <-- FIX
            bool actualDraw = actualHome == actualAway;

            // Verificar si acertó el resultado
            bool acertoResultado = (predictedHomeWins && actualHomeWins) ||
                                (predictedAwayWins && actualAwayWins) ||
                                (predictedDraw && actualDraw);

            if (acertoResultado)
            {
                int predictedDiff = predictedHome - predictedAway;
                int actualDiff = actualHome - actualAway;

                if (predictedDiff == actualDiff)
                {
                    return "Parcial Fuerte";
                }

                return "Parcial Débil";
            }

            return "Error";
        }

        public async Task ProcessMatchRemindersAsync()
        {
            int minutesBeforeLock = _configuration.GetValue<int>("MatchSettings:MinutesBeforeMatchToLock", 15);
            int reminderMinutesBefore = minutesBeforeLock + 15;

            var matches = await _matchRepository.GetMatchesForReminderAsync(reminderMinutesBefore);

            foreach (var match in matches)
            {
                if (match.ReminderNotificationSent) continue;

                var userIdsWithoutPrediction = await _predictionRepository.GetUserIdsWithoutPredictionForMatchAsync(match.Id);
                
                if (userIdsWithoutPrediction.Any())
                {
                    await _pushNotificationService.SendNotificationToUsersAsync(
                        userIdsWithoutPrediction,
                        $"⏰ {match.GetHomeTeamName()} vs {match.GetAwayTeamName()}",
                        $"Faltan {reminderMinutesBefore} minutos para cerrar las apuestas. ¿Ya hiciste tu pronóstico?"
                    );
                }

                match.ReminderNotificationSent = true;
                await _matchRepository.UpdateMatchAsync(match);
            }
        }

        public async Task ProcessMatchStartedNotificationsAsync()
        {
            var matches = await _matchRepository.GetMatchesJustStartedAsync();

            foreach (var match in matches)
            {
                if (match.StartedNotificationSent) continue;

                await _pushNotificationService.SendNotificationToAllUsersAsync(
                    "⚽ ¡El partido empezo!",
                    $"{match.GetHomeTeamName()} vs {match.GetAwayTeamName()} ya esta en curso"
                );

                match.StartedNotificationSent = true;
                await _matchRepository.UpdateMatchAsync(match);
            }
        }

        public async Task ProcessMatchFinishedNotificationsAsync()
        {
            var matches = await _matchRepository.GetMatchesJustFinishedAsync();

            foreach (var match in matches)
            {
                if (match.FinishedNotificationSent) continue;

                await _pushNotificationService.SendNotificationToAllUsersAsync(
                    "🎉 Resultados listos!",
                    "Ya finalizaron los partidos, mira como quedaron!"
                );

                match.FinishedNotificationSent = true;
                await _matchRepository.UpdateMatchAsync(match);
            }
        }
    }
}
