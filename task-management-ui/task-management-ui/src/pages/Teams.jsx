import { useEffect, useState } from 'react';
import { teamsAPI, usersAPI } from '../services/api';
import { useAuth } from '../context/AuthContext';

export default function Teams() {
  const { user } = useAuth();
  const [teams, setTeams] = useState([]);
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [showMemberModal, setShowMemberModal] = useState(null);
  const [form, setForm] = useState({ name: '', description: '' });
  const [selectedUser, setSelectedUser] = useState('');

  const canManage = user?.role === 'Admin' || user?.role === 'Manager';

  const load = () => {
    teamsAPI.getAll()
      .then((res) => setTeams(res.data))
      .catch(console.error)
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
    if (canManage) {
      usersAPI.getAll().then((r) => setUsers(r.data)).catch(() => {});
    }
  }, []);

  const handleCreate = async (e) => {
    e.preventDefault();
    try {
      await teamsAPI.create(form);
      setShowModal(false);
      setForm({ name: '', description: '' });
      load();
    } catch (err) {
      alert(err.response?.data?.message || 'Failed');
    }
  };

  const addMember = async (e) => {
    e.preventDefault();
    if (!selectedUser) return;
    try {
      await teamsAPI.addMember(showMemberModal, selectedUser);
      setShowMemberModal(null);
      setSelectedUser('');
      load();
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to add member');
    }
  };

  if (loading) return <div className="loading">Loading teams...</div>;

  return (
    <div className="page">
      <div className="page-header">
        <h1>Teams</h1>
        {canManage && (
          <button className="btn primary" onClick={() => setShowModal(true)}>+ New Team</button>
        )}
      </div>

      <div className="cards-grid">
        {teams.length === 0 && <p className="empty">No teams yet</p>}
        {teams.map((team) => (
          <div key={team.id} className="card">
            <h3>{team.name}</h3>
            <p className="muted">{team.description || 'No description'}</p>
            <p className="meta">Created by {team.createdByName}</p>
            <div className="members">
              <strong>Members ({team.members?.length || 0})</strong>
              <ul>
                {team.members?.map((m) => (
                  <li key={m.userId}>
                    {m.fullName} <span className="badge">{m.role}</span>
                  </li>
                ))}
              </ul>
            </div>
            {canManage && (
              <button className="btn secondary btn-sm" onClick={() => setShowMemberModal(team.id)}>
                + Add Member
              </button>
            )}
          </div>
        ))}
      </div>

      {showModal && (
        <div className="modal-overlay" onClick={() => setShowModal(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h2>Create Team</h2>
            <form onSubmit={handleCreate}>
              <div className="form-group">
                <label>Name *</label>
                <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
              </div>
              <div className="form-group">
                <label>Description</label>
                <textarea value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} rows={3} />
              </div>
              <div className="modal-actions">
                <button type="button" className="btn secondary" onClick={() => setShowModal(false)}>Cancel</button>
                <button type="submit" className="btn primary">Create</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {showMemberModal && (
        <div className="modal-overlay" onClick={() => setShowMemberModal(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h2>Add Member</h2>
            <form onSubmit={addMember}>
              <div className="form-group">
                <label>Select User</label>
                <select value={selectedUser} onChange={(e) => setSelectedUser(e.target.value)} required>
                  <option value="">Choose user...</option>
                  {users.map((u) => (
                    <option key={u.id} value={u.id}>{u.firstName} {u.lastName} ({u.role})</option>
                  ))}
                </select>
              </div>
              <div className="modal-actions">
                <button type="button" className="btn secondary" onClick={() => setShowMemberModal(null)}>Cancel</button>
                <button type="submit" className="btn primary">Add</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}