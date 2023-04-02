using E.S.Data.Query.Context.DI;
using E.S.Data.Query.DataAccess.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace E.S.BackgroundService;

public static class Init
{
    public delegate IBackgroundConJob BackgroundConJobResolver(Type type);

    public static void AddCronJob<T>(this IServiceCollection services, string cronJob)
        where T : IBackgroundConJob
    {
        services.AddDataQueryContext();

        services.AddTransient(typeof(IBackgroundConJob), typeof(T));

        services.AddTransient<BackgroundConJobResolver>(provider => type =>
        {
            return provider.GetRequiredService<IEnumerable<IBackgroundConJob>>().ToList()
                .Find(x => x.GetType().Name == type.Name);
        });

        services.AddHostedService(
            a => new CronBackgroundService<T>(a, cronJob,
                a.GetRequiredService<ILoggerFactory>().CreateLogger<CronBackgroundService<T>>()));
    }

    public static void AddCronJobs(this IApplicationBuilder app, IDataAccessQuery dataAccessQuery)
    {
        dataAccessQuery.CreateSchemaAndTableAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }
}