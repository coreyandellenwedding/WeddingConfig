using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using WeddingConfig.Models;

namespace WeddingConfig
{
    public class GoogleApp
    {
        private const string GuestListSheetId = "GuestListSheetId";
        private const string GuestListSheetTitle = "GuestList";
        private readonly ILogger<GoogleApp> _logger;
        public GoogleApp(ILogger<GoogleApp> logger)
        {
            _logger = logger;
        }

        [Function("VerifyCode")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            var isConfirmed = false;
            var credentialsJson = GetGoogleCredential();
            var code = await DeserializeCode(req);
            try
            {
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(credentialsJson));
                var credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);

                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Corey Wedding Config",
                });

                var spreadsheetId = Environment.GetEnvironmentVariable(GuestListSheetId);
                var range = $"{GuestListSheetTitle}!A1:A";
                var request = service.Spreadsheets.Values.Get(spreadsheetId, range);

                var response = await request.ExecuteAsync();
                var values = response.Values;
                var allCodes = new List<string>();

                if (values != null && values.Count > 0)
                {
                    foreach (var row in values)
                    {
                        _logger.LogInformation(string.Join(", ", row));
                        allCodes.Add(string.Join(", ", row));
                    }
                }
                else
                {
                    _logger.LogInformation("No data found.");
                }

                isConfirmed = allCodes.Contains(code);
            } catch (Exception ex)
            {
                return new OkObjectResult(new { ex });
            }

            return new OkObjectResult(new { isConfirmed });
        }

        private bool VerifyHash(string code, string storedHash)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));
                var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                return hashString == storedHash;
            }
        }

        private string GetGoogleCredential()
        {
            var googleCredentials = Environment.GetEnvironmentVariable("GoogleCredentials");
            if (googleCredentials == null) throw new NullReferenceException("Google creds isn't here");

            var unescapedJson = googleCredentials
                .Replace("\\n", "\n")   // Convert \n to newlines
                .Replace("\\\"", "\"")  // Convert \\" to "
                .Replace("\\r", "\r");  // Convert \r to carriage returns

            return unescapedJson;
        }

        private async Task<string> DeserializeCode(HttpRequest req)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Deserialize the JSON to a dynamic object or a defined model
            var jsonData = JsonConvert.DeserializeObject<UserRequest>(requestBody) ?? throw new Exception("No code");

            return jsonData.Code;
        }
    }
}
