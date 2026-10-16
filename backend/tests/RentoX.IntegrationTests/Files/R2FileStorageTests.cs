using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RentoX.Application.Files;
using RentoX.Domain.Common.Exceptions;
using RentoX.Infrastructure.Files;

namespace RentoX.IntegrationTests.Files;

/// <summary>
/// The R2 storage against a real S3-compatible server (a small mock in Docker): files are written, read back,
/// deleted, and bad keys are refused. Cloudflare R2 speaks the same protocol.
/// </summary>
public sealed class R2FileStorageTests : IAsyncLifetime, IDisposable
{
    private const string Bucket = "rentox-test";

    private readonly IContainer server = new ContainerBuilder("adobe/s3mock:latest")
        .WithPortBinding(9090, true)
        .WithEnvironment("COM_ADOBE_TESTING_S3MOCK_STORE_INITIAL_BUCKETS", Bucket)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Started S3MockApplication"))
        .Build();

    private R2FileStorage storage = null!;

    public async Task InitializeAsync()
    {
        await server.StartAsync();

        storage = new R2FileStorage(Configuration(new Dictionary<string, string?>
        {
            ["Storage:R2:ServiceUrl"] = $"http://{server.Hostname}:{server.GetMappedPublicPort(9090)}",
            ["Storage:R2:Bucket"] = Bucket,
            ["Storage:R2:AccessKeyId"] = "test",
            ["Storage:R2:SecretAccessKey"] = "test"
        }));
    }

    public async Task DisposeAsync() => await server.DisposeAsync();

    public void Dispose() => storage?.Dispose();

    [Fact]
    public async Task AFileIsSavedReadBackAndDeletedWithTheSameKeyShapeAsTheDisk()
    {
        byte[] bytes = [1, 2, 3, 4, 5, 250, 251];

        StoredFileResult saved = await storage.SaveAsync(
            FileStorageArea.ListingImages, new MemoryStream(bytes), "image/webp", ".webp");

        Assert.Matches("^listing-images/[0-9a-f]{32}\\.webp$", saved.StorageKey);
        Assert.Equal(bytes.Length, saved.SizeBytes);

        await using (Stream? read = await storage.OpenReadAsync(saved.StorageKey))
        {
            Assert.NotNull(read);
            using MemoryStream copy = new();
            await read.CopyToAsync(copy);
            Assert.Equal(bytes, copy.ToArray());
        }

        await storage.DeleteAsync(saved.StorageKey);
        Assert.Null(await storage.OpenReadAsync(saved.StorageKey));

        // Deleting what is not there is fine (as on the disk).
        await storage.DeleteAsync(saved.StorageKey);
    }

    [Fact]
    public async Task EachAreaHasItsOwnFolder()
    {
        StoredFileResult logo = await storage.SaveAsync(
            FileStorageArea.StoreLogos, new MemoryStream([9]), "image/png", "PNG");
        StoredFileResult chat = await storage.SaveAsync(
            FileStorageArea.ChatAttachments, new MemoryStream([9]), "image/jpeg", ".jpg");

        Assert.StartsWith("store-logos/", logo.StorageKey);
        Assert.EndsWith(".png", logo.StorageKey);
        Assert.StartsWith("chat-attachments/", chat.StorageKey);
    }

    [Fact]
    public async Task AMissingFileIsNullAndBadKeysOrExtensionsAreRefused()
    {
        Assert.Null(await storage.OpenReadAsync("listing-images/00000000000000000000000000000000.webp"));

        await Assert.ThrowsAsync<DomainException>(() => storage.OpenReadAsync("../secret.png"));
        await Assert.ThrowsAsync<DomainException>(() => storage.OpenReadAsync("/etc/passwd"));
        await Assert.ThrowsAsync<DomainException>(() => storage.DeleteAsync("listing-images/..\\x.png"));
        await Assert.ThrowsAsync<DomainException>(() =>
            storage.SaveAsync(FileStorageArea.ListingImages, new MemoryStream([1]), "text/html", ".html"));
    }

    [Fact]
    public void TheSettingPicksTheDiskByDefaultAndRefusesAnUnknownKind()
    {
        static IFileStorage Resolve(Dictionary<string, string?> settings)
        {
            ServiceCollection services = new();
            services.AddSingleton(Configuration(settings));
            services.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(new TestEnvironment());
            services.AddRentoXFileStorage();
            return services.BuildServiceProvider().GetRequiredService<IFileStorage>();
        }

        Assert.IsType<LocalFileStorage>(Resolve([]));
        Assert.IsType<LocalFileStorage>(Resolve(new() { ["Storage:Provider"] = "Disk" }));
        Assert.Throws<InvalidOperationException>(() => Resolve(new() { ["Storage:Provider"] = "Dropbox" }));
        // R2 without its settings stops at start instead of failing later on the first photo.
        Assert.Throws<InvalidOperationException>(() => Resolve(new() { ["Storage:Provider"] = "R2" }));
    }

    private sealed class TestEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";

        public string ApplicationName { get; set; } = "Test";

        public string ContentRootPath { get; set; } = Path.GetTempPath();

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private static IConfiguration Configuration(Dictionary<string, string?> settings) =>
        new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
}
