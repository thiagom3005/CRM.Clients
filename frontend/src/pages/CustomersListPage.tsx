import { useState, useEffect, type ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import PageContainer from '../components/PageContainer';
import ErrorMessage from '../components/ErrorMessage';
import { customersApi } from '../api/endpoints';
import type { SortOption } from '../api/endpoints';
import type { CustomerListItem, PagedResult } from '../api/types';
import { HttpError } from '../api/httpClient';
import { useDebounce } from '../lib/useDebounce';

const PAGE_SIZE = 20;
const SKELETON_ROWS = 8;
// Larguras aproximadas por coluna — evita que o skeleton tenha tamanho uniforme.
const SKELETON_WIDTHS = ['60%', '50%', '75%', '40%', '45%'];

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
  const [retryKey, setRetryKey] = useState(0);
  const [result, setResult] = useState<PagedResult<CustomerListItem> | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const debouncedSearch = useDebounce(search, 300);

  // Volta pra página 1 quando o filtro ou ordenação muda.
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, sort]);

  // Evita sobrescrever estado com respostas antigas.
  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError(null);

    customersApi
      .list({
        search: debouncedSearch || undefined,
        page,
        pageSize: PAGE_SIZE,
        sort,
        signal: controller.signal,
      })
      .then(data => {
        setResult(data);
        setLoading(false);
      })
      .catch((err: unknown) => {
        if (controller.signal.aborted) return;
        const detail = err instanceof HttpError ? ` (${err.status})` : '';
        setError(`Não foi possível carregar os clientes${detail}. Tente novamente.`);
        setLoading(false);
      });

    return () => controller.abort();
  }, [debouncedSearch, sort, page, retryKey]);

  function clearFilters() {
    setSearch('');
    setSort('updatedAtDesc');
    setPage(1);
  }

  // true quando já existe resultado na tela e a requisição é um refetch.
  const isRefetching = loading && result !== null;
  const isInitialLoad = loading && result === null;

  function renderTableBody(): ReactNode {
    if (isInitialLoad) {
      return Array.from({ length: SKELETON_ROWS }, (_, i) => (
        <tr key={i} aria-hidden="true">
          {SKELETON_WIDTHS.map((w, j) => (
            <td key={j}>
              <span className="skeleton-cell" style={{ width: w }} />
            </td>
          ))}
        </tr>
      ));
    }

    if (!result || result.items.length === 0) {
      return (
        <tr>
          <td colSpan={5} className="table-empty">
            {debouncedSearch ? (
              <>
                Nenhum resultado para a busca informada.{' '}
                <button type="button" className="btn-link" onClick={clearFilters}>
                  Limpar filtros
                </button>
              </>
            ) : (
              'Nenhum cliente encontrado.'
            )}
          </td>
        </tr>
      );
    }

    return result.items.map(c => (
      <tr
        key={c.id}
        className="table-row-clickable"
        onClick={() => void navigate(`/customers/${c.id}`)}
        aria-label={`Ver detalhes de ${c.name}`}
      >
        <td>{c.name}</td>
        <td className="doc-cell">{formatDocument(c.document)}</td>
        <td>{c.email}</td>
        <td>{c.city && c.state ? `${c.city} / ${c.state}` : '—'}</td>
        <td className="date-cell">{formatDate(c.updatedAtUtc)}</td>
      </tr>
    ));
  }

  return (
    <PageContainer title="Clientes">
      <div className="list-controls">
        <input
          className="search-input"
          type="search"
          placeholder="Buscar por nome, e-mail ou documento…"
          value={search}
          onChange={e => setSearch(e.target.value)}
          aria-label="Buscar clientes"
        />
        <select
          className="sort-select"
          value={sort}
          onChange={e => setSort(e.target.value as SortOption)}
          aria-label="Ordenação"
        >
          <option value="updatedAtDesc">Mais recentes</option>
          <option value="nameAsc">Nome A–Z</option>
        </select>
        <button
          type="button"
          className="btn-primary"
          onClick={() => void navigate('/customers/new')}
        >
          Novo cliente
        </button>
      </div>

      {error ? (
        <ErrorMessage message={error} onRetry={() => setRetryKey(k => k + 1)} />
      ) : (
        <>
          {/* Barra fina no topo da tabela durante refetch — evita piscadas visuais. */}
          {isRefetching && (
            <div className="refetch-bar" role="status" aria-label="Atualizando lista…" />
          )}

          <table className={`customers-table${isRefetching ? ' is-refetching' : ''}`}>
            <thead>
              <tr>
                <th scope="col">Nome</th>
                <th scope="col">Documento</th>
                <th scope="col">E-mail</th>
                <th scope="col">Cidade / UF</th>
                <th scope="col">Atualizado em</th>
              </tr>
            </thead>
            <tbody>{renderTableBody()}</tbody>
          </table>

          {result && result.totalPages > 1 && (
            <div className="pagination" role="navigation" aria-label="Paginação">
              <button
                type="button"
                className="btn-page"
                disabled={page <= 1}
                onClick={() => setPage(p => Math.max(1, p - 1))}
                aria-label="Página anterior"
              >
                ← Anterior
              </button>
              <span className="pagination-info" aria-live="polite">
                Página {result.page} de {result.totalPages} ({result.total} registros)
              </span>
              <button
                type="button"
                className="btn-page"
                disabled={page >= result.totalPages}
                onClick={() => setPage(p => p + 1)}
                aria-label="Próxima página"
              >
                Próxima →
              </button>
            </div>
          )}
        </>
      )}
    </PageContainer>
  );
}
