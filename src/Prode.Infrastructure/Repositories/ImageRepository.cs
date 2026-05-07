using Microsoft.EntityFrameworkCore;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;
using Prode.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prode.Infrastructure.Repositories
{
    public class ImageRepository : IImageRepository
    {
        private readonly ApplicationDbContext _context;

        public ImageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(List<Image> items, int totalItems)> GetAllAsync(int page, int pageSize, string? search)
        {
            var query = _context.Images.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(i => i.Name.Contains(search));
            }

            var totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(i => i.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalItems);
        }

        public async Task<Image?> GetByIdAsync(Guid id)
        {
            return await _context.Images.FindAsync(id);
        }

        public async Task AddAsync(Image image)
        {
            await _context.Images.AddAsync(image);
        }

        public Task DeleteAsync(Image image)
        {
            _context.Images.Remove(image);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}