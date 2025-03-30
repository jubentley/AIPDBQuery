/*
 * AIPDBQuery.cs
 * 
 * Author: Justin Bentley
 * GitHub: https://github.com/jubentley/AIPDQuery
 * Version: 1.0
 * Date: 2025-03-28
 * 
 * Description:
 * This console application queries the AbuseIPDB API v2 to check an IP address for 
 * potential abuse reports. It returns the Abuse Confidence Score, ISP, Usage Type,
 * and Country Code for the specified IP.
 * 
 * The tool uses a persistent HttpClient instance, pulls the API key from a local 
 * 'abuseipdbkey.config' file, and features color-coded terminal output for readability.
 * 
 * Usage:
 * 1. Place your AbuseIPDB API v2 key into a file named 'abuseipdbkey.config' 
 *    in the same directory as the application.
 * 2. Run the app and enter an IPv4 or IPv6 address when prompted.
 * 3. Type 'q' or press Enter on a blank line to quit.
 * 
 * Dependencies:
 * - .NET 6 or later if not using the AOT build
 * - AbuseIPDB API v2 (https://www.abuseipdb.com/)
 * 
 */


using System.Net;
using System.Text.RegularExpressions;

namespace AIPDBQuery
{
    public class AbuseResult
    {
        public bool Success { get; set; }
        public AbuseResponse? abuseResponse { get; set; }
        public string? ErrorMessage { get; set; }

        public class AbuseResponse
        {
            public int? AbuseConfidenceScore { get; set; }
            public string? ISP { get; set; }
            public string? UsageType { get; set; }
            public string? CountryCode { get; set; }
        }
    }

    public static class PersistentHttpClient
    {
        private static readonly HttpClient _httpClient;

        public static HttpClient Instance => _httpClient;

        static PersistentHttpClient()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.abuseipdb.com/");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AIPDBQuery v1.0 (github.com/jubentley/AIPDQuery)");

            // May be unnecessary in modern .NET, but safe if targeting .NET Framework
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
        }

        public static void SetAbuseAPIKey(string AbuseIPDBKey)
        {
            _httpClient.DefaultRequestHeaders.Add("Key", AbuseIPDBKey);
        }

        private const string AbuseCheckBase = "api/v2/check?maxAgeInDays=1&ipAddress=";

        public static async Task<AbuseResult> QueryIPAsync(string ipAddress)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(
                    $"{AbuseCheckBase}{ipAddress}"
                );

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();

                    AbuseResult.AbuseResponse abuseResponse = new AbuseResult.AbuseResponse
                    {
                        AbuseConfidenceScore = AIPDBQuery.JsonExtractorInt(content, "abuseConfidenceScore"),
                        ISP = AIPDBQuery.JsonExtractorString(content, "isp"),
                        UsageType = AIPDBQuery.JsonExtractorString(content, "usageType"),
                        CountryCode = AIPDBQuery.JsonExtractorString(content, "countryCode")
                    };
                    return new AbuseResult
                    {
                        Success = true,
                        abuseResponse = abuseResponse
                    };
                }
                else
                {
                    return new AbuseResult
                    {
                        Success = false,
                        ErrorMessage = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new AbuseResult
                {
                    Success = false,
                    ErrorMessage = $"Exception during request: {ex.Message}"
                };
            }
        }
    }
    internal class AIPDBQuery
    {
        public static async Task Main(string[] args)
        {

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} v1.0 Justin Bentley 2025");

            string abuseIPDBKey = ReadInConfigKey();

            HttpClient httpClient = PersistentHttpClient.Instance;

            PersistentHttpClient.SetAbuseAPIKey(abuseIPDBKey);

            while (true)
            {
                Console.Write($"\nAIPDB Query: ");

                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input) || 
                    input.ToLower().StartsWith("q") || 
                    input.ToLower() == "exit"
                ){ 
                    break;                      // terminate
                }                               

                if (!IsValidIpAddress(input))
                {
                    Console.Write($"IP Invalid for AbuseIPDB\n");
                    continue;
                }

                AbuseResult result = await PersistentHttpClient.QueryIPAsync(input);

                if (result.Success && result.abuseResponse != null)
                {
                    Console.Write($"Score      ");
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.Write($":");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($" {result.abuseResponse.AbuseConfidenceScore}\n");
                    Console.Write($"ISP        ");
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.Write($":");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($" {result.abuseResponse.ISP}\n");
                    Console.Write($"Type       ");
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.Write($":");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($" {result.abuseResponse.UsageType}\n");
                    Console.Write($"Country    ");
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.Write($":");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($" {result.abuseResponse.CountryCode}\n");
                }
                else
                {
                    Console.WriteLine($"Query failed: {result.ErrorMessage}");
                }
            }
        }

        public static string ReadInConfigKey()
        {
            try
            {
                string keyFilePath = Path.Combine(AppContext.BaseDirectory, "abuseipdbkey.config");
                if (!File.Exists(keyFilePath)) { throw new Exception("File not found."); }
                string apiKey = File.ReadAllText(keyFilePath).Trim();
                if (apiKey.Contains(' ')) { throw new Exception("Whitespace in API Key"); }
                if (apiKey == "") { throw new Exception("No Value"); }

                return apiKey;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError reading abuseipdbkey.config: {ex.Message}");
                Console.WriteLine($"\nEnsure there is a file called abuseipdbkey.config in " +
                    $"the same directory as this application that contains only your " +
                    $"AbuseIPDB APIv2 Key (and its saved).");
                Console.WriteLine($"\nThis application will not work otherwise.");
                Console.WriteLine("\nPress any key to exit...");
                WaitForUserInput();
                Environment.Exit(1); // terminate the application

                return null; // keeps the compiler happy
            }
        }
        public static bool TEST_MODE = false;
        private static void WaitForUserInput()
        {
            if (!TEST_MODE) { Console.ReadKey(true); }
        }
        public static bool IsValidIpAddress(string ipString)
        {
            if (ipString.Count(c => c == '.') < 3 && ipString.Count(c => c == ':') < 2)
            {
                return false;
            }
            return IPAddress.TryParse(ipString, out _);
            // 1.2 is a valid IP (shortcut) but AIPDB wont accept it
        }
        public static string JsonExtractorString(string json, string key)
        {
            return Regex.Unescape(Regex.Match(json, $"\"{key}\":\\s*\"([^\"]*)\"").Groups[1].Value);
        }
        public static int JsonExtractorInt(string json, string key)
        {
            return int.Parse(Regex.Match(json, $@"\""{key}\"":\s*(\d+)").Groups[1].Value);
        }
    }
}
