import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { tasksAPI, commentsAPI } from '../services/api';
import { useAuth } from '../context/AuthContext';

export default function TaskDetail() {
  const { id } = useParams();
  const { user } = useAuth();
  const [task, setTask] = useState(null);
  const [comments, setComments] = useState([]);
  const [newComment, setNewComment] = useState('');
  const [loading, setLoading] = useState(true);

  const load = () => {
    Promise.all([
      tasksAPI.getById(id),
      commentsAPI.getAll(id),
    ]).then(([taskRes, commentsRes]) => {
      setTask(taskRes.data);
      setComments(commentsRes.data);
    }).catch(console.error)
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, [id]);

  const addComment = async (e) => {
    e.preventDefault();
    if (!newComment.trim()) return;
    try {
      await commentsAPI.add(id, newComment);
      setNewComment('');
      load();
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to add comment');
    }
  };

  if (loading) return <div className="loading">Loading...</div>;
  if (!task) return <div className="alert error">Task not found</div>;

  return (
    <div className="page">
      <Link to="/tasks" className="back-link">← Back to Tasks</Link>

      <div className="task-detail">
        <div className="task-header">
          <h1>{task.title}</h1>
          <div className="badges">
            <span className={`badge ${task.status.toLowerCase()}`}>{task.status}</span>
            <span className={`badge priority-${task.priority.toLowerCase()}`}>{task.priority}</span>
          </div>
        </div>

        <p className="task-desc">{task.description || 'No description'}</p>

        <div className="task-meta">
          <div><strong>Assigned To:</strong> {task.assignedToName || 'Unassigned'}</div>
          <div><strong>Created By:</strong> {task.createdByName}</div>
          <div><strong>Team:</strong> {task.teamName || '—'}</div>
          <div><strong>Deadline:</strong> {task.deadline ? new Date(task.deadline).toLocaleString() : '—'}</div>
          <div><strong>Created:</strong> {new Date(task.createdAt).toLocaleString()}</div>
        </div>
      </div>

      <div className="section">
        <h2>Comments ({comments.length})</h2>

        <form onSubmit={addComment} className="comment-form">
          <textarea
            value={newComment}
            onChange={(e) => setNewComment(e.target.value)}
            placeholder="Write a comment..."
            rows={3}
          />
          <button type="submit" className="btn primary">Add Comment</button>
        </form>

        <div className="comments-list">
          {comments.length === 0 && <p className="empty">No comments yet</p>}
          {comments.map((c) => (
            <div key={c.id} className="comment">
              <div className="comment-header">
                <strong>{c.userName}</strong>
                <span>{new Date(c.createdAt).toLocaleString()}</span>
              </div>
              <p>{c.content}</p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}