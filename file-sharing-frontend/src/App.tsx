import React from 'react';
import { useIsAuthenticated } from '@azure/msal-react';
import LoginPage from './components/LoginPage';
import UserProfile from './components/UserProfile';
import './App.css';

function App() {
  const isAuthenticated = useIsAuthenticated();

  return (
    <div className="App">
      {isAuthenticated ? <UserProfile /> : <LoginPage />}
    </div>
  );
}

export default App;
