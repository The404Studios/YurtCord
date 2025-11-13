import { useState, FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../store/hooks';
import { register } from '../store/slices/authSlice';

const RegisterPage = () => {
  const dispatch = useAppDispatch();
  const { loading, error } = useAppSelector((state) => state.auth);
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    await dispatch(register({ username, email, password }));
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-indigo-900 via-purple-900 to-pink-900 p-4">
      <div className="w-full max-w-md">
        <div className="bg-gray-800/90 backdrop-blur-xl rounded-lg shadow-2xl p-8 animate-slide-up">
          <div className="text-center mb-8">
            <h1 className="text-4xl font-bold text-white mb-2 animate-fade-in">
              Create an account
            </h1>
            <p className="text-gray-400 animate-fade-in-delay">
              Join the community!
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="animate-fade-in-delay-2">
              <label className="block text-sm font-semibold text-gray-300 mb-2">
                USERNAME
              </label>
              <input
                type="text"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                className="w-full px-4 py-3 bg-gray-900/50 border border-gray-700 rounded-md text-white placeholder-gray-500 focus:outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/50 transition-all"
                placeholder="Choose a username"
                required
                disabled={loading}
              />
            </div>

            <div className="animate-fade-in-delay-3">
              <label className="block text-sm font-semibold text-gray-300 mb-2">
                EMAIL
              </label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full px-4 py-3 bg-gray-900/50 border border-gray-700 rounded-md text-white placeholder-gray-500 focus:outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/50 transition-all"
                placeholder="Enter your email"
                required
                disabled={loading}
              />
            </div>

            <div className="animate-fade-in-delay-4">
              <label className="block text-sm font-semibold text-gray-300 mb-2">
                PASSWORD
              </label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full px-4 py-3 bg-gray-900/50 border border-gray-700 rounded-md text-white placeholder-gray-500 focus:outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/50 transition-all"
                placeholder="Choose a secure password"
                required
                disabled={loading}
              />
            </div>

            {error && (
              <div className="bg-red-500/20 border border-red-500 rounded-md p-3 animate-shake">
                <p className="text-red-400 text-sm">{error}</p>
              </div>
            )}

            <button
              type="submit"
              disabled={loading}
              className="w-full py-3 bg-indigo-600 hover:bg-indigo-700 text-white font-semibold rounded-md transition-all transform hover:scale-105 active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:scale-100 animate-fade-in-delay-5"
            >
              {loading ? 'Creating account...' : 'Continue'}
            </button>
          </form>

          <div className="mt-6 text-center animate-fade-in-delay-6">
            <Link
              to="/login"
              className="text-indigo-400 hover:text-indigo-300 hover:underline transition-colors text-sm"
            >
              Already have an account?
            </Link>
          </div>
        </div>

        <p className="text-center text-gray-500 text-xs mt-4 animate-fade-in-delay-7">
          By registering, you agree to our Terms of Service and Privacy Policy
        </p>
      </div>
    </div>
  );
};

export default RegisterPage;
