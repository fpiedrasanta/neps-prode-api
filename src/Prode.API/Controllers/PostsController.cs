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
    }
}