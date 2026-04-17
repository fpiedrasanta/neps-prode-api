using System.IO;
using System.Threading.Tasks;

namespace Prode.Application.Interfaces
{
    public interface IFileService
    {
        Task<string> SaveAvatarAsync(Stream fileStream, string fileName, string userId);
        Task<string> GetAvatarUrl(string fileName);
        Task<bool> DeleteAvatarAsync(string fileName);
        string GenerateFileName(string originalFileName, string userId);
        Task<string> SaveFlagAsync(Stream fileStream, string fileName);
    }
}
