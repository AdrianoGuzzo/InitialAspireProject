using InitialAspireProject.ApiIdentity.Repository;
using Microsoft.EntityFrameworkCore;

namespace InitialAspireProject.Tests.ApiIdentity;

public class ApplicationDbContextTests
{
    [Fact]
    public void Constructor_WithValidOptions_CreatesContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("TestIdentityDb_Constructor")
            .Options;

        using var context = new ApplicationDbContext(options);

        Assert.NotNull(context);
    }

    [Fact]
    public async Task OnModelCreating_ConfiguresIdentityEntities()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("TestIdentityDb_OnModelCreating")
            .Options;

        await using var context = new ApplicationDbContext(options);

        // Accessing Users triggers model building (calls OnModelCreating)
        var count = await context.Users.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, count);
    }
}
