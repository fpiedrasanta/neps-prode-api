using Prode.Application.DTOs;
using Prode.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Prode.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IFileService _fileService;
        private readonly ICountryRepository _countryRepository;

        public UserService(IUserRepository userRepository, IFileService fileService, ICountryRepository countryRepository)
        {
            _userRepository = userRepository;
            _fileService = fileService;
            _countryRepository = countryRepository;
        }

        public async Task<UserRankingResponseDto> GetRankingAsync(UserRankingFilterDto filter)
        {
            var (users, totalCount, userPositions) = await _userRepository.GetRankingAsync(filter);

            var userDtos = users.Select(u => new UserRankingDto
            {
                Position = userPositions.TryGetValue(u.Id, out var position) ? position : 0, // Posición global en el ranking
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                AvatarUrl = u.AvatarPath,
                TotalPoints = u.TotalPoints,
                CountryName = u.Country?.Name
            }).ToList();

            return new UserRankingResponseDto
            {
                Users = userDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<UserDto> GetByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            
            if (user == null)
                return null;

            return new UserDto
            {
                Id = Guid.Parse(user.Id),
                Email = user.Email,
                FullName = user.FullName,
                AvatarUrl = user.AvatarPath,
                CountryId = user.Country != null ? user.Country.Id : null,
                CountryName = user.Country?.Name,
                TotalPoints = user.TotalPoints
            };
        }

        public async Task<UserDto> UpdateAsync(Guid id, UserUpdateDto updateDto, IFormFile avatar = null)
        {
            var user = await _userRepository.GetByIdAsync(id);
            
            if (user == null)
                return null;

            // Actualizar campos básicos
            if (!string.IsNullOrWhiteSpace(updateDto.Email))
                user.Email = updateDto.Email;
                
            if (!string.IsNullOrWhiteSpace(updateDto.FullName))
                user.FullName = updateDto.FullName;
                
            if (updateDto.CountryId.HasValue)
            {
                var country = await _countryRepository.GetCountryByIdAsync(updateDto.CountryId.Value);
                if (country != null)
                {
                    user.Country = country;
                }
            }

            // Procesar avatar si se envía
            if (avatar != null && avatar.Length > 0)
            {
                // Eliminar avatar anterior si existe
                if (!string.IsNullOrEmpty(user.AvatarPath))
                {
                    await _fileService.DeleteAvatarAsync(user.AvatarPath);
                }
                
                // Guardar nuevo avatar
                using var stream = avatar.OpenReadStream();
                user.AvatarPath = await _fileService.SaveAvatarAsync(stream, avatar.FileName, user.Id);
            }

            await _userRepository.UpdateAsync(user);

            return new UserDto
            {
                Id = Guid.Parse(user.Id),
                Email = user.Email,
                FullName = user.FullName,
                AvatarUrl = user.AvatarPath,
                TotalPoints = user.TotalPoints
            };
        }
    }
}
