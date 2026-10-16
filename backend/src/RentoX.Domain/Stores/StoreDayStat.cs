namespace RentoX.Domain.Stores;

/// <summary>
/// How many times one store was looked at, and its contact buttons used, on one day (Baku day). Written by a
/// single "add one" statement, so counting is exact even when many visitors arrive together.
/// </summary>
public sealed class StoreDayStat
{
    public Guid StoreId { get; private set; }

    public DateOnly Day { get; private set; }

    public int Views { get; private set; }

    public int Calls { get; private set; }

    public int WhatsApp { get; private set; }

    public int Messages { get; private set; }

    /// <summary>The kinds of event the page may report.</summary>
    public static readonly string[] Kinds = ["view", "call", "whatsapp", "message"];
}
