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

        // Crear Post Especial (Dashboard)
        Task<PostDto> CreateSpecialPostAsync(string title, string content);

        // Actualizar Post Especial
        Task<PostDto?> UpdateSpecialPostAsync(Guid postId, string title, string content);

        // Eliminar Post Especial
        Task<bool> DeleteSpecialPostAsync(Guid postId);

        // Listar Posts Especiales paginados para administración
        Task<(List<PostDto> Posts, int TotalCount, int TotalPages)> GetAllSpecialPostsAsync(int pageNumber, int pageSize, string? search);
    }
}
