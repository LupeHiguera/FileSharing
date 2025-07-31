import React from 'react';
import { useMsal } from '@azure/msal-react';
import { loginRequest } from '../authConfig';
import './LoginPage.css';

const LoginPage: React.FC = () => {
    const { instance } = useMsal();

    const handleLogin = () => {
        instance.loginPopup(loginRequest).catch(e => {
            console.error('Login failed:', e);
        });
    };

    return (
        <div className="login-container">
            <div className="login-card">
                <div className="login-header">
                    <h1>File Sharing App</h1>
                    <p>Secure file sharing with Azure AD Enterprise authentication</p>
                </div>
                
                <div className="login-content">
                    <h2>Welcome</h2>
                    <p>Please sign in with your organizational account to access your files.</p>
                    
                    <div className="features">
                        <div className="feature-item">
                            <span className="feature-icon">🔒</span>
                            <span>Enterprise Security</span>
                        </div>
                        <div className="feature-item">
                            <span className="feature-icon">☁️</span>
                            <span>Cloud Storage</span>
                        </div>
                        <div className="feature-item">
                            <span className="feature-icon">🤝</span>
                            <span>Easy Sharing</span>
                        </div>
                    </div>
                    
                    <button 
                        className="login-button" 
                        onClick={handleLogin}
                    >
                        <span className="microsoft-icon">🔑</span>
                        Sign in with Microsoft
                    </button>
                    
                    <div className="login-info">
                        <p><strong>Storage Quota:</strong> 500 MB per user</p>
                        <p><strong>Security:</strong> Azure AD Enterprise Authentication</p>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default LoginPage;