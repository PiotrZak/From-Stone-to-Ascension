using TTS.Core.Models;
using TTS.Core.Systems;

namespace TTS.Tests;

public class TechTreeCatalogTests
{
    [Fact]
    public void Catalog_LoadsAllTiers()
    {
        var path = TechTreeCatalog.ResolveDefaultPath();
        Assert.True(File.Exists(path), $"Missing catalog at {path}");

        var catalog = new TechTreeCatalog(path);

        Assert.True(catalog.IsLoaded);
        Assert.True(catalog.Technologies.Count >= 60);

        for (var tier = 1; tier <= 8; tier++)
            Assert.NotEmpty(catalog.GetForTier((TechTier)tier));
    }

    [Fact]
    public void Catalog_AllPrerequisitesExist()
    {
        var catalog = new TechTreeCatalog();
        var ids = catalog.Technologies.Select(t => t.Id).ToHashSet();

        var missing = catalog.Technologies
            .SelectMany(t => t.Prerequisites.Where(p => !ids.Contains(p)).Select(p => (t.Id, p)))
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void Catalog_ContainsDemoSpineIds()
    {
        var catalog = new TechTreeCatalog();
        var demoIds = new[]
        {
            "tech-agriculture", "tech-governance", "tech-metallurgy", "tech-steam",
            "tech-electrical", "tech-computing", "tech-cybersecurity", "tech-ml",
            "tech-agi", "tech-recursive-ai"
        };

        foreach (var id in demoIds)
            Assert.NotNull(catalog.GetById(id));
    }
}
