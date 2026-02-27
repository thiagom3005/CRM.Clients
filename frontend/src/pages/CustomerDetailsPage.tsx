import { Link, useParams } from 'react-router-dom';
import PageContainer from '../components/PageContainer';

export default function CustomerDetailsPage() {
  const { id } = useParams<{ id: string }>();

  return (
    <PageContainer title="Detalhes do cliente">
      <p>Cliente: <code>{id}</code> — em construção.</p>
      <p>
        <Link to={`/customers/${id}/events`}>Ver histórico de eventos</Link>
        {' · '}
        <Link to="/customers">← Voltar</Link>
      </p>
    </PageContainer>
  );
}
