using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.CMU14.AdminAI;

public sealed record AISafeFaqTicket(Guid Id, Guid Channel, int Round, long Epoch, TimeSpan Created);

/// <summary>Game-thread state only: one current request per player, finite retention and cooldown.</summary>
public sealed class AISafeFaqSessionStore
{
    private readonly Dictionary<Guid, (AISafeFaqTicket? Pending, TimeSpan Next)> _channels = new();
    private long _epoch;
    private TimeSpan _globalNext;
    public int Count => _channels.Count;

    public AISafeFaqTicket? Begin(Guid channel, int round, TimeSpan now)
    {
        Sweep(now);
        if (channel == Guid.Empty || now < _globalNext || _channels.Count >= 128 && !_channels.ContainsKey(channel)) return null;
        if (_channels.TryGetValue(channel, out var current) && now < current.Next) return null;
        var ticket = new AISafeFaqTicket(Guid.NewGuid(), channel, round, _epoch, now);
        _channels[channel] = (ticket, now + TimeSpan.FromSeconds(5));
        _globalNext = now + TimeSpan.FromMilliseconds(500);
        return ticket;
    }

    public void Invalidate(Guid channel)
    {
        if (_channels.TryGetValue(channel, out var current)) _channels[channel] = (null, current.Next);
    }

    public bool Complete(AISafeFaqTicket ticket, int round, TimeSpan now, bool eligible)
    {
        if (!_channels.TryGetValue(ticket.Channel, out var current) || current.Pending?.Id != ticket.Id) return false;
        _channels[ticket.Channel] = (null, current.Next);
        return eligible && round == ticket.Round && ticket.Epoch == _epoch && now - ticket.Created < TimeSpan.FromSeconds(5);
    }

    public void Discard(AISafeFaqTicket ticket)
    {
        if (_channels.TryGetValue(ticket.Channel, out var current) && current.Pending?.Id == ticket.Id) Invalidate(ticket.Channel);
    }

    public void Reset() { _epoch++; _channels.Clear(); _globalNext = TimeSpan.Zero; }
    public void Sweep(TimeSpan now)
    {
        foreach (var channel in _channels.Where(pair => now >= pair.Value.Next).Select(pair => pair.Key).ToArray()) _channels.Remove(channel);
    }
}
