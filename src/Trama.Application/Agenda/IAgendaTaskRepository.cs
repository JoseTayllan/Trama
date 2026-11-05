namespace Trama.Application.Agenda;

public interface IAgendaTaskRepository
{
    Task SaveAsync(AgendaTaskSnapshot task, CancellationToken cancellationToken = default);
}

public sealed class AgendaPersistenceException : Exception
{
    public AgendaPersistenceException(string message) : base(message) { }
}
