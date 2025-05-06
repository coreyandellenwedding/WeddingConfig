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
            var credentialsJson = GetGoogleCredential();
            var code = await DeserializeCode(req);
            Guest? matchingGuest = null;
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(credentialsJson));
            var credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(SheetsService.Scope.Spreadsheets);

            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Corey Wedding Config",
            });
            try
            {
                var spreadsheetId = Environment.GetEnvironmentVariable(GuestListSheetId);
                var range = $"{GuestListSheetTitle}!A1:Z";
                var request = service.Spreadsheets.Values.Get(spreadsheetId, range);

                var response = await request.ExecuteAsync();
                var values = response.Values;

                if (values != null && values.Count > 0)
                {
                    foreach (var row in values)
                    {
                        var rowToGuest = CreateGuest(row);

                        if (rowToGuest.Code == code)
                        {
                            matchingGuest = rowToGuest;
                            break;
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("No data found.");
                }
            } catch (Exception ex)
            {
                return new OkObjectResult(new { ex });
            }

            if (matchingGuest != null)
            {
                await LogLogin(service, matchingGuest);
                return new OkObjectResult(new UserResponse
                {
                    IsConfirmed = matchingGuest != null,
                    Name = matchingGuest?.Name?.ToString(),
                    Email = matchingGuest?.Email?.ToString(),
                    Description = matchingGuest?.Description?.ToString(),
                    HasOne = matchingGuest?.HasOne,
                    HasCeremony = matchingGuest?.HasCeremony,
                    HasReception = matchingGuest?.HasReception,
                });
            }

            return new OkObjectResult(new UserResponse
            {
                IsConfirmed = false,
            });
        }

        private async Task LogLogin(SheetsService service, Guest guest)
        {
            try
            {
                var spreadsheetId = Environment.GetEnvironmentVariable(GuestListSheetId);

                var range = "Metrics!A1";

                var valueRange = new ValueRange
                {
                    Values = new List<IList<dynamic>> {
                    new List<dynamic> {
                        DateTime.Now,
                        guest.Name,
                        guest.Code,
                        "Login"
                }
            }
                };

                var appendRequest = service.Spreadsheets.Values.Append(valueRange, spreadsheetId, range);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

                await appendRequest.ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex.Message);
            }
        }

        private Guest CreateGuest(IList<object> row) => 
            new Guest
            {
                Code = row.Count > 0 ? row[0]?.ToString() : null,
                Name = row.Count > 1 ? row[1]?.ToString() : null,
                Email = row.Count > 2 ? row[2]?.ToString() : null,
                Description = row.Count > 3 ? row[3]?.ToString() ?? "Hello!" : "Hello!",
                HasOne = row.Count > 4 && row[4] != null ? row[4].ToString()?.ToLower() == "true" : false,
                HasCeremony = row.Count > 5 && row[5] != null ? row[5].ToString()?.ToLower() == "true" : false,
                HasReception = row.Count > 6 && row[6] != null ? row[6].ToString()?.ToLower() == "true" : false,
            };

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
