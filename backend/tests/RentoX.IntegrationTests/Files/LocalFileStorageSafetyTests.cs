using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RentoX.Application.Files;
using RentoX.Domain.Common.Exceptions;
using RentoX.Infrastructure.Files;

namespace RentoX.IntegrationTests.Files;

public sealed class LocalFileStorageSafetyTests : IDisposable
{
    private readonly string testRoot;
    private readonly string storageRoot;
    private readonly string outsidePath;
    private readonly LocalFileStorage storage;

    public LocalFileStorageSafetyTests()
    {
        testRoot = Path.Combine(
            Path.GetTempPath(),
            "RentoX-storage-safety",
            Guid.NewGuid().ToString("N"));

        storageRoot = Path.Combine(testRoot, "storage");
        outsidePath = Path.Combine(testRoot, "outside.txt");

        Directory.CreateDirectory(storageRoot);
        File.WriteAllText(outsidePath, "keep");

        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Storage:RootPath"] = storageRoot
                    })
                .Build();

        storage = new LocalFileStorage(
            new TestEnvironment
            {
                ContentRootPath = testRoot
            },
            configuration);
    }

    [Theory]
    [InlineData(FileStorageArea.ListingImages, "listing-images")]
    [InlineData(FileStorageArea.StoreLogos, "store-logos")]
    [InlineData(FileStorageArea.StoreCovers, "store-covers")]
    [InlineData(FileStorageArea.ProfileImages, "profile-images")]
    [InlineData(FileStorageArea.ChatAttachments, "chat-attachments")]
    public async Task SavedFileCanBeReadAndDeleted(
        FileStorageArea area,
        string folder)
    {
        byte[] expected = Encoding.UTF8.GetBytes("storage-test");
        using MemoryStream input = new(expected, writable: false);

        StoredFileResult saved = await storage.SaveAsync(
            area,
            input,
            "image/png",
            ".png");

        Assert.StartsWith(
            folder + "/",
            saved.StorageKey,
            StringComparison.Ordinal);

        Assert.Equal((long)expected.Length, saved.SizeBytes);

        Stream? opened = await storage.OpenReadAsync(
            saved.StorageKey);

        Assert.NotNull(opened);

        using (opened)
        using (MemoryStream output = new())
        {
            await opened.CopyToAsync(output);
            Assert.Equal(expected, output.ToArray());
        }

        await storage.DeleteAsync(saved.StorageKey);

        Stream? deleted = await storage.OpenReadAsync(
            saved.StorageKey);

        using (deleted)
        {
            Assert.Null(deleted);
        }

        Assert.Equal("keep", File.ReadAllText(outsidePath));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("../outside.txt")]
    [InlineData("chat-attachments/../../outside.txt")]
    [InlineData(@"..\outside.txt")]
    [InlineData("/outside.txt")]
    [InlineData("C:outside.txt")]
    [InlineData(@"C:\outside.txt")]
    [InlineData("//server/share/file.jpg")]
    [InlineData("chat-attachments//file.jpg")]
    [InlineData("chat-attachments/./file.jpg")]
    [InlineData("chat-attachments/../file.jpg")]
    [InlineData("chat-attachments/file.jpg:stream")]
    [InlineData("chat-attachments/file\0.jpg")]
    public async Task InvalidKeyCannotReadOrDeleteFiles(string? key)
    {
        await Assert.ThrowsAsync<DomainException>(
            () => storage.OpenReadAsync(key!));

        await Assert.ThrowsAsync<DomainException>(
            () => storage.DeleteAsync(key!));

        Assert.Equal("keep", File.ReadAllText(outsidePath));
    }

    [Fact]
    public async Task AbsolutePathInsideStorageIsAlsoRejected()
    {
        string filePath = Path.Combine(storageRoot, "existing.png");
        await File.WriteAllTextAsync(filePath, "keep");

        await Assert.ThrowsAsync<DomainException>(
            () => storage.OpenReadAsync(filePath));

        await Assert.ThrowsAsync<DomainException>(
            () => storage.DeleteAsync(filePath));

        Assert.Equal("keep", await File.ReadAllTextAsync(filePath));
    }

    [Fact]
    public async Task MissingFileReturnsNullAndCanBeDeletedAgain()
    {
        const string key =
            "chat-attachments/00000000000000000000000000000001.png";

        Stream? opened = await storage.OpenReadAsync(key);

        using (opened)
        {
            Assert.Null(opened);
        }

        await storage.DeleteAsync(key);
        await storage.DeleteAsync(key);

        Assert.Equal("keep", File.ReadAllText(outsidePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } =
            Environments.Development;

        public string ApplicationName { get; set; } =
            nameof(LocalFileStorageSafetyTests);

        public string ContentRootPath { get; set; } =
            string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}
