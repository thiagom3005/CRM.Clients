import { createBrowserRouter, Navigate } from 'react-router-dom';
import Layout from '../components/Layout';
import CustomersListPage from '../pages/CustomersListPage';
import CustomerCreatePage from '../pages/CustomerCreatePage';
import CustomerDetailsPage from '../pages/CustomerDetailsPage';
import CustomerEventsPage from '../pages/CustomerEventsPage';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [
      // Redireciona raiz para /customers para não ter tela em branco.
      { index: true, element: <Navigate to="/customers" replace /> },
      { path: 'customers',              element: <CustomersListPage /> },
      { path: 'customers/new',          element: <CustomerCreatePage /> },
      { path: 'customers/:id',          element: <CustomerDetailsPage /> },
      { path: 'customers/:id/events',   element: <CustomerEventsPage /> },
    ],
  },
]);
