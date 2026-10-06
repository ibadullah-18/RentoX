namespace RentoX.Application.Authentication;

public interface IOtpOperationScope : IAsyncDisposable
{
    Task CommitAsync(
        CancellationToken cancellationToken = default);
}
