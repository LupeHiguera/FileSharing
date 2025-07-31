import axios, { AxiosResponse } from 'axios';
import { apiConfig } from '../authConfig';

// User interface matching your backend User model
export interface User {
    id: string;
    username: string;
    email: string;
    displayName?: string;
    givenName?: string;
    surname?: string;
    jobTitle?: string;
    department?: string;
    azureAdObjectId?: string;
    tenantId?: string;
    isActive: boolean;
    lastLoginDate?: string;
    createdDate: string;
    updatedDate: string;
    userProfile: UserProfile;
}

export interface UserProfile {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    userId: string;
    storageQuota: number;
    usedStorage: number;
    createdDate: string;
    updatedDate: string;
}

class ApiService {
    private baseURL = apiConfig.baseUrl;
    private accessToken: string = '';

    setAccessToken(token: string) {
        this.accessToken = token;
    }

    private getAuthHeaders() {
        return {
            'Authorization': `Bearer ${this.accessToken}`,
            'Content-Type': 'application/json',
        };
    }

    async getCurrentUser(): Promise<User> {
        try {
            const response: AxiosResponse<User> = await axios.get(
                `${this.baseURL}/api/users/me`,
                { headers: this.getAuthHeaders() }
            );
            return response.data;
        } catch (error) {
            console.error('Error fetching current user:', error);
            throw error;
        }
    }

    async updateCurrentUser(user: Partial<User>): Promise<User> {
        try {
            const response: AxiosResponse<User> = await axios.put(
                `${this.baseURL}/api/users/me`,
                user,
                { headers: this.getAuthHeaders() }
            );
            return response.data;
        } catch (error) {
            console.error('Error updating current user:', error);
            throw error;
        }
    }

    async getCurrentUserProfile(): Promise<UserProfile> {
        try {
            const response: AxiosResponse<UserProfile> = await axios.get(
                `${this.baseURL}/api/users/profile`,
                { headers: this.getAuthHeaders() }
            );
            return response.data;
        } catch (error) {
            console.error('Error fetching user profile:', error);
            throw error;
        }
    }

    async logout(): Promise<void> {
        try {
            await axios.post(
                `${this.baseURL}/api/users/logout`,
                {},
                { headers: this.getAuthHeaders() }
            );
        } catch (error) {
            console.error('Error during logout:', error);
            // Don't throw error for logout - it should always succeed on frontend
        }
    }

    // Helper method to format storage size
    formatStorageSize(bytes: number): string {
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        if (bytes === 0) return '0 Bytes';
        const i = Math.floor(Math.log(bytes) / Math.log(1024));
        return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
    }
}

export const apiService = new ApiService();