using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
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
        private const string GuestListSheetId = "GuestListSheedId";
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
            //var credentialsJson = GetGoogleCredential();
            //using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(credentialsJson)))
            //{
            //    var credential = GoogleCredential.FromStream(stream)
            //        .CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);

            //    var service = new SheetsService(new BaseClientService.Initializer()
            //    {
            //        HttpClientInitializer = credential,
            //        ApplicationName = "Corey Wedding",
            //    });

            //    var spreadsheetId = Environment.GetEnvironmentVariable(GuestListSheetId);
            //    var range = $"{GuestListSheetTitle}!A1:A";
            //    var request = service.Spreadsheets.Values.Get(spreadsheetId, range);

            //    var response = await request.ExecuteAsync();
            //    var values = response.Values;

            //    if (values != null && values.Count > 0)
            //    {
            //        foreach (var row in values)
            //        {
            //            _logger.LogInformation(string.Join(", ", row));
            //        }
            //    }
            //    else
            //    {
            //        _logger.LogInformation("No data found.");
            //    }
            //}

            //string storedHash = row[0].ToString(); // The hashed value in column A

            //// Check if the hashed name matches the provided code
            //if (VerifyHash(code, storedHash))
            //{
            //    var name = row[1].ToString();
            //    var description = row[2].ToString();
            //    var imageUrl = row[3].ToString(); // Assuming images are URLs

            //    return new OkObjectResult(new
            //    {
            //        success = true,
            //        name = name,
            //        description = description,
            //        image = imageUrl
            //    });
            //}

            return new OkObjectResult(new { success = true });
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
    }
}
