import { useEffect } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useAppSelector, useAppDispatch } from './store/hooks';
import { checkAuth } from './store/slices/authSlice';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import HomePage from './pages/HomePage';
import LoadingSpinner from './components/common/LoadingSpinner';

function App() {
  const dispatch = useAppDispatch();
  const { isAuthenticated, loading } = useAppSelector((state) => state.auth);

  useEffect(() => {
    dispatch(checkAuth());
  }, [dispatch]);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-discord-dark">
        <LoadingSpinner size="large" />
      </div>
    );
  }

  return (
    <div className="app">
      <Routes>
        <Route
          path="/login"
          element={!isAuthenticated ? <LoginPage /> : <Navigate to="/" />}
        />
        <Route
          path="/register"
          element={!isAuthenticated ? <RegisterPage /> : <Navigate to="/" />}
        />
        <Route
          path="/*"
          element={isAuthenticated ? <HomePage /> : <Navigate to="/login" />}
        />
      </Routes>
    </div>
  );
}

export default App;
