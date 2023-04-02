namespace E.S.BackgroundService;

public interface IBackgroundConJob
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}