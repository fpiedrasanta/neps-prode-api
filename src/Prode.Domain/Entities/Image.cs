using System;

namespace Prode.Domain.Entities
{
    public class Image
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
    }
}