import { Configuration, PopupRequest } from '@azure/msal-browser';

// MSAL configuration
export const msalConfig: Configuration = {
    auth: {
        clientId: 'your-client-id', // Replace with your Azure AD app registration client ID
        authority: 'https://login.microsoftonline.com/your-tenant-id', // Replace with your tenant ID
        redirectUri: 'http://localhost:3000', // Must match registered redirect URI
        postLogoutRedirectUri: 'http://localhost:3000'
    },
    cache: {
        cacheLocation: 'sessionStorage', // This configures where your cache will be stored
        storeAuthStateInCookie: false, // Set this to "true" if you are having issues on IE11 or Edge
    },
};

// Add scopes here for ID token to be used at Microsoft identity platform endpoints.
export const loginRequest: PopupRequest = {
    scopes: ['User.Read', 'openid', 'profile', 'email'],
};

// Add the endpoints here for Microsoft Graph API services you'd like to use.
export const graphConfig = {
    graphMeEndpoint: 'https://graph.microsoft.com/v1.0/me'
};

// API configuration for your backend
export const apiConfig = {
    baseUrl: 'https://localhost:7196', // Replace with your API URL  
    scopes: ['api://your-api-id/access_as_user'], // Replace with your API scope
};