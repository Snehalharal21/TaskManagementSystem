import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { tasksAPI, usersAPI, teamsAPI } from '../services/api';
import { useAuth } from '../context/AuthContext';

const STATUS = { 0: 'ToDo', 1: 'InProgress', 2: 'Done' };
const PRIORITY = { 0: 'Low', 1: 'Medium', 2: 'High', 3: 'Critical' };

export default function Tasks() {
  const { user } = useAuth();
  const [tasks, setTasks] = useState([]);
  const [users, setUsers] = useState([]);
  const [teams, setTeams] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [filters, setFilters] = useState({ status: '', priority: '' });
  const [form, setForm] = useState({
    title: '',
    description: '',
    priority: 1,
    deadline: '',
    assignedToId: '',
    teamId: '',
  });
  const [error, setError] = useState('');

  const canCreate = user?.role === 'Admin' || user?.role === 'Manager';

  const loadTasks = () => {
    const params = {};
    if (filters.status !== '') params.status = Number(filters.status);
    if (filters.priority !== '') params.priority = Number(filters.priority);

    tasksAPI.getAll(params)
      .then((res) => setTasks(res.data))
      .catch(console.error)
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadTasks();
    if (canCreate) {
      usersAPI.getAll().then((r) => setUsers(r.data)).catch(() => {});
      teamsAPI.getAll().then((r) => setTeams(r.data)).catch(() => {});
    }
  }, [filters]);

  const handleCreate = async (e) => {
    e.preventDefault();
    setError('');
    try {
      const payload = {
        ...form,
        priority: Number(form.priority),
        assignedToId: form.assignedToId || null,
        teamId: form.teamId || null,
        deadline: form.deadline || null,
      };
      await tasksAPI.create(payload);
      setShowModal(false);
      setForm({ title: '', description: '', priority: 1, deadline: '', assignedToId: '', teamId: '' });
      loadTasks();
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to create task');
    }
  };

  const updateStatus = async (id, status) => {
    try {
      await tasksAPI.update(id, { status: Number(status) });
      loadTasks();
    } catch (err) {
      alert(err.response?.data?.message || 'Update failed');
    }
  };

  if (loading) return <div className="loading">Loading tasks...</div>;

  return (
    <div className="page">
      <div className="page-header">
        <h1>Tasks</h1>
        {canCreate && (
          <button className="btn primary" onClick={() => setShowModal(true)}>+ New Task</button>
        )}
      </div>

      <div className="filters">
        <select value={filters.status} onChange={(e) => setFilters({ ...filters, status: e.target.value })}>
          <option value="">All Status</option>
          <option value="0">To Do</option>
          <option value="1">In Progress</option>
          <option value="2">Done</option>
        </select>
        <select value={filters.priority} onChange={(e) => setFilters({ ...filters, priority: e.target.value })}>
          <option value="">All Priority</option>
          <option value="0">Low</option>
          <option value="1">Medium</option>
          <option value="2">High</option>
          <option value="3">Critical</option>
        </select>
      </div>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Title</th>
              <th>Status</th>
              <th>Priority</th>
              <th>Assigned To</th>
              <th>Team</th>
              <th>Deadline</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {tasks.length === 0 && (
              <tr><td colSpan="7" className="empty">No tasks found</td></tr>
            )}
            {tasks.map((t) => (
              <tr key={t.id}>
                <td><Link to={`/tasks/${t.id}`}>{t.title}</Link></td>
                <td>
                  <select
                    className={`status-select ${t.status.toLowerCase()}`}
                    value={Object.keys(STATUS).find((k) => STATUS[k] === t.status) ?? 0}
                    onChange={(e) => updateStatus(t.id, e.target.value)}
                  >
                    <option value="0">ToDo</option>
                    <option value="1">InProgress</option>
                    <option value="2">Done</option>
                  </select>
                </td>
                <td><span className={`badge priority-${t.priority.toLowerCase()}`}>{t.priority}</span></td>
                <td>{t.assignedToName || '—'}</td>
                <td>{t.teamName || '—'}</td>
                <td>{t.deadline ? new Date(t.deadline).toLocaleDateString() : '—'}</td>
                <td>
                  <Link to={`/tasks/${t.id}`} className="btn-sm">View</Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showModal && (
        <div className="modal-overlay" onClick={() => setShowModal(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h2>Create Task</h2>
            {error && <div className="alert error">{error}</div>}
            <form onSubmit={handleCreate}>
              <div className="form-group">
                <label>Title *</label>
                <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} required />
              </div>
              <div className="form-group">
                <label>Description</label>
                <textarea value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} rows={3} />
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label>Priority</label>
                  <select value={form.priority} onChange={(e) => setForm({ ...form, priority: e.target.value })}>
                    <option value="0">Low</option>
                    <option value="1">Medium</option>
                    <option value="2">High</option>
                    <option value="3">Critical</option>
                  </select>
                </div>
                <div className="form-group">
                  <label>Deadline</label>
                  <input type="date" value={form.deadline} onChange={(e) => setForm({ ...form, deadline: e.target.value })} />
                </div>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label>Assign To</label>
                  <select value={form.assignedToId} onChange={(e) => setForm({ ...form, assignedToId: e.target.value })}>
                    <option value="">Unassigned</option>
                    {users.map((u) => (
                      <option key={u.id} value={u.id}>{u.firstName} {u.lastName}</option>
                    ))}
                  </select>
                </div>
                <div className="form-group">
                  <label>Team</label>
                  <select value={form.teamId} onChange={(e) => setForm({ ...form, teamId: e.target.value })}>
                    <option value="">No Team</option>
                    {teams.map((t) => (
                      <option key={t.id} value={t.id}>{t.name}</option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="modal-actions">
                <button type="button" className="btn secondary" onClick={() => setShowModal(false)}>Cancel</button>
                <button type="submit" className="btn primary">Create Task</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}