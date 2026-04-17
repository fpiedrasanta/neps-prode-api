using Prode.Domain.Entities;

namespace Prode.Application.Interfaces
{
    public interface IPostRepository
    {
        Task<(List<Post> Posts, int TotalCount)> GetPostsByUsersAsync(List<string> userIds, int pageNumber, int pageSize);
        // Crear post
        Task<Post> CreatePostAsync(Post post);
        
        // Obtener post por ID con comentarios
        Task<Post?> GetPostByIdWithCommentsAsync(Guid id);
        
        // Obtener posts paginados
        Task<(List<Post> Posts, int TotalCount)> GetPostsAsync(int pageNumber, int pageSize);
        
        // Crear comentario
        Task<Comment> CreateCommentAsync(Comment comment);
        
        // Obtener comentarios de un post
        Task<List<Comment>> GetCommentsByPostIdAsync(Guid postId);
        
        // Verificar si existe post para una predicción
        Task<bool> ExistsPostForPredictionAsync(Guid predictionId);
    }
}