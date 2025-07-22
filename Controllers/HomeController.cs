using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TranslateGPT.Models;
using Newtonsoft.Json;

namespace TranslateGPT.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    private readonly List<string> _supportedLanguages = new List<string>
    {
        "English", "Mandarin Chinese", "spanish", "Hindi", "Arabic", "Bengali", "Portuguese", "Russian", "French", "Urdu", "Indonesian", "German", "Japanese", "Korean", "Italian", "Turkish", "Vietnamese", "Polish", "Dutch", "Thai"
    };

    public HomeController(ILogger<HomeController> logger,IConfiguration configuration, HttpClient httpClient)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public IActionResult Index()
    {
        ViewBag.Languages = _supportedLanguages;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> GetGPTResponse(string text, string targetLanguage)
    {
        // GET the Gemini API key from configuration
        var apiKey = _configuration["Gemini:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            ModelState.AddModelError(string.Empty, "Gemini API key is not configured.");
            return View("Index");
        }

        // Validate input
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(targetLanguage))
        {
            ModelState.AddModelError(string.Empty, "Text and target language are required.");
            return View("Index");
        }

        // Gemini API endpoint
        var geminiEndpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";

        // Define the request payload for Gemini
        var geminiRequestBody = new
        {
            contents = new[]
            {
                new {
                    parts = new[]
                    {
                        new { text = $"Translate the following text to {targetLanguage}: {text}" }
                    }
                }
            }
        };
        string geminiRequestJson = JsonConvert.SerializeObject(geminiRequestBody);
        HttpContent geminiContent = new StringContent(geminiRequestJson, System.Text.Encoding.UTF8, "application/json");

        // Send the request to the Gemini API
        var geminiResponse = await _httpClient.PostAsync(geminiEndpoint, geminiContent);
        var geminiResponseContent = await geminiResponse.Content.ReadAsStringAsync();

        // Parse the response to get the translated text
        dynamic geminiJsonResponse = JsonConvert.DeserializeObject(geminiResponseContent);
        string translatedText = "";
        try
        {
            translatedText = geminiJsonResponse.candidates[0].content.parts[0].text;
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Failed to parse the response from Gemini.");
            ViewBag.responseContent = geminiResponseContent;
            return View("Index");
        }

        ViewBag.TranslatedText = translatedText;
        ViewBag.Languages = _supportedLanguages;
        return View("Index");
    }
    


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
