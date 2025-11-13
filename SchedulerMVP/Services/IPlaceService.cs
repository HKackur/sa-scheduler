using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services;

public interface IPlaceService
{
    Task<List<Place>> GetPlacesAsync();
    Task<Place?> GetPlaceAsync(Guid id);
    Task<Place> CreatePlaceAsync(Place place);
    Task<Place> UpdatePlaceAsync(Place place);
    Task DeletePlaceAsync(Guid placeId);
    Task<List<Area>> GetAreasForPlaceAsync(Guid placeId);
    Task<List<Leaf>> GetLeafsForPlaceAsync(Guid placeId);
    Task UpdateAreaPathAsync(Guid areaId, string newPath, Guid? newParentId);
    Task GenerateFootballTemplateAsync(Guid placeId);
    Task GeneratePoolTemplateAsync(Guid placeId, int lanes, bool splitInTwo);
    // Overloads with editable names/structure
    Task GenerateFootballTemplateAsync(Guid placeId, string helplanName, string halvAName, string halvBName, List<string> leafsA, List<string> leafsB);
    Task GenerateFootballTemplateAsync(Guid placeId, string helplanName, List<string> halvplanNames, List<List<string>> kvartsplanNames);
    Task GeneratePoolTemplateAsync(Guid placeId, List<string> laneNames, bool splitInTwo, string poolName, string? halfAName, string? halfBName);
    Task GeneratePoolTemplateAsync(Guid placeId, string poolName, List<string> halfNames, List<List<string>> laneNamesPerHalf);
    Task GenerateCustomTemplateAsync(Guid placeId, string topAreaName, List<string> leafNames);
    Task GenerateCustomTemplateAsync(Guid placeId, string topAreaName, List<string> zoneNames, List<List<string>> ytaNames);
    Task GenerateFromBlueprintAsync(Models.PlaceBlueprint blueprint);
    Task FixMissingAreaLeafRelationsAsync(Guid placeId);
}


