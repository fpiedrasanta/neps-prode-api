using Prode.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prode.Application.Interfaces
{
    public interface IImageService
    {
        Task<PaginatedResponseDto<ImageDto>> GetAllAsync(ImageFilterDto filter);
        Task<ImageDto> GetByIdAsync(Guid id);
        Task<List<ImageDto>> UploadAsync(IEnumerable<(byte[] FileContent, string FileName, string Name)> files);
        Task<bool> DeleteAsync(Guid id);
    }
}