import { useEffect, useState } from 'react';
import { notificationsAPI } from '../services/api';

export default function Notifications() {
  const [list, setList] = useState([]);
  const [loading, setLoading] = useState(true);

  const load = () => {
    notificationsAPI.getAll()
      .then((res) => setList(res.data))
      .catch(console.error)
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  const markRead = async (id) => {
    await notificationsAPI.markRead(id);
    load();
  };

  const markAll = async () => {
    await notificationsAPI.markAllRead();
    load();
  };

  if (loading) return <div className="loading">Loading...</div>;

  return (
    <div className="page">
      <div className="page-header">
        <h1>Notifications</h1>
        {list.some((n) => !n.isRead) && (
          <button className="btn secondary" onClick={markAll}>Mark all as read</button>
        )}
      </div>

      <div className="notifications-list">
        {list.length === 0 && <p className="empty">No notifications</p>}
        {list.map((n) => (
          <div key={n.id} className={`notification-item ${n.isRead ? 'read' : 'unread'}`}>
            <div>
              <strong>{n.title}</strong>
              <p>{n.message}</p>
              <span className="time">{new Date(n.createdAt).toLocaleString()}</span>
            </div>
            {!n.isRead && (
              <button className="btn-sm" onClick={() => markRead(n.id)}>Mark read</button>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}