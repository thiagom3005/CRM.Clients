import { Link } from 'react-router-dom';
import PageContainer from '../components/PageContainer';

export default function CustomersListPage() {
  return (
    <PageContainer title="Clientes">
      <p>
        Lista de clientes — em construção.{' '}
        <Link to="/customers/new">Criar novo cliente</Link>
      </p>
    </PageContainer>
  );
}
