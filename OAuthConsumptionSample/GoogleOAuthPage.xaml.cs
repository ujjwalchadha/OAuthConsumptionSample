using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Security.Authentication.OAuth;
using System.Dynamic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Reflection;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OAuthConsumptionSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GoogleOAuthPage : Page
    {
        private readonly HttpListener _httpListener = new HttpListener();
        private static readonly string _redirectUri = "http://localhost:5000/";

        public GoogleOAuthPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            var outputTextBlock = $"Provide client id and client secret to a google oauth app. Set {_redirectUri} as the redirect uri";
            await RetrieveAndShowUserGoogleProfile();
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

        private async Task RetrieveAndShowUserGoogleProfile()
        {
            loginUI.Visibility = Visibility.Collapsed;
            var accessToken = TokenManager.RetrieveAccessTokenForGoogle();
            try
            {
                var profile = await GetUserGoogleProfile(accessToken);
                outputTextBlock.Text = BuildDisplayStringFromMap(profile);
            }
            catch (UnauthorizedAccessException)
            {
                outputTextBlock.Text = "No valid session found. Provide the client id and client secret to a valid github OAuth app.";
                loginUI.Visibility = Visibility.Visible;
            }
            catch (Exception e)
            {
                outputTextBlock.Text = $"{e.Message} Provide the client id and client secret to a valid Google OAuth app.";
                loginUI.Visibility = Visibility.Visible;
            }
        }

        private async Task<IDictionary<string, string>> GetUserGoogleProfile(string accessToken)
        {
            if (accessToken == null || accessToken == "")
            {
                throw new UnauthorizedAccessException("Access token is either invalid or expired");
            }

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Host", "www.googleapis.com");
            client.DefaultRequestHeaders.Add("User-Agent", "HttpClient");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await client.GetAsync("https://www.googleapis.com/oauth2/v1/userinfo?alt=json");

            if (response.StatusCode.HasFlag(System.Net.HttpStatusCode.Unauthorized))
            {
                throw new UnauthorizedAccessException("Access token is either invalid or expired");
            }

            if (response.StatusCode.HasFlag(System.Net.HttpStatusCode.OK))
            {
                var obj = await JsonSerializer.DeserializeAsync<ExpandoObject>(response.Content.ReadAsStream());
                var profileData = new Dictionary<string, string>();
                profileData.Add("id", obj.Where(x => x.Key == "id").First().Value.ToString());
                profileData.Add("name", obj.Where(x => x.Key == "name").First().Value.ToString());
                profileData.Add("locale", obj.Where(x => x.Key == "locale").First().Value.ToString());
                profileData.Add("picture", obj.Where(x => x.Key == "picture").First().Value.ToString());
                return profileData;
            }
            else
            {
                throw new HttpRequestException($"Google api responded with status {response.StatusCode}.");
            }
        }

        private async void loginWithGoogleButton_Click(object sender, RoutedEventArgs e)
        {
            StartLocalHostForOAuth();
            var clientId = clientIdTextBox.Text;
            var clientSecret = clientSecretTextBox.Text;

            AuthRequestParams requestParams = AuthRequestParams.CreateForAuthorizationCodeRequest(clientId, new Uri(_redirectUri));
            requestParams.Scope = "https://www.googleapis.com/auth/userinfo.profile";
            requestParams.ResponseType = "code";
            // Setting this automatically generates the code_challenge parameter
            requestParams.CodeChallengeMethod = CodeChallengeMethodKind.Plain;
 
            AuthRequestResult res = await AuthManager.InitiateAuthRequestAsync(new Uri("https://accounts.google.com/o/oauth2/v2/auth"), requestParams);

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
                var tokenRes = await AuthManager.RequestTokenAsync(new Uri("https://oauth2.googleapis.com/token"), tokenRequestParams, clientAuth);

                if (tokenRes.Failure is not null)
                {
                    outputTextBlock.Text = $"{tokenRes.Failure.Error} ({tokenRes.Failure.ErrorCode}): {tokenRes.Failure.ErrorDescription}";
                    return;
                }

                if (tokenRes.Response is not null)
                {
                    TokenManager.SaveAccessTokenForGoogle(tokenRes.Response.AccessToken);
                    await RetrieveAndShowUserGoogleProfile();
                }
            }

            // If there is no Failure and no Response, the response is not understood by AuthManager
            // Do manual parsing in that case
            outputTextBlock.Text = $"AuthRequest API responded with ${res.ResponseUri}.";
        }

        private void StartLocalHostForOAuth()
        {
            _httpListener.Prefixes.Add(_redirectUri);
            _httpListener.Start();

            Thread _responseThread = new Thread(ResponseThread);
            _responseThread.Start();
        }

        private void ResponseThread()
        {
            
            HttpListenerContext context = _httpListener.GetContext();
            var responseHtml = File.ReadAllText(Path.Combine(Assembly.GetExecutingAssembly().Location, "../oauth_webpage.html"));
            byte[] _responseArray = Encoding.UTF8.GetBytes(responseHtml);
            context.Response.OutputStream.Write(_responseArray, 0, _responseArray.Length);
            context.Response.KeepAlive = false;
            context.Response.Close();

            AuthManager.CompleteAuthRequest(context.Request.Url);
        }
    }
}
