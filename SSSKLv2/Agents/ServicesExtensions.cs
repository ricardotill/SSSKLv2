namespace SSSKLv2.Agents;

public static class ServicesExtensions
{
    public static IServiceCollection AddAgentsDI(this IServiceCollection services)
    {
        return services
            .AddSingleton<IBlobStorageAgent, BlobStorageAgent>();
    }
}