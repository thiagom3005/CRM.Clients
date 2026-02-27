import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import PageContainer from '../components/PageContainer';
import { customersApi } from '../api/endpoints';
import type { SortOption } from '../api/endpoints';
import type { CustomerListItem, PagedResult } from '../api/types';
import { useDebounce } from '../lib/useDebounce';
import { HttpError } from '../api/httpClient';

const PAGE_SIZE = 20;

function formatDocument(doc: string): string {
  const d = doc.replace(/\D/g, '');
  if (d.length === 11) return d.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, '$1.$2.$3-$4');
  if (d.length === 14) return d.replace(/(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})/, '$1.$2.$3/$4-$5');
  return doc;
}

function formatDate(iso: string): string {
  return new Intl.DateTimeFormat('pt-BR', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(iso));
}

export default function CustomersListPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [sort, setSort] = useState<SortOption>('updatedAtDesc');
  const [page, setPage] = useState(1);
  const [result, setResult] = useState<PagedResult<CustomerListItem> | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const debouncedSearch = useDebounce(search, 300);

  // Reinicia a página quando o filtro ou ordenação muda.
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, sort]);

  // Busca clientes sempre que página, filtro ou ordenação muda.
  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    customersApi
      .list({
        search: debouncedSearch || undefined,
        page,
        pageSize: PAGE_SIZE,
        sort,
      })
      .then(data => {
        if (!cancelled) {
          setResult(data);
          setLoading(false);
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          const msg =
            err instanceof HttpError
              ? `Erro ${err.status}: ${err.message}`
              : 'Erro ao carregar clientes.';
          setError(msg);
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [debouncedSearch, sort, page]);

  return (
    <PageContainer title="Clientes">
      <div className="list-controls">
        <input
          className="search-input"
          type="search"
          placeholder="Buscar por nome, e-mail ou documento…"
          value={search}
          onChange={e => setSearch(e.target.value)}
        />
        <select
          className="sort-select"
          value={sort}
          onChange={e => setSort(e.target.value as SortOption)}
        >
          <option value="updatedAtDesc">Mais recentes</option>
          <option value="nameAsc">Nome A–Z</option>
        </select>
        <button className="btn-primary" onClick={() => void navigate('/customers/new')}>
          Novo cliente
        </button>
      </div>

      {error && <div className="error-message">{error}</div>}

      {loading && <p className="loading">Carregando…</p>}

      {!loading && result && (
        <>
          <table className="customers-table">
            <thead>
              <tr>
                <th>Nome</th>
                <th>Documento</th>
                <th>E-mail</th>
                <th>Cidade / UF</th>
                <th>Atualizado em</th>
              </tr>
            </thead>
            <tbody>
              {result.items.length === 0 ? (
                <tr>
                  <td colSpan={5} className="table-empty">
                    Nenhum cliente encontrado.
                  </td>
                </tr>
              ) : (
                result.items.map(c => (
                  <tr
                    key={c.id}
                    className="table-row-clickable"
                    onClick={() => void navigate(`/customers/${c.id}`)}
                  >
                    <td>{c.name}</td>
                    <td className="doc-cell">{formatDocument(c.document)}</td>
                    <td>{c.email}</td>
                    <td>{c.city && c.state ? `${c.city} / ${c.state}` : '—'}</td>
                    <td className="date-cell">{formatDate(c.updatedAtUtc)}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>

          <div className="pagination">
            <button
              className="btn-page"
              disabled={page <= 1}
              onClick={() => setPage(p => p - 1)}
            >
              ← Anterior
            </button>
            <span className="pagination-info">
              Página {result.page} de {result.totalPages} ({result.total} registros)
            </span>
            <button
              className="btn-page"
              disabled={page >= result.totalPages}
              onClick={() => setPage(p => p + 1)}
            >
              Próxima →
            </button>
          </div>
        </>
      )}
    </PageContainer>
  );
}
