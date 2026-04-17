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
                .Where(p => userIds.Contains(p.UserId))
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
            post.CreatedAt = DateTime.UtcNow;
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
    }
}