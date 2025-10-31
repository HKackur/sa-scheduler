using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Services;

public class PlaceService : IPlaceService
{
    private readonly AppDbContext _db;
    private readonly UserContextService _userContext;

    public PlaceService(AppDbContext db, UserContextService userContext)
    {
        _db = db;
        _userContext = userContext;
    }

    public async Task<List<Place>> GetPlacesAsync()
    {
        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();

        var query = _db.Places.AsQueryable();

        // Admin can see all places, regular users only their own
        if (!isAdmin && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(p => p.UserId == userId);
        }

        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<Place?> GetPlaceAsync(Guid id)
    {
        var place = await _db.Places.FirstOrDefaultAsync(p => p.Id == id);
        if (place == null) return null;

        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();

        // Check access rights
        if (!isAdmin && !string.IsNullOrEmpty(userId) && place.UserId != userId && place.UserId != null)
        {
            return null; // User doesn't have access
        }

        return place;
    }

    public async Task<Place> CreatePlaceAsync(Place place)
    {
        if (place.Id == Guid.Empty) place.Id = Guid.NewGuid();

        // Set UserId for the current user
        var userId = _userContext.GetCurrentUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            place.UserId = userId;
        }

        _db.Places.Add(place);
        await _db.SaveChangesAsync();
        return place;
    }

    public async Task<Place> UpdatePlaceAsync(Place place)
    {
        // Verify access
        var existingPlace = await _db.Places.FindAsync(place.Id);
        if (existingPlace == null) throw new InvalidOperationException("Place not found");

        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();

        if (!isAdmin && !string.IsNullOrEmpty(userId) && existingPlace.UserId != userId && existingPlace.UserId != null)
        {
            throw new UnauthorizedAccessException("You don't have permission to update this place");
        }

        // Ensure UserId is preserved or set
        if (string.IsNullOrEmpty(place.UserId))
        {
            place.UserId = existingPlace.UserId ?? userId;
        }

        _db.Places.Update(place);
        await _db.SaveChangesAsync();
        return place;
    }

    public async Task DeletePlaceAsync(Guid placeId)
    {
        // Verify access
        var userId = _userContext.GetCurrentUserId();
        var isAdmin = await _userContext.IsAdminAsync();

        // Delete all related data in correct order due to foreign key constraints
        var place = await _db.Places.FindAsync(placeId);
        
        if (place == null) return;

        // Check access
        if (!isAdmin && !string.IsNullOrEmpty(userId) && place.UserId != userId && place.UserId != null)
        {
            throw new UnauthorizedAccessException("You don't have permission to delete this place");
        }
        if (place != null)
        {
            // Delete booking templates first
            var bookingTemplates = await _db.BookingTemplates
                .Where(bt => _db.ScheduleTemplates.Any(st => st.Id == bt.ScheduleTemplateId && st.PlaceId == placeId))
                .ToListAsync();
            _db.BookingTemplates.RemoveRange(bookingTemplates);

            // Delete schedule templates
            var scheduleTemplates = await _db.ScheduleTemplates.Where(st => st.PlaceId == placeId).ToListAsync();
            _db.ScheduleTemplates.RemoveRange(scheduleTemplates);

            // Delete area-leaf relationships
            var areaLeafs = await _db.AreaLeafs
                .Where(al => _db.Areas.Any(a => a.Id == al.AreaId && a.PlaceId == placeId))
                .ToListAsync();
            _db.AreaLeafs.RemoveRange(areaLeafs);

            // Delete areas
            var areas = await _db.Areas.Where(a => a.PlaceId == placeId).ToListAsync();
            _db.Areas.RemoveRange(areas);

            // Delete leafs
            var leafs = await _db.Leafs.Where(l => l.PlaceId == placeId).ToListAsync();
            _db.Leafs.RemoveRange(leafs);

            // Finally delete the place
            _db.Places.Remove(place);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<Area>> GetAreasForPlaceAsync(Guid placeId) => await _db.Areas.Where(a => a.PlaceId == placeId).OrderBy(a => a.Path).ToListAsync();

    public async Task<List<Leaf>> GetLeafsForPlaceAsync(Guid placeId) => await _db.Leafs.Where(l => l.PlaceId == placeId).OrderBy(l => l.SortOrder).ToListAsync();

    public async Task UpdateAreaPathAsync(Guid areaId, string newPath, Guid? newParentId)
    {
        var area = await _db.Areas.FirstAsync(a => a.Id == areaId);
        area.Path = newPath;
        area.ParentAreaId = newParentId;
        await _db.SaveChangesAsync();
    }

    public async Task GenerateFootballTemplateAsync(Guid placeId)
    {
        // Leafs: A1, A2, B1, B2
        var names = new[] { "A1", "A2", "B1", "B2" };
        var leafs = new List<Leaf>();
        var sort = 1;
        foreach (var n in names)
        {
            var leaf = new Leaf { Id = Guid.NewGuid(), PlaceId = placeId, Name = n, SortOrder = sort++ };
            leafs.Add(leaf);
        }
        await _db.Leafs.AddRangeAsync(leafs);

        // Areas
        var helplan = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = "Helplan", Path = "/Helplan" };
        var halvA = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = "Halvplan A", ParentAreaId = helplan.Id, Path = "/Helplan/Halvplan A" };
        var halvB = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = "Halvplan B", ParentAreaId = helplan.Id, Path = "/Helplan/Halvplan B" };
        var kvA1 = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = "Kvartsplan A1", ParentAreaId = halvA.Id, Path = "/Helplan/Halvplan A/Kvartsplan A1" };
        var kvA2 = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = "Kvartsplan A2", ParentAreaId = halvA.Id, Path = "/Helplan/Halvplan A/Kvartsplan A2" };
        var kvB1 = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = "Kvartsplan B1", ParentAreaId = halvB.Id, Path = "/Helplan/Halvplan B/Kvartsplan B1" };
        var kvB2 = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = "Kvartsplan B2", ParentAreaId = halvB.Id, Path = "/Helplan/Halvplan B/Kvartsplan B2" };
        await _db.Areas.AddRangeAsync(helplan, halvA, halvB, kvA1, kvA2, kvB1, kvB2);

        // Coverage
        var a1 = leafs.First(l => l.Name == "A1");
        var a2 = leafs.First(l => l.Name == "A2");
        var b1 = leafs.First(l => l.Name == "B1");
        var b2 = leafs.First(l => l.Name == "B2");

        await _db.AreaLeafs.AddRangeAsync(
            // Helplan covers all
            new AreaLeaf { AreaId = helplan.Id, LeafId = a1.Id },
            new AreaLeaf { AreaId = helplan.Id, LeafId = a2.Id },
            new AreaLeaf { AreaId = helplan.Id, LeafId = b1.Id },
            new AreaLeaf { AreaId = helplan.Id, LeafId = b2.Id },
            // Halvplan A covers A1+A2
            new AreaLeaf { AreaId = halvA.Id, LeafId = a1.Id },
            new AreaLeaf { AreaId = halvA.Id, LeafId = a2.Id },
            // Halvplan B covers B1+B2
            new AreaLeaf { AreaId = halvB.Id, LeafId = b1.Id },
            new AreaLeaf { AreaId = halvB.Id, LeafId = b2.Id },
            // Kvartsplan map 1:1
            new AreaLeaf { AreaId = kvA1.Id, LeafId = a1.Id },
            new AreaLeaf { AreaId = kvA2.Id, LeafId = a2.Id },
            new AreaLeaf { AreaId = kvB1.Id, LeafId = b1.Id },
            new AreaLeaf { AreaId = kvB2.Id, LeafId = b2.Id }
        );

        await _db.SaveChangesAsync();
    }

    public async Task GeneratePoolTemplateAsync(Guid placeId, int lanes, bool splitInTwo)
    {
        // Leafs = lanes (1..lanes)
        var leafs = new List<Leaf>();
        for (var i = 1; i <= lanes; i++)
        {
            leafs.Add(new Leaf { Id = Guid.NewGuid(), PlaceId = placeId, Name = $"Bana {i}", SortOrder = i });
        }
        await _db.Leafs.AddRangeAsync(leafs);

        // Areas: Hel bassäng + ev. Halva A/B + en area per bana
        var bass = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = "Hel bassäng", Path = "/Hel bassäng" };
        await _db.Areas.AddAsync(bass);

        Area? halvA = null; Area? halvB = null;
        if (splitInTwo)
        {
            halvA = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = "Halv bassäng A", ParentAreaId = bass.Id, Path = "/Hel bassäng/Halv A" };
            halvB = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = "Halv bassäng B", ParentAreaId = bass.                                                                                                                                                               Id, Path = "/Hel bassäng/Halv B" };
            await _db.Areas.AddRangeAsync(halvA, halvB);
        }

        var laneAreas = new List<Area>();
        for (var i = 1; i <= lanes; i++)
        {
            var a = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = $"Bana {i}", ParentAreaId = bass.Id, Path = $"/Hel bassäng/Bana {i}" };
            laneAreas.Add(a);
        }
        await _db.Areas.AddRangeAsync(laneAreas);

        // Coverage: Hel bassäng covers all lanes; Halv A/B covers half; each lane covers itself
        foreach (var l in leafs)
        {
            await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = bass.Id, LeafId = l.Id });
        }
        if (splitInTwo)
        {
            var half = lanes / 2;
            for (var i = 0; i < lanes; i++)
            {
                var target = i < half ? halvA! : halvB!;
                await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = target.Id, LeafId = leafs[i].Id });
            }
        }
        for (var i = 0; i < lanes; i++)
        {
            await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = laneAreas[i].Id, LeafId = leafs[i].Id });
        }

        await _db.SaveChangesAsync();
    }

    // Editable football template generation
    public async Task GenerateFootballTemplateAsync(Guid placeId, string helplanName, string halvAName, string halvBName, List<string> leafsA, List<string> leafsB)
    {
        var leafs = new List<Leaf>();
        var sort = 1;
        foreach (var n in leafsA)
        {
            leafs.Add(new Leaf { Id = Guid.NewGuid(), PlaceId = placeId, Name = n, SortOrder = sort++ });
        }
        foreach (var n in leafsB)
        {
            leafs.Add(new Leaf { Id = Guid.NewGuid(), PlaceId = placeId, Name = n, SortOrder = sort++ });
        }
        await _db.Leafs.AddRangeAsync(leafs);

        var hel = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = helplanName, Path = $"/{helplanName}" };
        var a = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = halvAName, ParentAreaId = hel.Id, Path = $"/{helplanName}/{halvAName}" };
        var b = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = halvBName, ParentAreaId = hel.Id, Path = $"/{helplanName}/{halvBName}" };
        await _db.Areas.AddRangeAsync(hel, a, b);

        foreach (var l in leafs)
        {
            await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = hel.Id, LeafId = l.Id });
        }
        foreach (var n in leafsA)
        {
            var leaf = leafs.First(x => x.Name == n);
            await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = a.Id, LeafId = leaf.Id });
        }
        foreach (var n in leafsB)
        {
            var leaf = leafs.First(x => x.Name == n);
            await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = b.Id, LeafId = leaf.Id });
        }

        await _db.SaveChangesAsync();
    }

    // Dynamic football template generation with variable halvplan and kvartsplan counts
    public async Task GenerateFootballTemplateAsync(Guid placeId, string helplanName, List<string> halvplanNames, List<List<string>> kvartsplanNames)
    {
        // Create all leafs first
        var leafs = new List<Leaf>();
        var sort = 1;
        foreach (var kvartsplanGroup in kvartsplanNames)
        {
            foreach (var kvartsplanName in kvartsplanGroup)
            {
                leafs.Add(new Leaf { Id = Guid.NewGuid(), PlaceId = placeId, Name = kvartsplanName, SortOrder = sort++ });
            }
        }
        await _db.Leafs.AddRangeAsync(leafs);

        // Create areas
        var areas = new List<Area>();
        var hel = new Area 
        { 
            Id = Guid.NewGuid(), 
            PlaceId = placeId, 
            Name = helplanName, 
            Path = $"/{helplanName}",
            Level1WidthPercent = 100, // Helplan shows itself at 100%
            Level2WidthPercent = 50,  // Helplan shows halvplaner at 50%
            Level3WidthPercent = 25   // Helplan shows kvartsplaner at 25%
        };
        areas.Add(hel);

        // Create halvplan areas
        var halvplanAreas = new List<Area>();
        for (var i = 0; i < halvplanNames.Count; i++)
        {
            var halvplanArea = new Area 
            { 
                Id = Guid.NewGuid(), 
                PlaceId = placeId, 
                Name = halvplanNames[i], 
                ParentAreaId = hel.Id, 
                Path = $"/{helplanName}/{halvplanNames[i]}",
                Level1WidthPercent = 100, // Halvplan shows helplan at 100%
                Level2WidthPercent = 100, // Halvplan shows itself at 100%
                Level3WidthPercent = 50   // Halvplan shows kvartsplaner at 50%
            };
            halvplanAreas.Add(halvplanArea);
            areas.Add(halvplanArea);
        }

        // Create kvartsplan areas
        var kvartsplanAreas = new List<Area>();
        for (var i = 0; i < halvplanNames.Count; i++)
        {
            foreach (var kvartsplanName in kvartsplanNames[i])
            {
                var kvartsplanArea = new Area 
                { 
                    Id = Guid.NewGuid(), 
                    PlaceId = placeId, 
                    Name = kvartsplanName, 
                    ParentAreaId = halvplanAreas[i].Id, 
                    Path = $"/{helplanName}/{halvplanNames[i]}/{kvartsplanName}",
                    Level1WidthPercent = 100, // Kvartsplan shows helplan at 100%
                    Level2WidthPercent = 100, // Kvartsplan shows halvplan at 100%
                    Level3WidthPercent = 100  // Kvartsplan shows itself at 100%
                };
                kvartsplanAreas.Add(kvartsplanArea);
                areas.Add(kvartsplanArea);
            }
        }

        await _db.Areas.AddRangeAsync(areas);

        // Create AreaLeaf coverage
        var areaLeafs = new List<AreaLeaf>();

        // Helplan covers all leafs
        foreach (var leaf in leafs)
        {
            areaLeafs.Add(new AreaLeaf { AreaId = hel.Id, LeafId = leaf.Id });
        }

        // Each halvplan covers its kvartsplan leafs
        for (var i = 0; i < halvplanNames.Count; i++)
        {
            foreach (var kvartsplanName in kvartsplanNames[i])
            {
                var leaf = leafs.First(l => l.Name == kvartsplanName);
                areaLeafs.Add(new AreaLeaf { AreaId = halvplanAreas[i].Id, LeafId = leaf.Id });
            }
        }

        // Each kvartsplan covers its own leaf
        foreach (var kvartsplanArea in kvartsplanAreas)
        {
            var leaf = leafs.First(l => l.Name == kvartsplanArea.Name);
            areaLeafs.Add(new AreaLeaf { AreaId = kvartsplanArea.Id, LeafId = leaf.Id });
        }

        await _db.AreaLeafs.AddRangeAsync(areaLeafs);
        await _db.SaveChangesAsync();
    }

    // Editable pool template generation
    public async Task GeneratePoolTemplateAsync(Guid placeId, List<string> laneNames, bool splitInTwo, string poolName, string? halfAName, string? halfBName)
    {
        var leafs = laneNames.Select((n, i) => new Leaf { Id = Guid.NewGuid(), PlaceId = placeId, Name = n, SortOrder = i + 1 }).ToList();
        await _db.Leafs.AddRangeAsync(leafs);

        var pool = new Area 
        { 
            Id = Guid.NewGuid(), 
            PlaceId = placeId, 
            Name = poolName, 
            Path = $"/{poolName}",
            Level1WidthPercent = 100, // Pool shows itself at 100%
            Level2WidthPercent = 50,  // Pool shows halves at 50% (if split)
            Level3WidthPercent = 100  // Pool shows lanes at 100%
        };
        await _db.Areas.AddAsync(pool);
        Area? a = null; Area? b = null;
        if (splitInTwo)
        {
            a = new Area 
            { 
                Id = Guid.NewGuid(), 
                PlaceId = placeId, 
                Name = halfAName ?? "Halv A", 
                ParentAreaId = pool.Id, 
                Path = $"/{poolName}/{halfAName ?? "Halv A"}",
                Level1WidthPercent = 100, // Half shows pool at 100%
                Level2WidthPercent = 100, // Half shows itself at 100%
                Level3WidthPercent = 100  // Half shows lanes at 100%
            };
            b = new Area 
            { 
                Id = Guid.NewGuid(), 
                PlaceId = placeId, 
                Name = halfBName ?? "Halv B", 
                ParentAreaId = pool.Id, 
                Path = $"/{poolName}/{halfBName ?? "Halv B"}",
                Level1WidthPercent = 100, // Half shows pool at 100%
                Level2WidthPercent = 100, // Half shows itself at 100%
                Level3WidthPercent = 100  // Half shows lanes at 100%
            };
            await _db.Areas.AddRangeAsync(a, b);
        }

        var laneAreas = new List<Area>();
        foreach (var n in laneNames)
        {
            laneAreas.Add(new Area 
            { 
                Id = Guid.NewGuid(), 
                PlaceId = placeId, 
                Name = n, 
                ParentAreaId = pool.Id, 
                Path = $"/{poolName}/{n}",
                Level1WidthPercent = 100, // Lane shows pool at 100%
                Level2WidthPercent = 100, // Lane shows half at 100% (if split)
                Level3WidthPercent = 100  // Lane shows itself at 100%
            });
        }
        await _db.Areas.AddRangeAsync(laneAreas);

        foreach (var l in leafs)
        {
            await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = pool.Id, LeafId = l.Id });
        }
        if (splitInTwo && a != null && b != null)
        {
            var half = laneAreas.Count / 2;
            for (var i = 0; i < laneAreas.Count; i++)
            {
                var target = i < half ? a : b;
                await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = target!.Id, LeafId = leafs[i].Id });
            }
        }
        for (var i = 0; i < laneAreas.Count; i++)
        {
            await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = laneAreas[i].Id, LeafId = leafs[i].Id });
        }

        await _db.SaveChangesAsync();
    }

    public async Task GenerateCustomTemplateAsync(Guid placeId, string topAreaName, List<string> leafNames)
    {
        var leafs = leafNames.Select((n, i) => new Leaf { Id = Guid.NewGuid(), PlaceId = placeId, Name = n, SortOrder = i + 1 }).ToList();
        await _db.Leafs.AddRangeAsync(leafs);
        
        // Create top area
        var top = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = topAreaName, Path = $"/{topAreaName}" };
        await _db.Areas.AddAsync(top);
        
        // Create individual areas for each leaf (yta)
        var areas = new List<Area>();
        for (var i = 0; i < leafNames.Count; i++)
        {
            var leafName = leafNames[i];
            var area = new Area 
            { 
                Id = Guid.NewGuid(), 
                PlaceId = placeId, 
                Name = leafName, 
                ParentAreaId = top.Id, 
                Path = $"/{topAreaName}/{leafName}" 
            };
            areas.Add(area);
        }
        await _db.Areas.AddRangeAsync(areas);
        
        // Link each leaf both to its own area and the top level for correct ghost widths
        for (var i = 0; i < leafs.Count; i++)
        {
            var leaf = leafs[i];
            await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = areas[i].Id, LeafId = leaf.Id });
            await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = top.Id, LeafId = leaf.Id });
        }
        
        await _db.SaveChangesAsync();
    }

    public async Task GenerateCustomTemplateAsync(Guid placeId, string topAreaName, List<string> zoneNames, List<List<string>> ytaNames)
    {
        // Create all leafs (yta names) with unique IDs
        var allYtas = ytaNames.SelectMany(x => x).ToList();
        var leafs = allYtas.Select((n, i) => new Leaf { Id = Guid.NewGuid(), PlaceId = placeId, Name = n, SortOrder = i + 1 }).ToList();
        await _db.Leafs.AddRangeAsync(leafs);

        // Create top area
        var top = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = topAreaName, Path = $"/{topAreaName}" };
        await _db.Areas.AddAsync(top);

        // Create zone areas and individual yta areas
        var zoneAreas = new List<Area>();
        for (var i = 0; i < zoneNames.Count; i++)
        {
            var zone = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = zoneNames[i], ParentAreaId = top.Id, Path = $"/{topAreaName}/{zoneNames[i]}" };
            await _db.Areas.AddAsync(zone);
            zoneAreas.Add(zone);

            // Create individual areas for each yta in this zone
            var zoneYtas = ytaNames[i];
            var zoneLeafIds = new HashSet<Guid>();
            foreach (var ytaName in zoneYtas)
            {
                var ytaArea = new Area { Id = Guid.NewGuid(), PlaceId = placeId, Name = ytaName, ParentAreaId = zone.Id, Path = $"/{topAreaName}/{zoneNames[i]}/{ytaName}" };
                await _db.Areas.AddAsync(ytaArea);

                // Link the yta area to its leaf
                var leaf = leafs.FirstOrDefault(x => x.Name == ytaName);
                if (leaf != null)
                {
                    await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = ytaArea.Id, LeafId = leaf.Id });
                    zoneLeafIds.Add(leaf.Id);
                }
            }

            foreach (var leafId in zoneLeafIds)
            {
                await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = zone.Id, LeafId = leafId });
            }
        }

        foreach (var leaf in leafs)
        {
            await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = top.Id, LeafId = leaf.Id });
        }

        await _db.SaveChangesAsync();
    }

    public async Task GenerateFromBlueprintAsync(Models.PlaceBlueprint blueprint)
    {
        // Create leafs
        var leafs = blueprint.LeafNames.Select((n, i) => new Leaf { Id = Guid.NewGuid(), PlaceId = blueprint.PlaceId, Name = n, SortOrder = i + 1 }).ToList();
        await _db.Leafs.AddRangeAsync(leafs);

        // Top area covers all
        var top = new Area { Id = Guid.NewGuid(), PlaceId = blueprint.PlaceId, Name = blueprint.TopAreaName, Path = $"/{blueprint.TopAreaName}" };
        await _db.Areas.AddAsync(top);
        foreach (var l in leafs)
        {
            await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = top.Id, LeafId = l.Id });
        }

        // Mid areas cover configured subsets
        foreach (var mid in blueprint.MidAreas)
        {
            var area = new Area { Id = Guid.NewGuid(), PlaceId = blueprint.PlaceId, Name = mid.Name, ParentAreaId = top.Id, Path = $"/{blueprint.TopAreaName}/{mid.Name}" };
            await _db.Areas.AddAsync(area);
            foreach (var name in mid.LeafNames)
            {
                var leaf = leafs.FirstOrDefault(x => x.Name == name);
                if (leaf != null)
                {
                    await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = area.Id, LeafId = leaf.Id });
                }
            }
        }

        await _db.SaveChangesAsync();
    }

    // Fix missing AreaLeaf relations for existing places
    public async Task FixMissingAreaLeafRelationsAsync(Guid placeId)
    {
        var place = await _db.Places.FindAsync(placeId);
        if (place == null) return;

        // Get all areas for this place
        var areas = await _db.Areas.Where(a => a.PlaceId == placeId).ToListAsync();
        var leafs = await _db.Leafs.Where(l => l.PlaceId == placeId).ToListAsync();
        
        // Find the top area (no parent)
        var topArea = areas.FirstOrDefault(a => a.ParentAreaId == null);
        if (topArea == null) return;

        Console.WriteLine($"Fixing AreaLeaf relations for place '{place.Name}' with {leafs.Count} leafs");

        // Ensure top area covers all leafs
        foreach (var leaf in leafs)
        {
            var existingRelation = await _db.AreaLeafs
                .FirstOrDefaultAsync(al => al.AreaId == topArea.Id && al.LeafId == leaf.Id);
            
            if (existingRelation == null)
            {
                await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = topArea.Id, LeafId = leaf.Id });
                Console.WriteLine($"Added top area relation: {topArea.Name} -> {leaf.Name}");
            }
        }

        // Ensure zone areas cover their leafs
        var zoneAreas = areas.Where(a => a.ParentAreaId == topArea.Id).ToList();
        foreach (var zoneArea in zoneAreas)
        {
            // Find leafs that belong to this zone (through yta areas)
            var ytaAreas = areas.Where(a => a.ParentAreaId == zoneArea.Id).ToList();
            var zoneLeafIds = new HashSet<Guid>();
            
            foreach (var ytaArea in ytaAreas)
            {
                var ytaLeafs = await _db.AreaLeafs
                    .Where(al => al.AreaId == ytaArea.Id)
                    .Select(al => al.LeafId)
                    .ToListAsync();
                
                foreach (var leafId in ytaLeafs)
                {
                    zoneLeafIds.Add(leafId);
                }
            }

            // Add missing zone-leaf relations
            foreach (var leafId in zoneLeafIds)
            {
                var existingRelation = await _db.AreaLeafs
                    .FirstOrDefaultAsync(al => al.AreaId == zoneArea.Id && al.LeafId == leafId);
                
                if (existingRelation == null)
                {
                    await _db.AreaLeafs.AddAsync(new AreaLeaf { AreaId = zoneArea.Id, LeafId = leafId });
                    var leafName = leafs.First(l => l.Id == leafId).Name;
                    Console.WriteLine($"Added zone area relation: {zoneArea.Name} -> {leafName}");
                }
            }
        }

        await _db.SaveChangesAsync();
        Console.WriteLine($"Fixed AreaLeaf relations for place '{place.Name}'");
    }
}

