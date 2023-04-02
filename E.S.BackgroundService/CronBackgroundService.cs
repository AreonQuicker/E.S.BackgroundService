using System.Transactions;
using E.S.BackgroundService.Constants;
using E.S.BackgroundService.DomainModels;
using E.S.Data.Query.Context.Extensions;
using E.S.Data.Query.Context.Interfaces;
using E.S.Data.Query.DataAccess.Interfaces;
using E.S.Logging.Enums;
using E.S.Logging.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sgbj.Cron;

namespace E.S.BackgroundService;

public class CronBackgroundService<T> : Microsoft.Extensions.Hosting.BackgroundService
    where T : IBackgroundConJob
{
    private readonly string _cronJob;
    private readonly ILogger<CronBackgroundService<T>> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Random _random;

    public CronBackgroundService(IServiceProvider serviceProvider, string cronJob,
        ILogger<CronBackgroundService<T>> logger)
    {
        _serviceProvider = serviceProvider;
        _cronJob = cronJob;
        _logger = logger;
        _random = new Random();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new CronTimer(_cronJob);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            int waitTime = _random.Next(1, 21);
            
            //Wait for random time to avoid multiple instances of the same cron job running at the same time
            await Task.Delay(waitTime * 1000, stoppingToken);
            
            using (var scope = _serviceProvider.CreateScope())
            {
                var continueExecute = false;

                var dateNow = DateTime.UtcNow;

                var dataAccessQuery = scope.ServiceProvider.GetRequiredService<IDataAccessQuery>();
                var cronJobRepositoryService =
                    scope.ServiceProvider.GetRequiredService<IRepositoryService<CronJobDomainModel>>();

                using (TransactionScope transactionScope = new TransactionScope(TransactionScopeOption.Required,
                           new TransactionOptions
                               { Timeout = TimeSpan.FromSeconds(600), IsolationLevel = IsolationLevel.Serializable },
                           TransactionScopeAsyncFlowOption.Enabled)
                      )
                {
                    try
                    {
                        var cronJob = await dataAccessQuery.SelectQuery<CronJobDomainModel>()
                            .WithSelectAllFields(false)
                            .Where(nameof(CronJobDomainModel.Job), typeof(T).Name)
                            .Where(nameof(CronJobDomainModel.CreatedDate), "=", dateNow.ToString("yyyy-MM-dd"), true)
                            .OrderDesc(nameof(CronJobDomainModel.CreatedDate))
                            .FirstOrDefaultAsync<CronJobDomainModel>();

                        if ((cronJob is null || cronJob.CreatedDate.AddMinutes(20) <= dateNow))
                        {
                            await cronJobRepositoryService.CreateAsync(new CronJobDomainModel()
                            {
                                Day = dateNow.Day,
                                CreatedDate = dateNow,
                                Year = dateNow.Year,
                                Month = dateNow.Month,
                                Job = typeof(T).Name
                            });

                            continueExecute = true;
                        }

                        transactionScope.Complete();
                    }
                    catch (Exception e)
                    {
                        _logger.LogErrorOperation(LoggerStatusEnum.Error, LoggerConstant.System,
                            typeof(T).Name, dateNow.ToString("yyyy-MM-dd"), null,
                            $"Failed processing cron job {typeof(T).Name}", e);
                    }
                }

                if (continueExecute)
                {
                    _logger.LogInformationOperation(LoggerStatusEnum.Start, LoggerConstant.System,
                        typeof(T).Name, dateNow.ToString("yyyy-MM-dd"), null, $"Processing cron job {typeof(T).Name}");

                    var resolver = scope.ServiceProvider.GetRequiredService<Init.BackgroundConJobResolver>();

                    var service = resolver(typeof(T));

                    try
                    {
                        await service.ExecuteAsync(stoppingToken);

                        _logger.LogInformationOperation(LoggerStatusEnum.EndWithSucces, LoggerConstant.System,
                            typeof(T).Name, dateNow.ToString("yyyy-MM-dd"), null,
                            $"Complete processing cron job {typeof(T).Name}");
                    }
                    catch (Exception e)
                    {
                        _logger.LogErrorOperation(LoggerStatusEnum.Error, LoggerConstant.System,
                            typeof(T).Name, dateNow.ToString("yyyy-MM-dd"), null,
                            $"Failed processing cron job {typeof(T).Name}", e);

                        throw;
                    }
                }
            }
        }
    }
}