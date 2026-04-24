using Prode.Application.DTOs;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;

namespace Prode.Application.Services
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly IFriendshipService _friendshipService;

        public PostService(IPostRepository postRepository, IFriendshipService friendshipService)
        {
            _postRepository = postRepository;
            _friendshipService = friendshipService;
        }

        public async Task<(List<PostDto> Posts, int TotalCount, int TotalPages)> GetPostsAsync(int pageNumber, int pageSize, string currentUserId)
        {
            // Obtener todos los amigos del usuario actual
            var summary = await _friendshipService.GetFriendshipSummaryAsync(currentUserId);
            var friendIds = summary.Friends.Select(f => f.FriendId).ToList();
            
            // Incluir tambien los propios posts del usuario
            friendIds.Add(currentUserId);

            var (posts, totalCount) = await _postRepository.GetPostsByUsersAsync(friendIds, pageNumber, pageSize);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var postDtos = posts.Select(MapToDto).ToList();

            return (postDtos, totalCount, totalPages);
        }

        public async Task<PostDto?> GetPostByIdAsync(Guid id)
        {
            var post = await _postRepository.GetPostByIdWithCommentsAsync(id);
            if (post == null)
            {
                return null;
            }

            return MapToDto(post);
        }

        public async Task<CommentDto> AddCommentAsync(Guid postId, string userId, string content)
        {
            // Verificar que el post existe
            var post = await _postRepository.GetPostByIdWithCommentsAsync(postId);
            if (post == null)
            {
                throw new Exception("Post no encontrado");
            }

            var comment = new Comment
            {
                PostId = postId,
                UserId = userId,
                Content = content
            };

            await _postRepository.CreateCommentAsync(comment);
            
            // Recargar el comentario con los datos del usuario
            var createdComment = await _postRepository.GetCommentsByPostIdAsync(postId)
                .ContinueWith(t => t.Result.LastOrDefault());

            if (createdComment == null)
            {
                throw new Exception("Error al crear el comentario");
            }

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

        private PostDto MapToDto(Post post)
        {
            return new PostDto
            {
                Id = post.Id,
                UserId = post.UserId,
                UserFullName = post.User?.FullName ?? string.Empty,
                UserAvatarUrl = post.User?.AvatarPath,
                
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