namespace Prode.Application.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
        public Guid? CountryId { get; set; }
        public string CountryName { get; set; }
        public int? TotalPoints { get; set; }
    }

    public class UserUpdateDto
    {
        public string Email { get; set; }
        public string FullName { get; set; }
        public Guid? CountryId { get; set; }
    }
}