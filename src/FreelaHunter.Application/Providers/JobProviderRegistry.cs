namespace FreelaHunter.Application.Providers;

public sealed class JobProviderRegistry
{
    private readonly Dictionary<string, IJobProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IJobProvider provider)
    {
        _providers[provider.ProviderId] = provider;
    }

    public IJobProvider GetProvider(string providerId)
    {
        if (_providers.TryGetValue(providerId, out var provider))
            return provider;

        throw new InvalidOperationException(
            $"Provider '{providerId}' not registered. " +
            $"Available: {string.Join(", ", _providers.Keys)}");
    }

    public IReadOnlyList<IJobProvider> GetAllProviders() =>
        _providers.Values.ToList().AsReadOnly();

    public IReadOnlyList<IJobProvider> GetEnabledProviders(IEnumerable<string> enabledIds)
    {
        return enabledIds
            .Where(id => _providers.ContainsKey(id))
            .Select(id => _providers[id])
            .ToList()
            .AsReadOnly();
    }

    public bool IsRegistered(string providerId) => _providers.ContainsKey(providerId);
}
