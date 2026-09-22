import { useEffect, useState } from 'react';
import { tasksAPI } from '../services/api';
import { Link } from 'react-router-dom';

export default function Dashboard() {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    tasksAPI.dashboard()
      .then((res) => setData(res.data))
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="loading">Loading dashboard...</div>;
  if (!data) return <div className="alert error">Failed to load dashboard</div>;

  return (
    <div className="page">
      <h1>Dashboard</h1>

      <div className="stats-grid">
        <div className="stat-card">
          <span className="stat-number">{data.totalTasks}</span>
          <span className="stat-label">Total Tasks</span>
        </div>
        <div className="stat-card todo">
          <span className="stat-number">{data.toDoCount}</span>
          <span className="stat-label">To Do</span>
        </div>
        <div className="stat-card progress">
          <span className="stat-number">{data.inProgressCount}</span>
          <span className="stat-label">In Progress</span>
        </div>
        <div className="stat-card done">
          <span className="stat-number">{data.doneCount}</span>
          <span className="stat-label">Done</span>
        </div>
        <div className="stat-card overdue">
          <span className="stat-number">{data.overdueCount}</span>
          <span className="stat-label">Overdue</span>
        </div>
      </div>

      <div className="section">
        <div className="section-header">
          <h2>Recent Tasks</h2>
          <Link to="/tasks" className="btn secondary">View All</Link>
        </div>

        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Title</th>
                <th>Status</th>
                <th>Priority</th>
                <th>Assigned To</th>
                <th>Deadline</th>
              </tr>
            </thead>
            <tbody>
              {data.recentTasks?.length === 0 && (
                <tr><td colSpan="5" className="empty">No tasks yet</td></tr>
              )}
              {data.recentTasks?.map((t) => (
                <tr key={t.id}>
                  <td><Link to={`/tasks/${t.id}`}>{t.title}</Link></td>
                  <td><span className={`badge ${t.status.toLowerCase()}`}>{t.status}</span></td>
                  <td><span className={`badge priority-${t.priority.toLowerCase()}`}>{t.priority}</span></td>
                  <td>{t.assignedToName || '—'}</td>
                  <td>{t.deadline ? new Date(t.deadline).toLocaleDateString() : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}