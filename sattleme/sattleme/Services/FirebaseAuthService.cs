using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using sattleme.auth.Models;

namespace sattleme.Services
{
    public class FirebaseAuthService
    {
        public static FirebaseAuthService Instance { get; } = new FirebaseAuthService();

        public FirebaseUser CurrentUser { get; set; }

        public string IdToken { get; set; }

        private const string ApiKey = "AIzaSyDSeiyWAT0rvn3mD1C8bbsb-oqYOK-QQQ0\r\n"; 
        private HttpClient client;

        public FirebaseAuthService()
        {
            client = new HttpClient();
        }

        public async Task<string> RegisterUserAsync(string fullName, string email, string password)
        {
            try
            {
                var signUpUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={ApiKey}";
                var payload = new
                {
                    email = email,
                    password = password,
                    returnSecureToken = true
                };

                var jsonPayload = JsonConvert.SerializeObject(payload);
                var response = await client.PostAsync(signUpUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception("Registration failed: " + error);
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseContent);

                string idToken = result.idToken;
                string localId = result.localId;
                this.IdToken = idToken; 

               
                await UpdateUserProfileAsync(idToken, fullName);

               
                CurrentUser = new FirebaseUser
                {
                    Uid = localId,
                    Email = email,
                    DisplayName = fullName,
                    PhotoUrl = ""
                };

                return localId;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in RegisterUserAsync: " + ex.Message, ex);
            }
        }

        private async Task UpdateUserProfileAsync(string idToken, string displayName)
        {
            try
            {
                var updateUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={ApiKey}";
                var payload = new
                {
                    idToken = idToken,
                    displayName = displayName,
                    returnSecureToken = true
                };
                var jsonPayload = JsonConvert.SerializeObject(payload);
                var response = await client.PostAsync(updateUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception("Profile update failed: " + error);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in UpdateUserProfileAsync: " + ex.Message, ex);
            }
        }
        public async Task<string> GetValidIdTokenAsync()
        {
            if (CurrentUser == null)
            {
                Console.WriteLine("No user is logged in.");
                return null;
            }

            try
            {

                if (!string.IsNullOrEmpty(IdToken))
                {
                    return IdToken;
                }

                var refreshUrl = $"https://securetoken.googleapis.com/v1/token?key={ApiKey}";
                var payload = new
                {
                    grant_type = "refresh_token",
                    refresh_token = IdToken 
                };

                var jsonPayload = JsonConvert.SerializeObject(payload);
                var response = await client.PostAsync(refreshUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to refresh token: {error}");
                    return null; 
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseContent);

               
                IdToken = result.id_token;
                return IdToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing ID token: {ex.Message}");
                return null;
            }
        }

        public async Task<string> LoginUserAsync(string email, string password)
        {
            try
            {
         
                var signInUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={ApiKey}";
                var payload = new
                {
                    email = email,
                    password = password,
                    returnSecureToken = true
                };
                var jsonPayload = JsonConvert.SerializeObject(payload);
                var response = await client.PostAsync(signInUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception("Login failed: " + error);
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseContent);

                string idToken = result.idToken;
                string localId = result.localId;
                this.IdToken = idToken; 

                CurrentUser = new FirebaseUser
                {
                    Uid = localId,
                    Email = email,
                    DisplayName = "", 
                    PhotoUrl = ""
                };

                return localId;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in LoginUserAsync: " + ex.Message, ex);
            }
        }
    }
}
