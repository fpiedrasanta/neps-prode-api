using Prode.Application.DTOs;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Prode.Application.Services
{
    public class ImageService : IImageService
    {
        private readonly IImageRepository _imageRepository;
        private readonly IFileService _fileService;

        public ImageService(IImageRepository imageRepository, IFileService fileService)
        {
            _imageRepository = imageRepository;
            _fileService = fileService;
        }

        public async Task<PaginatedResponseDto<ImageDto>> GetAllAsync(ImageFilterDto filter)
        {
            var result  = await _imageRepository.GetAllAsync(filter.PageNumber, filter.PageSize, filter.Search);
            var items = result.items;
            var totalItems = result.totalItems;

            var images = items.Select(i => new ImageDto
            {
                Id = i.Id,
                FileName = i.FileName,
                Url = i.Url,
                Name = i.Name,
                Type = i.Type,
                Date = i.Date
            }).ToList();

            return new PaginatedResponseDto<ImageDto>
            {
                Items = images,
                TotalCount = totalItems,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)filter.PageSize)
            };
        }

        public async Task<ImageDto> GetByIdAsync(Guid id)
        {
            var image = await _imageRepository.GetByIdAsync(id);

            if (image == null)
                return null;

            return new ImageDto
            {
                Id = image.Id,
                FileName = image.FileName,
                Url = image.Url,
                Name = image.Name,
                Type = image.Type,
                Date = image.Date
            };
        }

        public async Task<List<ImageDto>> UploadAsync(IEnumerable<(byte[] FileContent, string FileName, string Name)> files)
        {
            var result = new List<ImageDto>();

            foreach (var file in files)
            {
                using var stream = new MemoryStream(file.FileContent);
                var (fileName, url) = await _fileService.SaveImageAsync(stream, file.FileName);

                var image = new Image
                {
                    Id = Guid.NewGuid(),
                    FileName = fileName,
                    Name = file.Name,
                    Url = url,
                    Type = Path.GetExtension(file.FileName).TrimStart('.').ToLower(),
                    Date = DateTime.UtcNow
                };

                await _imageRepository.AddAsync(image);
                await _imageRepository.SaveChangesAsync();

                result.Add(new ImageDto
                {
                    Id = image.Id,
                    FileName = image.FileName,
                    Url = image.Url,
                    Name = image.Name,
                    Type = image.Type,
                    Date = image.Date
                });
            }

            return result;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var image = await _imageRepository.GetByIdAsync(id);

            if (image == null)
                return false;

            // Borrar archivo fisico
            await _fileService.DeleteImageAsync(image.FileName);

            // Borrar de BD
            await _imageRepository.DeleteAsync(image);
            await _imageRepository.SaveChangesAsync();

            return true;
        }
    }
}