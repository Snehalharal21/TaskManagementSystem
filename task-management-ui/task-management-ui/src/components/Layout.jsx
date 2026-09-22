import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useEffect, useState } from 'react';
import { notificationsAPI } from '../services/api';

export default function Layout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [unread, setUnread] = useState(0);

  useEffect(() => {
    notificationsAPI.getAll(true)
      .then((res) => setUnread(res.data.length))
      .catch(() => {});
  }, []);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="app-layout">
      <aside className="sidebar">
        <div className="logo">
          <span>📋</span> TaskMgmt
        </div>
        <nav>
          <NavLink to="/" end>Dashboard</NavLink>
          <NavLink to="/tasks">Tasks</NavLink>
          <NavLink to="/teams">Teams</NavLink>
          <NavLink to="/notifications">
            Notifications {unread > 0 && <span className="notif-badge">{unread}</span>}
          </NavLink>
        </nav>
        <div className="sidebar-footer">
          <div className="user-info">
            <strong>{user?.fullName}</strong>
            <span className="role-badge">{user?.role}</span>
          </div>
          <button className="btn logout" onClick={handleLogout}>Logout</button>
        </div>
      </aside>
      <main className="main-content">
        <Outlet />
      </main>
    </div>
  );
}