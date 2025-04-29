using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sattleme.Services
{
    public static class FirebaseFirestoreService
    {
        private const string ProjectId = "week-6-7bb6d";
        private const string ApiKey = "AIzaSyDSeiyWAT0rvn3mD1C8bbsb-oqYOK-QQQ0";
        private const string BaseUrl = "https://firestore.googleapis.com/v1";

        public static async Task AddDocumentAsync(string collection, string documentId, Dictionary<string, object> data, string idToken)
        {
            using (var client = new HttpClient())
            {
                string url = $"{BaseUrl}/projects/{ProjectId}/databases/(default)/documents/{collection}?documentId={documentId}&key={ApiKey}";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                var firestoreData = new { fields = ConvertToFirestoreFields(data) };
                string jsonPayload = JsonConvert.SerializeObject(firestoreData);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception("Error posting document: " + error);
                }
            }
        }

        public static async Task<List<Dictionary<string, object>>> QueryDocumentsAsync(string collection, string field, string value, string idToken)
        {
            using (var client = new HttpClient())
            {
                string url = $"{BaseUrl}/projects/{ProjectId}/databases/(default)/documents:runQuery?key={ApiKey}";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                var query = new
                {
                    structuredQuery = new
                    {
                        from = new[] { new { collectionId = collection } },
                        where = new
                        {
                            fieldFilter = new
                            {
                                field = new { fieldPath = field },
                                op = "EQUAL",
                                value = new { stringValue = value }
                            }
                        }
                    }
                };

                string jsonPayload = JsonConvert.SerializeObject(query);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error querying documents: {error}");
                }

                string responseJson = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Firestore Response: {responseJson}");

                var results = new List<Dictionary<string, object>>();

                try
                {
                    var jArray = JArray.Parse(responseJson);
                    foreach (var item in jArray)
                    {
                        var document = item["document"];
                        if (document != null)
                        {
                            var fields = ConvertFromFirestoreFields(document["fields"]);

                            string fullName = document["name"]?.ToString();
                            if (!string.IsNullOrEmpty(fullName))
                            {
                                var segments = fullName.Split('/');
                                fields["documentId"] = segments.Last();
                            }

                            results.Add(fields);
                        }
                    }
                }
                catch (JsonReaderException)
                {
                    var lines = responseJson.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        try
                        {
                            dynamic result = JsonConvert.DeserializeObject(line);
                            if (result?.document != null)
                            {
                                var fields = ConvertFromFirestoreFields(result.document.fields);
                                results.Add(fields);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Skipping a line due to parse error: {ex.Message}");
                        }
                    }
                }

                return results;
            }
        }

        public static async Task<Dictionary<string, object>> GetDocumentAsync(string collection, string documentId, string idToken)
        {
            using (var client = new HttpClient())
            {
                string url = $"{BaseUrl}/projects/{ProjectId}/databases/(default)/documents/{collection}/{documentId}?key={ApiKey}";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception("Error fetching document: " + error);
                }

                string responseJson = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseJson);

                if (result.fields != null)
                {
                    return ConvertFromFirestoreFields(result.fields);
                }
                return null;
            }
        }

        public static async Task UpdateDocumentAsync(string collection, string documentId, Dictionary<string, object> data, string idToken)
        {
            using (var client = new HttpClient())
            {
                var updateMask = string.Join("&updateMask.fieldPaths=", data.Keys);
                string url = $"{BaseUrl}/projects/{ProjectId}/databases/(default)/documents/{collection}/{documentId}?key={ApiKey}";
                if (!string.IsNullOrEmpty(updateMask))
                {
                    url += "&updateMask.fieldPaths=" + updateMask;
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                var firestoreData = new { fields = ConvertToFirestoreFields(data) };
                string jsonPayload = JsonConvert.SerializeObject(firestoreData);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception("Error updating document: " + error);
                }
            }
        }

        public static async Task DeleteDocumentAsync(string collection, string documentId, string idToken)
        {
            using (var client = new HttpClient())
            {
                string url = $"{BaseUrl}/projects/{ProjectId}/databases/(default)/documents/{collection}/{documentId}?key={ApiKey}";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                var response = await client.DeleteAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception("Error deleting document: " + error);
                }
            }
        }

        public static async Task<List<Dictionary<string, object>>> QueryAllDocumentsAsync(string collection, string idToken)
        {
            using (var client = new HttpClient())
            {
                string url = $"{BaseUrl}/projects/{ProjectId}/databases/(default)/documents:runQuery?key={ApiKey}";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                var query = new
                {
                    structuredQuery = new
                    {
                        from = new[] { new { collectionId = collection } }
                    }
                };

                string jsonPayload = JsonConvert.SerializeObject(query);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error querying documents: {error}");
                }

                string responseJson = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Firestore Response: {responseJson}");

                var results = new List<Dictionary<string, object>>();
                try
                {
                    var jArray = JArray.Parse(responseJson);
                    foreach (var item in jArray)
                    {
                        var document = item["document"];
                        if (document != null)
                        {
                            var fields = ConvertFromFirestoreFields(document["fields"]);

                            string fullName = document["name"]?.ToString();
                            if (!string.IsNullOrEmpty(fullName))
                            {
                                var segments = fullName.Split('/');
                                fields["documentId"] = segments.Last();
                            }

                            results.Add(fields);
                        }
                    }
                }
                catch (JsonReaderException)
                {
                    var lines = responseJson.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        try
                        {
                            dynamic result = JsonConvert.DeserializeObject(line);
                            if (result?.document != null)
                            {
                                var fields = ConvertFromFirestoreFields(result.document.fields);
                                results.Add(fields);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Skipping a line due to parse error: {ex.Message}");
                        }
                    }
                }
                return results;
            }
        }

        private static Dictionary<string, object> ConvertToFirestoreFields(Dictionary<string, object> data)
        {
            var result = new Dictionary<string, object>();
            foreach (var kvp in data)
            {
                if (kvp.Value is IEnumerable<object> list && !(kvp.Value is string))
                {
                    var arrayValues = new List<Dictionary<string, object>>();
                    foreach (var item in list)
                    {
                        arrayValues.Add(new Dictionary<string, object> { { "stringValue", item.ToString() } });
                    }
                    result[kvp.Key] = new Dictionary<string, object>
                    {
                        { "arrayValue", new Dictionary<string, object> { { "values", arrayValues } } }
                    };
                }
                else if (kvp.Value is DateTime dt)
                {
                    // Ensure the DateTime is converted to UTC and formatted in RFC 3339 format.
                    result[kvp.Key] = new Dictionary<string, object>
                    {
                        { "timestampValue", dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                    };
                }
                else
                {
                    result[kvp.Key] = new Dictionary<string, object>
                    {
                        { "stringValue", kvp.Value?.ToString() }
                    };
                }
            }
            return result;
        }

        private static Dictionary<string, object> ConvertFromFirestoreFields(dynamic fields)
        {
            var result = new Dictionary<string, object>();
            foreach (var property in fields)
            {
                string key = property.Name;
                var valueDict = (IDictionary<string, object>)property.Value.ToObject(typeof(Dictionary<string, object>));

                if (valueDict.ContainsKey("stringValue"))
                {
                    result[key] = valueDict["stringValue"];
                }
                else if (valueDict.ContainsKey("integerValue"))
                {
                    result[key] = Convert.ToInt32(valueDict["integerValue"]);
                }
                else if (valueDict.ContainsKey("doubleValue"))
                {
                    result[key] = Convert.ToDouble(valueDict["doubleValue"]);
                }
                else if (valueDict.ContainsKey("arrayValue"))
                {
                    var arrayObj = valueDict["arrayValue"] as JObject;
                    if (arrayObj != null && arrayObj["values"] != null)
                    {
                        var list = new List<string>();
                        foreach (var item in arrayObj["values"])
                        {
                            if (item["stringValue"] != null)
                            {
                                list.Add(item["stringValue"].ToString());
                            }
                        }
                        result[key] = list;
                    }
                }
                else if (valueDict.ContainsKey("timestampValue"))
                {
                    // Parse the timestamp string into a DateTime object
                    string ts = valueDict["timestampValue"].ToString();
                    DateTime dt;
                    if (DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out dt))
                    {
                        result[key] = dt;
                    }
                    else
                    {
                        result[key] = ts;
                    }
                }
            }
            return result;
        }
    }
}
