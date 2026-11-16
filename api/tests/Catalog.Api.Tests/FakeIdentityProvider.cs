using System.Collections.Concurrent;
using Catalog.Api.Infrastructure.Identity;

namespace Catalog.Api.Tests;

/// <summary>Records invitations and deletions; returns the same user id for the same email.</summary>
public sealed class FakeIdentityProvider : IIdentityProvider
{
    private readonly ConcurrentDictionary<string, Guid> _users = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<string> _invitations = new();
    private readonly ConcurrentQueue<Guid> _deletions = new();

    public IReadOnlyCollection<string> Invitations => _invitations.ToArray();
    public IReadOnlyCollection<Guid> Deletions => _deletions.ToArray();

    public Task<Guid> InviteUserAsync(string email, CancellationToken ct)
    {
        _invitations.Enqueue(email);
        return Task.FromResult(UserIdFor(email));
    }

    public Task DeleteUserAsync(Guid userId, CancellationToken ct)
    {
        _deletions.Enqueue(userId);
        foreach (var (email, id) in _users)
        {
            if (id == userId)
            {
                _users.TryRemove(email, out _);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>The user id the provider gives (or will give) to this email.</summary>
    public Guid UserIdFor(string email) => _users.GetOrAdd(email, _ => Guid.CreateVersion7());

    public void Reset()
    {
        _users.Clear();
        _invitations.Clear();
        _deletions.Clear();
    }
}
