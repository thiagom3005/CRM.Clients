// Layout mínimo para não reinventar estrutura em cada tela.
import { Link, Outlet, NavLink } from 'react-router-dom';

export default function Layout() {
  return (
    <div className="app">
      <header className="app-header">
        <nav className="app-nav">
          <Link to="/customers" className="brand">
            CRM Clientes
          </Link>
          <NavLink
            to="/customers"
            className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}
          >
            Clientes
          </NavLink>
          <NavLink
            to="/customers/new"
            className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}
          >
            Novo cliente
          </NavLink>
        </nav>
      </header>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  );
}
