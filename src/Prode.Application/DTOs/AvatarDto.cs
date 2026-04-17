namespace Prode.Application.DTOs
{
    public class AvatarUploadDto
    {
        public byte[] FileData { get; set; } = null!;
        public string FileName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    public class AvatarResponseDto
    {
        public string AvatarUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
