using System.ComponentModel.DataAnnotations;

namespace Presentation.Data.Entities;

public class VerificationCodeEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Email { get; set; } = null!;

    [Required]
    public string Code { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime ExpiresAt { get; set; }
}
