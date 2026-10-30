namespace SSSKL.Contracts.Services;

public interface IEventNotifier
{
    Task NotifyEventChangedAsync();
}
