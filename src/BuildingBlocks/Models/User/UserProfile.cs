namespace MyBuildingBlocks.Models.User;

using System;
using System.ComponentModel.DataAnnotations;

public class UserProfile
{
    [Key]

    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; }

    [Required, MaxLength(100)]
    public string LastName { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public DateTime? LastLoginDate { get; set; }

    // Navigation property
    public User User { get; set; }
}
