using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace sattleme.Services
{
    public static class FirebaseStorageService
    {
        private const string Bucket = "week-6-7bb6d.firebasestorage.app";
        private const string BaseStorageUrl = "https://firebasestorage.googleapis.com/v0/b/";

        public static async Task<string> UploadFileAsync(string folder, string userId, string fileName, Stream stream, string idToken)
        {
            using (var client = new HttpClient())
            {
                string path = $"{folder}/{userId}/{fileName}";
                string url = $"{BaseStorageUrl}{Bucket}/o?uploadType=media&name={path}";

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                var content = new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                var response = await client.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error uploading file: {error}");
                }

                string json = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(json);

                string downloadUrl = $"https://firebasestorage.googleapis.com/v0/b/{Bucket}/o/{Uri.EscapeDataString(path)}?alt=media";
                return downloadUrl;
            }
        }
    }
}
