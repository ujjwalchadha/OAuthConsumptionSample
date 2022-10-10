using Microsoft.Security.Authentication.OAuth;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OAuthConsumptionSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GithubOAuthPage : Page
    {
        private static readonly string RedirectUri = "ujchadha.oauthconsumptionsample://gitauth";

        public GithubOAuthPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            var outputTextBlock = $"Provide client id and client secret to a google oauth app. Set {RedirectUri} as the redirect uri";
            await RetrieveAndShowUserGithubProfile();
        }

        private string BuildDisplayStringFromMap(IDictionary<string, string> map)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var key in map.Keys)
            {
                sb.Append($"{key}: {map[key]}\n");
            }

            return sb.ToString();
        }

        private async Task RetrieveAndShowUserGithubProfile()
        {
            loginUI.Visibility = Visibility.Collapsed;
            var accessToken = TokenManager.RetrieveAccessTokenForGithub();
            try
            {
                var profile = await GetUserGithubProfile(accessToken);
                outputTextBlock.Text = BuildDisplayStringFromMap(profile);
            }
            catch (UnauthorizedAccessException)
            {
                outputTextBlock.Text = "No valid session found. Provide the client id and client secret to a valid github OAuth app.";
                loginUI.Visibility = Visibility.Visible;
            }
            catch (Exception e)
            {
                outputTextBlock.Text = $"{e.Message} Provide the client id and client secret to a valid github OAuth app.";
                loginUI.Visibility = Visibility.Visible;
            }
        }

        private async Task<IDictionary<string, string>> GetUserGithubProfile(string accessToken)
        {
            if (accessToken == null || accessToken == "")
            {
                throw new UnauthorizedAccessException("Access token is either invalid or expired");
            }

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Host", "api.github.com");
            client.DefaultRequestHeaders.Add("User-Agent", "HttpClient");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await client.GetAsync("https://api.github.com/user");

            if (response.StatusCode.HasFlag(System.Net.HttpStatusCode.Unauthorized))
            {
                throw new UnauthorizedAccessException("Access token is either invalid or expired");
            }

            if (response.StatusCode.HasFlag(System.Net.HttpStatusCode.OK))
            {
                var obj = await JsonSerializer.DeserializeAsync<ExpandoObject>(response.Content.ReadAsStream());
                var profileData = new Dictionary<string, string>();
                profileData.Add("name", obj.Where(x => x.Key == "name").First().Value.ToString());
                profileData.Add("company", obj.Where(x => x.Key == "company").First().Value.ToString());
                profileData.Add("url", obj.Where(x => x.Key == "url").First().Value.ToString());
                return profileData;
            }
            else
            {
                throw new HttpRequestException($"Github api responded with status {response.StatusCode}.");
            }
        }

        private async void loginWithGithubButton_Click(object sender, RoutedEventArgs e)
        {
            var clientId = clientIdTextBox.Text;
            var clientSecret = clientSecretTextBox.Text;

            AuthRequestParams requestParams = AuthRequestParams.CreateForAuthorizationCodeRequest(clientId, new Uri(RedirectUri));
            requestParams.Scope = "read:user user:email";
            AuthRequestResult res = await AuthManager.InitiateAuthRequestAsync(new Uri("https://github.com/login/oauth/authorize"), requestParams);

            if (res.Failure is not null)
            {
                outputTextBlock.Text = $"{res.Failure.Error}: {res.Failure.ErrorDescription}";
                return;
            }

            if (res.Response is not null)
            {
                outputTextBlock.Text = $"Auth Request succeeded with code ${res.Response.Code}. Starting token exchange";
                var tokenRequestParams = TokenRequestParams.CreateForAuthorizationCodeRequest(res.Response);

                // It is not ideal to expose the client secret in an app you distribute. 
                // Ideally this exchange should happen on a secure server.
                var clientAuth = ClientAuthentication.CreateForBasicAuthorization(clientId, clientSecret);
                var tokenRes = await AuthManager.RequestTokenAsync(new Uri("https://github.com/login/oauth/access_token"), tokenRequestParams, clientAuth);

                if (tokenRes.Failure is not null)
                {
                    outputTextBlock.Text = $"{tokenRes.Failure.Error} ({tokenRes.Failure.ErrorCode}): {tokenRes.Failure.ErrorDescription}";
                    return;
                }

                if (tokenRes.Response is not null)
                {
                    TokenManager.SaveAccessTokenForGithub(tokenRes.Response.AccessToken);
                    await RetrieveAndShowUserGithubProfile();
                }
            }

            // If there is no Failure and no Response, the response is not understood by AuthManager
            // Do manual parsing in that case
            outputTextBlock.Text = $"AuthRequest API responded with ${res.ResponseUri}.";

        }
    }
}
