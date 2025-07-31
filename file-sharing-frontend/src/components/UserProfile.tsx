import React, { useState, useEffect } from 'react';
import { useMsal } from '@azure/msal-react';
import { apiService, User } from '../services/apiService';
import { apiConfig } from '../authConfig';
import './UserProfile.css';

const UserProfile: React.FC = () => {
    const { instance, accounts } = useMsal();
    const [user, setUser] = useState<User | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        fetchUserData();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const fetchUserData = async () => {
        try {
            setLoading(true);
            setError(null);

            // Get access token for API calls
            const request = {
                scopes: apiConfig.scopes,
                account: accounts[0],
            };

            const response = await instance.acquireTokenSilent(request);
            apiService.setAccessToken(response.accessToken);

            // Fetch user data from your API
            const userData = await apiService.getCurrentUser();
            setUser(userData);
        } catch (error) {
            console.error('Error fetching user data:', error);
            setError('Failed to load user data. Please try refreshing the page.');
        } finally {
            setLoading(false);
        }
    };

    const handleLogout = async () => {
        try {
            // Call API logout endpoint
            await apiService.logout();
        } catch (error) {
            console.error('API logout error:', error);
        } finally {
            // Always perform client-side logout
            instance.logout();
        }
    };

    const getStorageUsagePercentage = () => {
        if (!user?.userProfile) return 0;
        return (user.userProfile.usedStorage / user.userProfile.storageQuota) * 100;
    };

    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    if (loading) {
        return (
            <div className="profile-container">
                <div className="loading-spinner">
                    <div className="spinner"></div>
                    <p>Loading your profile...</p>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="profile-container">
                <div className="error-message">
                    <h2>⚠️ Error</h2>
                    <p>{error}</p>
                    <button onClick={fetchUserData} className="retry-button">
                        Try Again
                    </button>
                </div>
            </div>
        );
    }

    if (!user) {
        return (
            <div className="profile-container">
                <div className="error-message">
                    <h2>No user data found</h2>
                    <button onClick={fetchUserData} className="retry-button">
                        Refresh
                    </button>
                </div>
            </div>
        );
    }

    return (
        <div className="profile-container">
            <div className="profile-header">
                <div className="profile-info">
                    <h1>Welcome, {user.displayName || user.username}!</h1>
                    <p>File Sharing Dashboard</p>
                </div>
                <button onClick={handleLogout} className="logout-button">
                    Sign Out
                </button>
            </div>

            <div className="profile-content">
                <div className="profile-card">
                    <h2>👤 Profile Information</h2>
                    <div className="info-grid">
                        <div className="info-item">
                            <strong>Display Name:</strong>
                            <span>{user.displayName || 'Not provided'}</span>
                        </div>
                        <div className="info-item">
                            <strong>Email:</strong>
                            <span>{user.email}</span>
                        </div>
                        <div className="info-item">
                            <strong>Username:</strong>
                            <span>{user.username}</span>
                        </div>
                        <div className="info-item">
                            <strong>Job Title:</strong>
                            <span>{user.jobTitle || 'Not provided'}</span>
                        </div>
                        <div className="info-item">
                            <strong>Department:</strong>
                            <span>{user.department || 'Not provided'}</span>
                        </div>
                        <div className="info-item">
                            <strong>Account Status:</strong>
                            <span className={user.isActive ? 'status-active' : 'status-inactive'}>
                                {user.isActive ? '✅ Active' : '❌ Inactive'}
                            </span>
                        </div>
                    </div>
                </div>

                <div className="profile-card">
                    <h2>💾 Storage Information</h2>
                    <div className="storage-info">
                        <div className="storage-usage">
                            <div className="usage-header">
                                <span>Storage Used</span>
                                <span>{apiService.formatStorageSize(user.userProfile.usedStorage)} / {apiService.formatStorageSize(user.userProfile.storageQuota)}</span>
                            </div>
                            <div className="progress-bar">
                                <div 
                                    className="progress-fill" 
                                    style={{ width: `${getStorageUsagePercentage()}%` }}
                                ></div>
                            </div>
                            <div className="usage-percentage">
                                {getStorageUsagePercentage().toFixed(1)}% used
                            </div>
                        </div>
                    </div>
                </div>

                <div className="profile-card">
                    <h2>📅 Account Activity</h2>
                    <div className="info-grid">
                        <div className="info-item">
                            <strong>Account Created:</strong>
                            <span>{formatDate(user.createdDate)}</span>
                        </div>
                        <div className="info-item">
                            <strong>Last Login:</strong>
                            <span>{user.lastLoginDate ? formatDate(user.lastLoginDate) : 'Never'}</span>
                        </div>
                        <div className="info-item">
                            <strong>Profile Updated:</strong>
                            <span>{formatDate(user.updatedDate)}</span>
                        </div>
                    </div>
                </div>

                <div className="profile-card">
                    <h2>🔧 Quick Actions</h2>
                    <div className="action-buttons">
                        <button className="action-button primary">
                            📁 Browse Files
                        </button>
                        <button className="action-button secondary">
                            ⬆️ Upload Files
                        </button>
                        <button className="action-button secondary">
                            🤝 Share Files
                        </button>
                        <button onClick={fetchUserData} className="action-button secondary">
                            🔄 Refresh Data
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default UserProfile;