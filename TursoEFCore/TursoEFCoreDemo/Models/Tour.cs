using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TursoEFCoreDemo.Models;

/// <summary>
/// Represents a tour record in the database.
/// </summary>
[Table("tours")]
public class Tour
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("name")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column("destination")]
    [MaxLength(200)]
    public string Destination { get; set; } = string.Empty;

    [Column("price")]
    public double Price { get; set; }

    [Column("duration_days")]
    public int DurationDays { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public override string ToString()
    {
        return $"[{Id}] {Name} | {Destination} | ${Price:F2} | {DurationDays} days | Active: {IsActive} | Created: {CreatedAt:yyyy-MM-dd}";
    }
}
