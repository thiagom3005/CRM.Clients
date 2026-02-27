namespace CRM.Clients.Application.Abstractions;

/// <summary>
/// Consultas simples no read model usadas pelos handlers do write side.
/// Isola o acesso ao banco da logica de aplicacao.
/// SaveAsync comita atomicamente eventos + projection do mesmo DbContext.
/// </summary>
public interface ICustomerReadModelReader
{
    Task<bool> DocumentExistsAsync(string normalizedDocument, CancellationToken ct = default);

    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default);

    Task SaveAsync(CancellationToken ct = default);
}
