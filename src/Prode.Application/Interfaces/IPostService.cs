using Prode.Application.DTOs;

namespace Prode.Application.Interfaces
{
    public interface IPostService
    {
        // Obtener posts paginados
        Task<(List<PostDto> Posts, int TotalCount, int TotalPages)> GetPostsAsync(int pageNumber, int pageSize, string currentUserId);
        
        // Obtener post por ID
        Task<PostDto?> GetPostByIdAsync(Guid id);
        
        // Agregar comentario a un post
        Task<CommentDto> AddCommentAsync(Guid postId, string userId, string content);
    }
}