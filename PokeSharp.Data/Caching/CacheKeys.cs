namespace PokeSharp.Data.Caching;

/// <summary>
///     Strongly-typed cache keys for game data.
///     Using constants prevents typos and makes cache key management easier.
///     Format: "{category}:{identifier}"
/// </summary>
public static class CacheKeys
{
    public static string AllSpecies => "species:all";

    public static string AllMoves => "moves:all";

    public static string AllItems => "items:all";

    // Type effectiveness cache keys
    public static string TypeEffectiveness => "types:effectiveness";

    public static string AllStatusConditions => "status:all";

    public static string AllAbilities => "abilities:all";

    // Species cache keys
    public static string Species(int id)
    {
        return $"species:{id}";
    }

    public static string SpeciesByName(string name)
    {
        return $"species:name:{name.ToLowerInvariant()}";
    }

    // Move cache keys
    public static string Move(int id)
    {
        return $"move:{id}";
    }

    public static string MoveByName(string name)
    {
        return $"move:name:{name.ToLowerInvariant()}";
    }

    public static string MovesByType(string typeId)
    {
        return $"moves:type:{typeId}";
    }

    public static string MovesByDamageClass(string damageClass)
    {
        return $"moves:damageclass:{damageClass}";
    }

    // Item cache keys
    public static string Item(int id)
    {
        return $"item:{id}";
    }

    public static string ItemByName(string name)
    {
        return $"item:name:{name.ToLowerInvariant()}";
    }

    public static string ItemsByCategory(string category)
    {
        return $"items:category:{category}";
    }

    public static string TypeById(int id)
    {
        return $"type:{id}";
    }

    // Encounter cache keys
    public static string EncounterTable(string tableId)
    {
        return $"encounter:table:{tableId}";
    }

    public static string EncountersByMap(string mapId)
    {
        return $"encounters:map:{mapId}";
    }

    // Status condition cache keys
    public static string StatusCondition(int id)
    {
        return $"status:{id}";
    }

    // Ability cache keys
    public static string Ability(int id)
    {
        return $"ability:{id}";
    }

    // Evolution cache keys
    public static string EvolutionChain(int speciesId)
    {
        return $"evolution:chain:{speciesId}";
    }

    public static string EvolutionsBySpecies(int speciesId)
    {
        return $"evolutions:species:{speciesId}";
    }

    // Template cache keys (from PokeSharp.Core)
    public static string Template(string templateId)
    {
        return $"template:{templateId}";
    }

    public static string TemplatesByTag(string tag)
    {
        return $"templates:tag:{tag}";
    }
}
