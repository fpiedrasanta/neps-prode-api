using Prode.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace Prode.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserRankingResponseDto> GetRankingAsync(UserRankingFilterDto filter);
        Task<UserDto> GetByIdAsync(Guid id);
        Task<UserDto> UpdateAsync(Guid id, UserUpdateDto updateDto, IFormFile avatar = null);
    }
}
