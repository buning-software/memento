namespace Memento.Core.Storage;

/// <summary>The database could not be read or written. Callers show the message; the inner exception has the details.</summary>
public sealed class StorageException(string message, Exception innerException) : Exception(message, innerException);
