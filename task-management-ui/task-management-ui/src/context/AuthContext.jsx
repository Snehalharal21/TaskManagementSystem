import { createContext, useContext, useState, useEffect } from 'react';
import { authAPI } from '../services/api';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const stored = localStorage.getItem('user');
    const token = localStorage.getItem('token');
    if (stored && token) {
      setUser(JSON.parse(stored));
    }
    setLoading(false);
  }, []);

  const login = async (email, password) => {
    const res = await authAPI.login({ email, password });
    const data = res.data;
    localStorage.setItem('token', data.token);
    localStorage.setItem('user', JSON.stringify({
      userId: data.userId,
      email: data.email,
      fullName: data.fullName,
      role: data.role,
    }));
    setUser({
      userId: data.userId,
      email: data.email,
      fullName: data.fullName,
      role: data.role,
    });
    return data;
  };

  const register = async (formData) => {
    const res = await authAPI.register(formData);
    const data = res.data;
    localStorage.setItem('token', data.token);
    localStorage.setItem('user', JSON.stringify({
      userId: data.userId,
      email: data.email,
      fullName: data.fullName,
      role: data.role,
    }));
    setUser({
      userId: data.userId,
      email: data.email,
      fullName: data.fullName,
      role: data.role,
    });
    return data;
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, login, register, logout, loading, isAuthenticated: !!user }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => useContext(AuthContext);