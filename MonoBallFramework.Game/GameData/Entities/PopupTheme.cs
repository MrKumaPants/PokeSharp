using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonoBallFramework.Game.GameData.Entities;

/// <summary>
///     EF Core entity for popup theme definitions.
///     Defines background and outline assets for map popup display.
/// </summary>
[Table("PopupThemes")]
public class PopupTheme
{
    /// <summary>
    ///     Unique theme identifier (e.g., "wood", "marble", "stone").
    /// </summary>
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     Display name (e.g., "Wood", "Marble", "Stone").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Description of theme usage (e.g., "Default wooden frame - used for towns, land routes, woods").
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    ///     Background asset ID (references asset in Popups/Backgrounds/).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Background { get; set; } = string.Empty;

    /// <summary>
    ///     Outline asset ID (references asset in Popups/Outlines/).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Outline { get; set; } = string.Empty;

    /// <summary>
    ///     Number of map sections using this theme (for statistics).
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    ///     Source mod ID (null for base game).
    /// </summary>
    [MaxLength(100)]
    public string? SourceMod { get; set; }

    /// <summary>
    ///     Version for compatibility tracking.
    /// </summary>
    [MaxLength(20)]
    public string Version { get; set; } = "1.0.0";

    // Navigation property
    /// <summary>
    ///     Map sections that use this theme.
    /// </summary>
    public ICollection<MapSection> MapSections { get; set; } = new List<MapSection>();
}


