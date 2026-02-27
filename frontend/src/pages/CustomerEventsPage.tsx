import { Link, useParams } from 'react-router-dom';
import PageContainer from '../components/PageContainer';

export default function CustomerEventsPage() {
  const { id } = useParams<{ id: string }>();

  return (
    <PageContainer title="Histórico de eventos">
      <p>Eventos do cliente: <code>{id}</code> — em construção.</p>
      <Link to={`/customers/${id}`}>← Voltar para detalhes</Link>
    </PageContainer>
  );
}
