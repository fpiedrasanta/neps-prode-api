using System;

namespace Prode.Application.DTOs
{
    public class ImageDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
    }

    public class ImageUploadDto
    {
        public string Name { get; set; }
    }

    public class ImageFilterDto
    {
        public int PageNumber { get; set; } = 1;
        
        public int PageSize { get; set; } = 10;
        
        public string? Search { get; set; }
    }
}