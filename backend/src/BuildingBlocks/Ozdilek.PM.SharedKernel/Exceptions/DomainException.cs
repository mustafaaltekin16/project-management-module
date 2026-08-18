namespace Ozdilek.PM.SharedKernel.Exceptions;

/// <summary>Thrown for expected business-rule violations (e.g. invalid state transition). Maps to HTTP 400.</summary>
public class DomainException(string message) : Exception(message);

/// <summary>Thrown when a requested entity does not exist. Maps to HTTP 404.</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>Thrown when login credentials are missing or invalid. Maps to HTTP 401.</summary>
public class AuthenticationFailedException(string message) : Exception(message);
