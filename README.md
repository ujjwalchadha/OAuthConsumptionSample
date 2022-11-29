# OAuthConsumptionSample

Shows usage of OAuth API in Windows App SDK app. It depicts 2 different scenarios whith slightly different usages and browser callbacks.

1. The Github OAuth Flow:
    - Making the basic auth request without PKCE verification
    - Using protocol activation in redirect uri to get the auth params back to the app from the browser and completeting the request
    - Exchanging the auth response for access token
    - Saving the access token securely in Windows.Security.Credentials.PasswordVault
    - Accessing the token back and using an HTTP get request with Authorization Header to get user profile
    
2. The Google OAuth Flow:
    - Making the basic auth request with PKCE verification (using the code_challenge parameter)
    - Creating a localhost server to get the auth params back from the browser.
    - Exchanging the auth response for access token
    - Saving the access token securely in Windows.Security.Credentials.PasswordVault
    - Accessing the token back and using an HTTP get request with Authorization Header to get user profile
