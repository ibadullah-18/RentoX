using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace RentoX.Worker.PushNotifications;

public sealed record PushSendRequest(
    string Title,
    string Body,
    string? ActionUrl,
    Guid NotificationId,
    Guid? RelatedEntityId,
    IReadOnlyList<string> Tokens);

public sealed record PushSendResult(
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<string> InvalidTokens,
    string? Error);

public interface IPushNotificationSender
{
    Task<PushSendResult> SendAsync(
        PushSendRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class FirebasePushNotificationSender
    : IPushNotificationSender
{
    private readonly FirebaseMessaging messaging;

    public FirebasePushNotificationSender()
    {
        FirebaseApp app =
            FirebaseApp.Create(
                new AppOptions
                {
                    Credential =
                        GoogleCredential
                            .GetApplicationDefault()
                },
                "RentoX.Worker.Push");

        messaging =
            FirebaseMessaging.GetMessaging(app);
    }

    public async Task<PushSendResult> SendAsync(
        PushSendRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Tokens.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one push token is required.");
        }

        if (request.Tokens.Count > 500)
        {
            throw new InvalidOperationException(
                "Firebase accepts at most 500 tokens per request.");
        }

        Dictionary<string, string> data =
            new(StringComparer.Ordinal)
            {
                ["notificationId"] =
                    request.NotificationId.ToString(),
                ["actionUrl"] =
                    request.ActionUrl ?? string.Empty,
                ["relatedEntityId"] =
                    request.RelatedEntityId?.ToString()
                    ?? string.Empty
            };

        MulticastMessage message =
            new()
            {
#pragma warning disable CS0618
                Tokens = request.Tokens,
#pragma warning restore CS0618
                Notification =
                    new FirebaseAdmin.Messaging.Notification
                    {
                        Title = request.Title,
                        Body = request.Body
                    },
                Data = data
            };

        BatchResponse response =
            await messaging.SendEachForMulticastAsync(
                message,
                cancellationToken);

        List<string> invalidTokens = [];
        string? firstError = null;

        for (int index = 0;
             index < response.Responses.Count;
             index++)
        {
            SendResponse sendResponse =
                response.Responses[index];

            if (sendResponse.IsSuccess)
            {
                continue;
            }

            FirebaseMessagingException? exception =
                sendResponse.Exception;

            firstError ??= exception?.Message;

            if (exception?.MessagingErrorCode is
                MessagingErrorCode.Unregistered or
                MessagingErrorCode.SenderIdMismatch)
            {
                invalidTokens.Add(
                    request.Tokens[index]);
            }
        }

        return new PushSendResult(
            response.SuccessCount,
            response.FailureCount,
            invalidTokens,
            firstError);
    }
}