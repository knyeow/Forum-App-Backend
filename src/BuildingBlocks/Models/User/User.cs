namespace MyBuildingBlocks.Models.User
{

    using System;
    using System.ComponentModel.DataAnnotations;

    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required, EmailAddress, MaxLength(200)]
        public string Email { get; set; }
        [Required,MaxLength(200)]
        public string Username { get; set; }

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; }

        public bool IsActive { get; set; } = true;

        public bool EmailConfirmed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public UserProfile? Profile { get; set; }
    }
}

