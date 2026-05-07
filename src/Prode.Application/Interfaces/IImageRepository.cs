using Prode.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prode.Application.Interfaces
{
    public interface IImageRepository
    {
        Task<(List<Image> items, int totalItems)> GetAllAsync(int page, int pageSize, string? search);
        Task<Image?> GetByIdAsync(Guid id);
        Task AddAsync(Image image);
        Task DeleteAsync(Image image);
        Task SaveChangesAsync();
    }
}