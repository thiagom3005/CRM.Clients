namespace CRM.Clients.Domain.Exceptions;

/// <summary>
/// Representa violacao de regra de negocio no dominio.
/// Nao deve ser capturada silenciosamente -- indicia invariante quebrada.
/// </summary>
public class DomainException(string message) : Exception(message);
