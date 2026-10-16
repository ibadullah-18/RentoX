using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using RentoX.Application.Files;

namespace RentoX.Infrastructure.Files;

/// <summary>
/// The files in Cloudflare R2 (or any S3-compatible storage), so they do not depend on one server: the server can be
/// replaced and the photos are still there. Same keys as the disk storage ("listing-images/….webp"), so files move
/// between the two without renaming.
/// </summary>
/// <remarks>
/// Settings (Storage:R2:*): ServiceUrl (https://&lt;account id&gt;.r2.cloudflarestorage.com), Bucket, AccessKeyId,
/// SecretAccessKey. The token should be limited to this one bucket.
/// </remarks>
public sealed class R2FileStorage : IFileStorage, IDisposable
{
    private readonly AmazonS3Client client;
    private readonly string bucket;
    private readonly bool unsignedPayload;

    public R2FileStorage(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string serviceUrl = Required(configuration, "ServiceUrl");
        bucket = Required(configuration, "Bucket");
        // Over HTTPS the body need not be signed (R2 accepts that and it is faster); plain HTTP (a test server) needs it.
        unsignedPayload = serviceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        AmazonS3Config config = new()
        {
            ServiceURL = serviceUrl,
            // R2 has no regions: any value works, "auto" is what Cloudflare documents.
            AuthenticationRegion = "auto",
            ForcePathStyle = true,
            // R2 does not accept the extra checksum headers the SDK adds by default.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
        };

        client = new AmazonS3Client(
            new BasicAWSCredentials(
                Required(configuration, "AccessKeyId"),
                Required(configuration, "SecretAccessKey")),
            config);
    }

    public async Task<StoredFileResult> SaveAsync(
        FileStorageArea area,
        Stream content,
        string contentType,
        string extension,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        string storageKey = StorageKeyRules.NewKey(area, extension);

        // A photo is at most a few megabytes: it is held in memory so the size is known before it is sent.
        using MemoryStream buffer = new();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        PutObjectRequest request = new()
        {
            BucketName = bucket,
            Key = storageKey,
            InputStream = buffer,
            ContentType = contentType,
            AutoCloseStream = false,
            DisablePayloadSigning = unsignedPayload
        };

        await client.PutObjectAsync(request, cancellationToken);
        return new StoredFileResult(storageKey, contentType, buffer.Length);
    }

    public async Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        StorageKeyRules.RequireValidKey(storageKey);

        try
        {
            GetObjectResponse response = await client.GetObjectAsync(bucket, storageKey, cancellationToken);
            return new ResponseStream(response);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        StorageKeyRules.RequireValidKey(storageKey);

        // Deleting what is not there is not an error (the same as on the disk).
        await client.DeleteObjectAsync(bucket, storageKey, cancellationToken);
    }

    public void Dispose() => client.Dispose();

    private static string Required(IConfiguration configuration, string name)
    {
        string? value = configuration[$"Storage:R2:{name}"];
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException(
                $"Storage:R2:{name} is missing. Set it, or use Storage:Provider=Disk.")
            : value.Trim();
    }

    /// <summary>The file stream of an answer, which also closes the answer when the reader is done.</summary>
    private sealed class ResponseStream(GetObjectResponse response) : Stream
    {
        private readonly Stream inner = response.ResponseStream;

        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => response.ContentLength;

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            inner.ReadAsync(buffer, offset, count, cancellationToken);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            inner.ReadAsync(buffer, cancellationToken);

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                response.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
