// Detalhe é leitura do read model; alterações ficam para comandos específicos.
import { useState, useEffect } from 'react';
import { Link, useParams } from 'react-router-dom';
import PageContainer from '../components/PageContainer';
import ErrorMessage from '../components/ErrorMessage';
import { getCustomerById } from '../api/customersQueries';
import { HttpError } from '../api/httpClient';
import type { CustomerDetails } from '../api/types';

function formatDocument(doc: string, type: string): string {
  const d = doc.replace(/\D/g, '');
  if (type === 'Individual' && d.length === 11)
    return d.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, '$1.$2.$3-$4');
  if (type === 'Company' && d.length === 14)
    return d.replace(/(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})/, '$1.$2.$3/$4-$5');
  return doc;
}

function formatPhone(phone: string): string {
  const d = phone.replace(/\D/g, '');
  if (d.length === 11) return d.replace(/(\d{2})(\d{5})(\d{4})/, '($1) $2-$3');
  if (d.length === 10) return d.replace(/(\d{2})(\d{4})(\d{4})/, '($1) $2-$3');
  return phone;
}

// DateOnly chega como "YYYY-MM-DD" — adiciona horário local para evitar rollback por fuso.
function formatLocalDate(iso: string): string {
  const d = iso.includes('T') ? new Date(iso) : new Date(`${iso}T12:00:00`);
  return d.toLocaleDateString('pt-BR');
}

const SKELETON_ROWS = 5;

export default function CustomerDetailsPage() {
  const { id } = useParams<{ id: string }>();

  const [customer, setCustomer] = useState<CustomerDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [retryKey, setRetryKey] = useState(0);

  useEffect(() => {
    if (!id) return;

    const controller = new AbortController();
    setLoading(true);
    setError(null);
    setNotFound(false);

    getCustomerById(id, controller.signal)
      .then(data => {
        setCustomer(data);
        setLoading(false);
      })
      .catch((err: unknown) => {
        if (controller.signal.aborted) return;
        if (err instanceof HttpError && err.status === 404) {
          setNotFound(true);
        } else {
          setError('Não foi possível carregar os dados do cliente.');
        }
        setLoading(false);
      });

    return () => controller.abort();
  }, [id, retryKey]);

  if (loading) {
    return (
      <PageContainer title="Cliente">
        <div className="detail-skeleton">
          {Array.from({ length: SKELETON_ROWS }, (_, i) => (
            <span key={i} className="skeleton-cell detail-skeleton-line" />
          ))}
        </div>
      </PageContainer>
    );
  }

  if (notFound) {
    return (
      <PageContainer title="Cliente">
        <p className="detail-not-found">Cliente não encontrado.</p>
        <Link to="/customers" className="btn-secondary">← Voltar para a lista</Link>
      </PageContainer>
    );
  }

  if (error || !customer) {
    return (
      <PageContainer title="Cliente">
        <ErrorMessage
          message={error ?? 'Erro inesperado.'}
          onRetry={() => setRetryKey(k => k + 1)}
        />
        <Link to="/customers" className="btn-link detail-back-link">
          ← Voltar para a lista
        </Link>
      </PageContainer>
    );
  }

  const isCompany = customer.type === 'Company';

  return (
    <PageContainer title={customer.name}>
      <div className="detail-header">
        <Link to="/customers" className="btn-secondary">← Lista de clientes</Link>
        <Link to={`/customers/${id}/events`} className="btn-secondary">Auditoria de eventos →</Link>
      </div>

      <section className="detail-section">
        <h2 className="detail-section-title">Resumo</h2>
        <dl className="detail-grid">
          <dt>Tipo</dt>
          <dd>{isCompany ? 'Pessoa Jurídica' : 'Pessoa Física'}</dd>

          <dt>{isCompany ? 'Razão social' : 'Nome'}</dt>
          <dd>{customer.name}</dd>

          <dt>{isCompany ? 'CNPJ' : 'CPF'}</dt>
          <dd className="mono">{formatDocument(customer.document, customer.type)}</dd>

          <dt>{isCompany ? 'Fundação' : 'Nascimento'}</dt>
          <dd>{formatLocalDate(customer.birthOrFoundationDate)}</dd>
        </dl>
      </section>

      <section className="detail-section">
        <h2 className="detail-section-title">Contato</h2>
        <dl className="detail-grid">
          <dt>E-mail</dt>
          <dd>{customer.email}</dd>

          <dt>Telefone</dt>
          <dd>{formatPhone(customer.phone)}</dd>
        </dl>
      </section>

      <section className="detail-section">
        <h2 className="detail-section-title">Endereço</h2>
        <dl className="detail-grid">
          <dt>CEP</dt>
          <dd className="mono">{customer.zipCode}</dd>

          <dt>Logradouro</dt>
          <dd>{customer.street}, {customer.number}</dd>

          <dt>Bairro</dt>
          <dd>{customer.district}</dd>

          <dt>Cidade / UF</dt>
          <dd>{customer.city} / {customer.state}</dd>
        </dl>
      </section>

      {isCompany && (
        <section className="detail-section">
          <h2 className="detail-section-title">Tributação</h2>
          <dl className="detail-grid">
            <dt>Isento de IE</dt>
            <dd>{customer.isStateRegistrationExempt ? 'Sim' : 'Não'}</dd>

            {!customer.isStateRegistrationExempt && (
              <>
                <dt>Inscrição Estadual</dt>
                <dd>{customer.stateRegistration ?? '—'}</dd>
              </>
            )}
          </dl>
        </section>
      )}

      <p className="detail-meta">
        Criado em {formatLocalDate(customer.createdAtUtc)}
        {' · '}
        Atualizado em {formatLocalDate(customer.updatedAtUtc)}
      </p>
    </PageContainer>
  );
}
