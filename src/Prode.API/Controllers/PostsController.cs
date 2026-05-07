using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prode.Application.DTOs;
using Prode.Application.Interfaces;

namespace Prode.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostsController(IPostService postService)
        {
            _postService = postService;
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new Exception("Usuario no autenticado");
        }

        /// <summary>
        /// Obtener lista de posts paginada
        /// </summary>
        /// <param name="pageNumber">Número de página (default: 1)</param>
        /// <param name="pageSize">Tamaño de página (default: 10)</param>
        [HttpGet]
        public async Task<IActionResult> GetPosts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetUserId();
                var (posts, totalCount, totalPages) = await _postService.GetPostsAsync(pageNumber, pageSize, userId);
                
                Response.Headers.Append("X-Pagination-Total-Count", totalCount.ToString());
                Response.Headers.Append("X-Pagination-Total-Pages", totalPages.ToString());
                Response.Headers.Append("X-Pagination-Current-Page", pageNumber.ToString());
                
                return Ok(new { posts, totalCount, totalPages, pageNumber, pageSize });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Obtener un post por ID
        /// </summary>
        /// <param name="id">ID del post</param>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPost(Guid id)
        {
            try
            {
                var post = await _postService.GetPostByIdAsync(id);
                if (post == null)
                {
                    return NotFound("Post no encontrado");
                }
                return Ok(post);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Agregar comentario a un post
        /// </summary>
        /// <param name="postId">ID del post</param>
        /// <param name="dto">Contenido del comentario</param>
        [HttpPost("{postId}/comments")]
        public async Task<IActionResult> AddComment(Guid postId, [FromBody] CreateCommentDto dto)
        {
            try
            {
                var userId = GetUserId();
                var comment = await _postService.AddCommentAsync(postId, userId, dto.Content);
                return Ok(comment);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Crear Post Especial (Solo Administradores)
        /// </summary>
        /// <param name="dto">Título y contenido HTML del post</param>
        [HttpPost("special")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSpecialPost([FromBody] CreateSpecialPostDto dto)
        {
            try
            {
                var post = await _postService.CreateSpecialPostAsync(dto.Title, dto.Content);
                return Ok(post);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Actualizar Post Especial (Solo Administradores)
        /// </summary>
        [HttpPut("special/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSpecialPost(Guid id, [FromBody] CreateSpecialPostDto dto)
        {
            try
            {
                var post = await _postService.UpdateSpecialPostAsync(id, dto.Title, dto.Content);
                if (post == null)
                    return NotFound("Post especial no encontrado");
                
                return Ok(post);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Eliminar Post Especial (Solo Administradores)
        /// </summary>
        [HttpDelete("special/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSpecialPost(Guid id)
        {
            try
            {
                var success = await _postService.DeleteSpecialPostAsync(id);
                if (!success)
                    return NotFound("Post especial no encontrado");
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Listar Posts Especiales paginados para administración
        /// </summary>
        /// <param name="pageNumber">Número de página (default: 1)</param>
        /// <param name="pageSize">Tamaño de página (default: 10)</param>
        /// <param name="search">Filtro de busqueda en título y contenido</param>
        [HttpGet("special")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllSpecialPosts(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] string? search = null)
        {
            try
            {
                var (posts, totalCount, totalPages) = await _postService.GetAllSpecialPostsAsync(pageNumber, pageSize, search);
                
                Response.Headers.Append("X-Pagination-Total-Count", totalCount.ToString());
                Response.Headers.Append("X-Pagination-Total-Pages", totalPages.ToString());
                Response.Headers.Append("X-Pagination-Current-Page", pageNumber.ToString());
                
                return Ok(new { posts, totalCount, totalPages, pageNumber, pageSize });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
