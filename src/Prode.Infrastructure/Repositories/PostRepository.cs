using Microsoft.EntityFrameworkCore;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;
using Prode.Infrastructure.Data;

namespace Prode.Infrastructure.Repositories
{
    public class PostRepository : IPostRepository
    {
        public async Task<(List<Post> Posts, int TotalCount)> GetPostsByUsersAsync(List<string> userIds, int pageNumber, int pageSize)
        {
            var now = DateTime.UtcNow;
            var query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.Match)
                    .ThenInclude(m => m.HomeTeam)
                        .ThenInclude(t => t.Country)
                .Include(p => p.Match)
                    .ThenInclude(m => m.AwayTeam)
                        .ThenInclude(t => t.Country)
                .Include(p => p.Prediction)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Where(p => (userIds.Contains(p.UserId) || p.IsSpecialPost) && p.CreatedAt <= now)
                .OrderByDescending(p => p.CreatedAt)
                .AsQueryable();

            var totalCount = await query.CountAsync();
            
            var posts = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (posts, totalCount);
        }
        private readonly ApplicationDbContext _context;

        public PostRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Post> CreatePostAsync(Post post)
        {
            post.Id = Guid.NewGuid();
            post.CreatedAt = post.CreatedAt == default ? DateTime.UtcNow : post.CreatedAt;
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<Post?> GetPostByIdWithCommentsAsync(Guid id)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Match).ThenInclude(m => m.HomeTeam).ThenInclude(t => t.Country)
                .Include(p => p.Match).ThenInclude(m => m.AwayTeam).ThenInclude(t => t.Country)
                .Include(p => p.Prediction)
                .Include(p => p.Comments).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<(List<Post> Posts, int TotalCount)> GetPostsAsync(int pageNumber, int pageSize)
        {
            var query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.Match).ThenInclude(m => m.HomeTeam).ThenInclude(t => t.Country)
                .Include(p => p.Match).ThenInclude(m => m.AwayTeam).ThenInclude(t => t.Country)
                .Include(p => p.Prediction)
                .Include(p => p.Comments).ThenInclude(c => c.User)
                .AsQueryable();

            var totalCount = await query.CountAsync();

            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (posts, totalCount);
        }

        public async Task<Comment> CreateCommentAsync(Comment comment)
        {
            comment.Id = Guid.NewGuid();
            comment.CreatedAt = DateTime.UtcNow;
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<List<Comment>> GetCommentsByPostIdAsync(Guid postId)
        {
            return await _context.Comments
                .Include(c => c.User)
                .Where(c => c.PostId == postId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ExistsPostForPredictionAsync(Guid predictionId)
        {
            return await _context.Posts
                .AnyAsync(p => p.PredictionId == predictionId);
        }

        public async Task<List<Post>> GetSpecialPostsAsync()
        {
            return await _context.Posts
                .Include(p => p.Comments)
                .Where(p => p.IsSpecialPost)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdatePostAsync(Post post)
        {
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePostAsync(Guid postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post != null)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(List<Post> Posts, int TotalCount)> GetAllSpecialPostsPagedAsync(int pageNumber, int pageSize, string? search)
        {
            var query = _context.Posts
                .Where(p => p.IsSpecialPost)
                .AsQueryable();

            // Filtro de busqueda por título o contenido
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => 
                    EF.Functions.Like(p.Title, $"%{search}%") || 
                    EF.Functions.Like(p.Content, $"%{search}%"));
            }

            var totalCount = await query.CountAsync();
            
            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (posts, totalCount);
        }
    }
}
