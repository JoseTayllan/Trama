namespace Trama.Application.Knowledge;

public sealed class NotePersistenceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
