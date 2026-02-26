namespace CRM.Clients.Domain.Exceptions;

/// <summary>
/// Representa violação de regra de negócio no domínio.
/// Não deve ser capturada silenciosamente — indicia invariante quebrada.
/// </summary>
public class DomainException(string message) : Exception(message);
