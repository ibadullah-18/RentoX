namespace RentoX.Contracts.Accounts;

/// <summary>Sent to delete the account; <c>Confirm</c> must be true.</summary>
public sealed record DeleteAccountRequest(bool Confirm);
