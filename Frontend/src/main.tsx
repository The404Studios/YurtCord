import React from 'react';
import ReactDOM from 'react-dom/client';
import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import App from './App';
import { store } from './store/store';
import './styles/globals.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <Provider store={store}>
      <BrowserRouter>
        <App />
        <Toaster
          position="bottom-right"
          toastOptions={{
            duration: 4000,
            style: {
              background: '#2f3136',
              color: '#dcddde',
              border: '1px solid #40444b',
            },
            success: {
              iconTheme: {
                primary: '#3ba55c',
                secondary: '#fff',
              },
            },
            error: {
              iconTheme: {
                primary: '#ed4245',
                secondary: '#fff',
              },
            },
          }}
        />
      </BrowserRouter>
    </Provider>
  </React.StrictMode>
);
