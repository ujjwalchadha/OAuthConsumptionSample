using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials;

namespace OAuthConsumptionSample
{
    internal static class TokenManager
    {
        private static readonly PasswordVault _vault = new PasswordVault();

        private const string AppName = "OAuthConsumptionSample";
        private const string GithubAccessTokenName = "GithubAccessToken";
        private const string GoogleAccessTokenName = "GoogleAccessToken";

        public static void SaveAccessTokenForGoogle(string token)
        {
            // The token should ideally be encrypted before storing
            _vault.Add(new PasswordCredential(AppName, GoogleAccessTokenName, token));
        }

        public static void SaveAccessTokenForGithub(string token)
        {
            // The token should ideally be encrypted before storing
            _vault.Add(new PasswordCredential(AppName, GithubAccessTokenName, token));
        }

        public static string RetrieveAccessTokenForGithub()
        {
            try
            {
                return _vault.Retrieve(AppName, GithubAccessTokenName).Password;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string RetrieveAccessTokenForGoogle()
        {
            try
            {
                return _vault.Retrieve(AppName, GoogleAccessTokenName).Password;
            }
            catch(Exception)
            {
                return null;
            }
        }
    }
}
