# File Sharing Frontend - Azure AD Authentication Test

This React application demonstrates Azure AD Enterprise authentication integration with your File Sharing backend API.

## Features

- ✅ Azure AD Enterprise Authentication
- ✅ User Profile Display
- ✅ Storage Quota Visualization (500MB)
- ✅ Responsive Design
- ✅ API Integration with Backend

## Prerequisites

1. **Azure AD App Registration** (required for authentication)
2. **Backend API running** (UserManagementService)
3. **Node.js and npm** installed

## Azure AD Setup Required

### 1. Create Azure AD App Registration

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** > **App registrations**
3. Click **New registration**
4. Configure:
   - **Name**: File Sharing Frontend
   - **Supported account types**: Accounts in this organizational directory only
   - **Redirect URI**: Web - `http://localhost:3000`

### 2. Configure App Registration

1. **Authentication**:
   - Add redirect URI: `http://localhost:3000`
   - Enable **Access tokens** and **ID tokens**
   - Set logout URL: `http://localhost:3000`

2. **API Permissions**:
   - Add Microsoft Graph permissions:
     - `User.Read` (delegated)
     - `openid` (delegated)
     - `profile` (delegated)
     - `email` (delegated)
   - Add your API permissions (if exposing API scopes)

3. **Expose an API** (for backend communication):
   - Set Application ID URI: `api://your-app-id`
   - Add scope: `access_as_user`

### 3. Update Configuration

Update `src/authConfig.ts` with your Azure AD details:

```typescript
export const msalConfig: Configuration = {
    auth: {
        clientId: 'your-client-id-here',
        authority: 'https://login.microsoftonline.com/your-tenant-id-here',
        redirectUri: 'http://localhost:3000',
        postLogoutRedirectUri: 'http://localhost:3000'
    },
    // ... rest of config
};

export const apiConfig = {
    baseUrl: 'https://localhost:7196', // Your backend API URL
    scopes: ['api://your-api-id/access_as_user'],
};
```

## Installation & Setup

### 1. Install Dependencies
```bash
npm install
```

### 2. Configure Authentication
Edit `src/authConfig.ts` with your Azure AD app registration details.

### 3. Start Development Server
```bash
npm start
```

The app will open at `http://localhost:3000`

## Backend Configuration

Ensure your backend `appsettings.json` is configured:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Audience": "api://your-api-id"
  }
}
```

## How to Test

### 1. Start Backend API
```bash
cd ../UserManagementService
dotnet run
```

### 2. Start Frontend
```bash
npm start
```

### 3. Test Authentication Flow

1. **Login Page**: Click "Sign in with Microsoft"
2. **Azure AD Login**: Enter your organizational credentials
3. **User Profile**: View your profile information and storage quota
4. **API Integration**: Profile data is fetched from your backend API

## Application Flow

1. **Unauthenticated**: Shows login page
2. **Authentication**: Redirects to Azure AD login
3. **Token Acquisition**: Gets access token for API calls
4. **API Calls**: Fetches user data from backend
5. **Profile Display**: Shows user information and storage quota

## Storage Quota

- **Default Quota**: 500 MB per user
- **Display**: Progress bar showing usage percentage
- **Backend Integration**: Quota managed by UserManagementService

## Components

- **LoginPage**: Azure AD authentication interface
- **UserProfile**: Displays user information and storage
- **ApiService**: Handles backend API communication
- **AuthConfig**: Azure AD and API configuration

## Troubleshooting

### Common Issues

1. **CORS Errors**: Ensure backend CORS is configured for `http://localhost:3000`
2. **Authentication Fails**: Check Azure AD app registration redirect URIs
3. **API Calls Fail**: Verify backend is running and API scopes are correct
4. **Token Issues**: Check browser console for MSAL errors

### Debug Tips

1. Check browser console for errors
2. Verify Azure AD app registration settings
3. Ensure backend API is accessible
4. Test API endpoints directly with tools like Postman

## Next Steps

After successful testing:

1. **Production Configuration**: Update redirect URIs for production
2. **File Management**: Add file upload/download components
3. **Sharing Features**: Implement file sharing functionality
4. **Azure Deployment**: Deploy to Azure App Service

## Security Notes

- Never commit real Azure AD credentials to source control
- Use environment variables for production configuration
- Implement proper error handling for production use
- Consider implementing refresh token rotation