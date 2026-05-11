using Hangfire;
using Microsoft.Extensions.Logging;
using Prode.Application.DTOs;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Prode.Application.Services
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly IFriendshipService _friendshipService;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<PostService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public PostService(
            IPostRepository postRepository,
            IFriendshipService friendshipService,
            IPushNotificationService pushNotificationService,
            IBackgroundJobClient backgroundJobClient,
            ILogger<PostService> logger,
            UserManager<ApplicationUser> userManager)
        {
            _postRepository = postRepository;
            _friendshipService = friendshipService;
            _pushNotificationService = pushNotificationService;
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<(List<PostDto> Posts, int TotalCount, int TotalPages)> GetPostsAsync(int pageNumber, int pageSize, string currentUserId)
        {
            var summary = await _friendshipService.GetFriendshipSummaryAsync(currentUserId);
            var friendIds = summary.Friends.Select(f => f.FriendId).ToList();
            friendIds.Add(currentUserId);

            var (friendsPosts, friendsTotalCount) = await _postRepository.GetPostsByUsersAsync(friendIds, pageNumber, pageSize);
            
            var totalPages = (int)Math.Ceiling(friendsTotalCount / (double)pageSize);
            var postDtos = friendsPosts.Select(MapToDto).ToList();

            return (postDtos, friendsTotalCount, totalPages);
        }

        public async Task<PostDto?> GetPostByIdAsync(Guid id)
        {
            var post = await _postRepository.GetPostByIdWithCommentsAsync(id);
            return post == null ? null : MapToDto(post);
        }

        public async Task<PostDto?> UpdateSpecialPostAsync(Guid postId, string title, string content)
        {
            var post = await _postRepository.GetPostByIdWithCommentsAsync(postId);
            if (post == null || !post.IsSpecialPost)
                return null;

            post.Title = title;
            post.Content = content;
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.UpdatePostAsync(post);

            try
            {
                var jobId = _backgroundJobClient.Enqueue<SendPushNotificationJob>(job =>
                    job.SendToAllAsync(
                        "📢 Nuevo post",
                        title,
                        "{\"click_action\":\"/feed\"}"
                    ));

                _logger.LogInformation(
                    "📨 [Hangfire] Post especial editado - notificaciones encoladas. JobId: {JobId}, PostId: {PostId}, Title: {Title}",
                    jobId, postId, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ [Hangfire] Error crítico al encolar notificaciones push para post especial editado. PostId: {PostId}, Title: {Title}, Message: {Message}",
                    postId, title, ex.Message);
                throw;
            }

            return MapToDto(post);
        }

        public async Task<bool> DeleteSpecialPostAsync(Guid postId)
        {
            var post = await _postRepository.GetPostByIdWithCommentsAsync(postId);
            if (post == null || !post.IsSpecialPost)
                return false;

            await _postRepository.DeletePostAsync(postId);
            return true;
        }

        public async Task<(List<PostDto> Posts, int TotalCount, int TotalPages)> GetAllSpecialPostsAsync(int pageNumber, int pageSize, string? search)
        {
            var (posts, totalCount) = await _postRepository.GetAllSpecialPostsPagedAsync(pageNumber, pageSize, search);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var postDtos = posts.Select(MapToDto).ToList();

            return (postDtos, totalCount, totalPages);
        }

        public async Task<CommentDto> AddCommentAsync(Guid postId, string userId, string content)
        {
            var post = await _postRepository.GetPostByIdWithCommentsAsync(postId);
            if (post == null)
                throw new Exception("Post no encontrado");

            var comment = new Comment
            {
                PostId = postId,
                UserId = userId,
                Content = content
            };

            await _postRepository.CreateCommentAsync(comment);

            var createdComment = await _postRepository.GetCommentsByPostIdAsync(postId)
                .ContinueWith(t => t.Result.LastOrDefault());

            if (createdComment != null && post.UserId != null && post.UserId != userId)
            {
                var userComment = await _userManager.FindByIdAsync(userId);
                if (userComment != null)
                {
                    var matchInfo = post.Match != null
                        ? $"en {post.Match.HomeTeam?.Name} vs {post.Match.AwayTeam?.Name}"
                        : "";

                    await _pushNotificationService.SendNotificationToUsersAsync(
                        new[] { post.UserId },
                        "💬 Nuevo comentario",
                        $"{userComment.FullName} comentó en tú post {matchInfo}: {content}",
                        new { click_action = "/feed" }
                    );
                }
            }

            if (createdComment == null)
                throw new Exception("Error al crear el comentario");

            return new CommentDto
            {
                Id = createdComment.Id,
                UserId = createdComment.UserId,
                UserFullName = createdComment.User?.FullName ?? string.Empty,
                UserAvatarUrl = createdComment.User?.AvatarPath,
                Content = createdComment.Content,
                CreatedAt = createdComment.CreatedAt
            };
        }

        public async Task<PostDto> CreateSpecialPostAsync(string title, string content)
        {
            var post = new Post
            {
                IsSpecialPost = true,
                Title = title,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                UserId = null,
                MatchId = null,
                PredictionId = null
            };

            await _postRepository.CreatePostAsync(post);

            try
            {
                var jobId = _backgroundJobClient.Enqueue<SendPushNotificationJob>(job =>
                    job.SendToAllAsync(
                        "📢 Nuevo post",
                        title,
                        "{\"click_action\":\"/feed\"}"
                    ));

                _logger.LogInformation(
                    "📨 [Hangfire] Post especial creado - notificaciones encoladas. JobId: {JobId}, PostId: {PostId}, Title: {Title}",
                    jobId, post.Id, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ [Hangfire] Error crítico al encolar notificaciones push para post especial creado. PostId: {PostId}, Title: {Title}, Message: {Message}",
                    post.Id, title, ex.Message);
                throw;
            }

            return MapToDto(post);
        }

        private PostDto MapToDto(Post post)
        {
            return new PostDto
            {
                Id = post.Id,
                UserId = post.UserId,
                UserFullName = post.User?.FullName,
                UserAvatarUrl = post.User?.AvatarPath,
                IsSpecialPost = post.IsSpecialPost,
                Title = post.Title,
                MatchId = post.MatchId,
                HomeTeamName = post.Match?.HomeTeam?.Name ?? string.Empty,
                HomeTeamFlagUrl = post.Match?.HomeTeam?.FlagUrl,
                AwayTeamName = post.Match?.AwayTeam?.Name ?? string.Empty,
                AwayTeamFlagUrl = post.Match?.AwayTeam?.FlagUrl,
                HomeScore = post.Match?.HomeScore,
                AwayScore = post.Match?.AwayScore,
                HomePrediction = post.Prediction?.HomeGoals,
                AwayPrediction = post.Prediction?.AwayGoals,
                PointsEarned = post.PointsEarned,
                MatchDate = post.Match?.MatchDate ?? DateTime.MinValue,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                Comments = post.Comments?.Select(c => new CommentDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    UserFullName = c.User?.FullName ?? string.Empty,
                    UserAvatarUrl = c.User?.AvatarPath,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                }).ToList() ?? new List<CommentDto>()
            };
        }
    }
}