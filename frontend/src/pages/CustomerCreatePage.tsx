import { Link } from 'react-router-dom';
import PageContainer from '../components/PageContainer';

export default function CustomerCreatePage() {
  return (
    <PageContainer title="Novo cliente">
      <p>Formulário de criação — em construção.</p>
      <Link to="/customers">← Voltar</Link>
    </PageContainer>
  );
}
