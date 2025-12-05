using Microsoft.EntityFrameworkCore;
using MonoBallFramework.Game.GameData.Entities;
using MonoBallFramework.Game.GameData.ValueConverters;

namespace MonoBallFramework.Game.GameData;

/// <summary>
///     EF Core DbContext for game data definitions (NPCs, trainers, maps, etc.).
///     Uses in-memory database for fast, read-only access.
/// </summary>
public class GameDataContext : DbContext
{
    // TODO: Add when implementing Pokemon system
    // public DbSet<Species> Species { get; set; } = null!;
    // public DbSet<Move> Moves { get; set; } = null!;
    // public DbSet<Item> Items { get; set; } = null!;
    // public DbSet<Ability> Abilities { get; set; } = null!;

    public GameDataContext(DbContextOptions<GameDataContext> options)
        : base(options) { }

    // NPC-related entities
    public DbSet<NpcDefinition> Npcs { get; set; } = null!;
    public DbSet<TrainerDefinition> Trainers { get; set; } = null!;

    // Map entities
    public DbSet<MapDefinition> Maps { get; set; } = null!;

    // Popup entities
    public DbSet<PopupTheme> PopupThemes { get; set; } = null!;
    public DbSet<MapSection> MapSections { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureNpcDefinition(modelBuilder);
        ConfigureTrainerDefinition(modelBuilder);
        ConfigureMapDefinition(modelBuilder);
        ConfigurePopupTheme(modelBuilder);
        ConfigureMapSection(modelBuilder);
    }

    /// <summary>
    ///     Configure NpcDefinition entity.
    /// </summary>
    private void ConfigureNpcDefinition(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NpcDefinition>(entity =>
        {
            entity.HasKey(n => n.NpcId);

            // Indexes for common queries
            entity.HasIndex(n => n.NpcType);
            entity.HasIndex(n => n.DisplayName);

            // Value converter for SpriteId
            entity.Property(n => n.SpriteId).HasConversion(new SpriteIdValueConverter());
        });
    }

    /// <summary>
    ///     Configure TrainerDefinition entity.
    /// </summary>
    private void ConfigureTrainerDefinition(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrainerDefinition>(entity =>
        {
            entity.HasKey(t => t.TrainerId);

            // Indexes for common queries
            entity.HasIndex(t => t.TrainerClass);
            entity.HasIndex(t => t.DisplayName);

            // Value converter for SpriteId
            entity.Property(t => t.SpriteId).HasConversion(new SpriteIdValueConverter());

            // PartyJson will be deserialized on-demand
        });
    }

    /// <summary>
    ///     Configure MapDefinition entity.
    /// </summary>
    private void ConfigureMapDefinition(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MapDefinition>(entity =>
        {
            entity.HasKey(m => m.MapId);

            // Indexes for common queries
            entity.HasIndex(m => m.Region);
            entity.HasIndex(m => m.MapType);
            entity.HasIndex(m => m.DisplayName);

            // Value converters for MapIdentifier
            var mapIdConverter = new MapIdentifierValueConverter();
            entity.Property(m => m.MapId).HasConversion(mapIdConverter);
            entity.Property(m => m.NorthMapId).HasConversion(mapIdConverter);
            entity.Property(m => m.SouthMapId).HasConversion(mapIdConverter);
            entity.Property(m => m.EastMapId).HasConversion(mapIdConverter);
            entity.Property(m => m.WestMapId).HasConversion(mapIdConverter);

            // TiledDataJson stores complete Tiled map data
            // Will be deserialized on-demand by MapLoader
        });
    }

    /// <summary>
    ///     Configure PopupTheme entity.
    /// </summary>
    private void ConfigurePopupTheme(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PopupTheme>(entity =>
        {
            entity.HasKey(t => t.Id);

            // Indexes for common queries
            entity.HasIndex(t => t.Name);
            entity.HasIndex(t => t.Background);
            entity.HasIndex(t => t.Outline);

            // Configure relationship with MapSections
            entity
                .HasMany(t => t.MapSections)
                .WithOne(s => s.Theme)
                .HasForeignKey(s => s.ThemeId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting themes with sections
        });
    }

    /// <summary>
    ///     Configure MapSection entity.
    /// </summary>
    private void ConfigureMapSection(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MapSection>(entity =>
        {
            entity.HasKey(s => s.Id);

            // Indexes for common queries
            entity.HasIndex(s => s.Name);
            entity.HasIndex(s => s.ThemeId);

            // Composite index for region map coordinates
            entity.HasIndex(s => new { s.X, s.Y });
        });
    }
}
