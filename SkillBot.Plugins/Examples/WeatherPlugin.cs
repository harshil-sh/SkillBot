using System.ComponentModel;
using Microsoft.SemanticKernel;
using SkillBot.Infrastructure.Plugins;

namespace SkillBot.Plugins.Examples;

/// <summary>
/// Example plugin for weather information (simulated).
/// In a real implementation, this would call an actual weather API.
/// </summary>
[Plugin(Name = "Weather", Description = "Get weather information for locations")]
public class WeatherPlugin
{
    private static readonly Dictionary<string, WeatherInfo> _mockData = new()
    {
        ["London"] = new WeatherInfo("London", 15, "Cloudy", 65),
        ["New York"] = new WeatherInfo("New York", 22, "Sunny", 40),
        ["Tokyo"] = new WeatherInfo("Tokyo", 18, "Rainy", 80),
        ["Sydney"] = new WeatherInfo("Sydney", 25, "Partly Cloudy", 55)
    };

    [KernelFunction("get_current_weather")]
    [Description("Get the current weather for a city")]
    public string GetCurrentWeather(
        [Description("The city name")] string city)
    {
        if (_mockData.TryGetValue(city, out var weather))
        {
            return $"Weather in {weather.City}: {weather.Temperature}°C, {weather.Condition}, " +
                   $"Humidity: {weather.Humidity}%";
        }

        return $"Weather data not available for {city}. Try: London, New York, Tokyo, or Sydney.";
    }

    [KernelFunction("get_forecast")]
    [Description("Get a 3-day weather forecast for a city")]
    public string GetForecast(
        [Description("The city name")] string city)
    {
        if (_mockData.TryGetValue(city, out var current))
        {
            return $"3-Day Forecast for {city}:\n" +
                   $"Today: {current.Temperature}°C, {current.Condition}\n" +
                   $"Tomorrow: {current.Temperature + 2}°C, Sunny\n" +
                   $"Day 3: {current.Temperature - 1}°C, Cloudy";
        }

        return $"Forecast not available for {city}. Try: London, New York, Tokyo, or Sydney.";
    }

    private record WeatherInfo(
        string City,
        int Temperature,
        string Condition,
        int Humidity);
}
