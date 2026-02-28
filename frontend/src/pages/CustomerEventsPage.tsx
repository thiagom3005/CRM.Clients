// Histórico de eventos é para auditoria; payload exibido bruto de propósito (MVP do desafio).
import { useState, useEffect, type ReactNode } from 'react';
import { Link, useParams } from 'react-router-dom';
import PageContainer from '../components/PageContainer';
import ErrorMessage from '../components/ErrorMessage';
import { getCustomerEvents } from '../api/customersQueries';
import { HttpError } from '../api/httpClient';
import type { CustomerEvent, PagedResult } from '../api/types';

const PAGE_SIZE_OPTIONS = [10, 20, 50] as const;
const SKELETON_ROWS = 5;
// Larguras aproximadas por coluna para o skeleton.
const SKELETON_WIDTHS = ['3rem', '55%', '75%', '45%', '40%', '4rem'];

// Mapa de tipos conhecidos — mantém o original se vier algo novo no futuro.
const EVENT_LABELS: Record<string, string> = {
  CustomerCreated:       'Cliente criado',
  CustomerEmailChanged:  'E-mail alterado',
  CustomerAddressUpdated:'Endereço atualizado',
  CustomerPhoneChanged:  'Telefone alterado',
  CustomerTaxInfoUpdated:'Tributação atualizada',
};

function formatDateTime(iso: string): string {
  return new Intl.DateTimeFormat('pt-BR', {
    dateStyle: 'short',
    timeStyle: 'medium',
  }).format(new Date(iso));
}

export default function CustomerEventsPage() {
  const { id } = useParams<{ id: string }>();

  const [result, setResult] = useState<PagedResult<CustomerEvent> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState<10 | 20 | 50>(20);
  const [retryKey, setRetryKey] = useState(0);
  // Expand/collapse por eventId — sem modal, inline na tabela.
  const [expanded, setExpanded] = useState<Set<string>>(new Set());

  useEffect(() => {
    if (!id) return;

    const controller = new AbortController();
    setLoading(true);
    setError(null);

    getCustomerEvents(id, page, pageSize, controller.signal)
      .then(data => {
        setResult(data);
        setLoading(false);
      })
      .catch((err: unknown) => {
        if (controller.signal.aborted) return;
        if (err instanceof HttpError && err.status === 404) {
          setNotFound(true);
        } else {
          setError('Não foi possível carregar os eventos.');
        }
        setLoading(false);
      });

    return () => controller.abort();
  }, [id, page, pageSize, retryKey]);

  function toggleExpand(eventId: string): void {
    setExpanded(prev => {
      const next = new Set(prev);
      if (next.has(eventId)) next.delete(eventId);
      else next.add(eventId);
      return next;
    });
  }

  if (notFound) {
    return (
      <PageContainer title="Auditoria">
        <p className="detail-not-found">Cliente não encontrado.</p>
        <Link to="/customers" className="btn-secondary">← Voltar para a lista</Link>
      </PageContainer>
    );
  }

  if (error && !result) {
    return (
      <PageContainer title="Auditoria de eventos">
        <div className="detail-header">
          <Link to={`/customers/${id}`} className="btn-secondary">← Detalhe do cliente</Link>
          <Link to="/customers" className="btn-secondary">Lista de clientes</Link>
        </div>
        <ErrorMessage message={error} onRetry={() => setRetryKey(k => k + 1)} />
      </PageContainer>
    );
  }

  function renderBody(): ReactNode {
    if (loading) {
      return Array.from({ length: SKELETON_ROWS }, (_, i) => (
        <tr key={i} aria-hidden="true">
          {SKELETON_WIDTHS.map((w, j) => (
            <td key={j}><span className="skeleton-cell" style={{ width: w }} /></td>
          ))}
        </tr>
      ));
    }

    if (!result || result.items.length === 0) {
      return (
        <tr>
          <td colSpan={6} className="table-empty">
            Ainda não há eventos para este cliente.
          </td>
        </tr>
      );
    }

    return result.items.flatMap(evt => {
      const isOpen = expanded.has(evt.eventId);
      const rows: ReactNode[] = [
        <tr key={evt.eventId} className="table-row-clickable" onClick={() => toggleExpand(evt.eventId)}>
          <td className="event-version">{evt.version}</td>
          <td>{EVENT_LABELS[evt.eventType] ?? evt.eventType}</td>
          <td className="date-cell">{formatDateTime(evt.occurredAtUtc)}</td>
          <td>{evt.userId ?? '—'}</td>
          <td className="correlation-cell" title={evt.correlationId ?? undefined}>
            {evt.correlationId ? `${evt.correlationId.slice(0, 8)}…` : '—'}
          </td>
          <td>
            <button
              type="button"
              className="btn-link"
              onClick={e => { e.stopPropagation(); toggleExpand(evt.eventId); }}
              aria-expanded={isOpen}
            >
              {isOpen ? 'Ocultar' : 'Ver payload'}
            </button>
          </td>
        </tr>,
      ];

      if (isOpen) {
        rows.push(
          <tr key={`${evt.eventId}-payload`} className="payload-row">
            <td colSpan={6}>
              <pre className="payload-pre">
                {JSON.stringify(evt.data, null, 2)}
              </pre>
            </td>
          </tr>,
        );
      }

      return rows;
    });
  }

  const totalPages = result?.totalPages ?? 1;

  return (
    <PageContainer title="Auditoria de eventos">
      <div className="detail-header">
        <Link to={`/customers/${id}`} className="btn-secondary">← Detalhe do cliente</Link>
        <Link to="/customers" className="btn-secondary">Lista de clientes</Link>
      </div>

      <div className="events-controls">
        <label className="events-page-size-label">
          Eventos por página:
          <select
            className="form-input events-page-size-select"
            value={pageSize}
            onChange={e => {
              setPageSize(Number(e.target.value) as 10 | 20 | 50);
              setPage(1);
            }}
          >
            {PAGE_SIZE_OPTIONS.map(n => (
              <option key={n} value={n}>{n}</option>
            ))}
          </select>
        </label>
        {result && (
          <span className="events-total">
            {result.total} evento{result.total !== 1 ? 's' : ''} no total
          </span>
        )}
      </div>

      <table className="customers-table events-table">
        <thead>
          <tr>
            <th scope="col">Versão</th>
            <th scope="col">Tipo do evento</th>
            <th scope="col">Data / Hora</th>
            <th scope="col">Usuário</th>
            <th scope="col">Correlation ID</th>
            <th scope="col">Payload</th>
          </tr>
        </thead>
        <tbody>{renderBody()}</tbody>
      </table>

      {totalPages > 1 && (
        <div className="pagination" role="navigation" aria-label="Paginação de eventos">
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
            Página {result?.page ?? page} de {totalPages}
          </span>
          <button
            type="button"
            className="btn-page"
            disabled={page >= totalPages}
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            aria-label="Próxima página"
          >
            Próxima →
          </button>
        </div>
      )}
    </PageContainer>
  );
}
