namespace MyBuildingBlocks.DTOs
{
    public class UserProfileDto
    {
        public Guid Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? ProfilePictureUrl { get; set; }

        public DateTime? LastLoginDate { get; set; }
    }
}
