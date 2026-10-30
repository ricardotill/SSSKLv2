namespace SSSKL.Domain.Exceptions;

/// <summary>The request clashes with existing data, e.g. a duplicate name. The API returns 409.</summary>
public class ConflictException(string message) : Exception(message);
