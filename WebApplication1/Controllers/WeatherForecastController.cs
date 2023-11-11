using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace WebApplication1.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly MyInstruments _myInstruments;
    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(MyInstruments myInstruments, ILogger<WeatherForecastController> logger)
    {
        _myInstruments = myInstruments;
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public ActionResult<IEnumerable<WeatherForecast>> Get()
    {
        _logger.LogDebug("The GetWeatherForecast endpoint is executing...");

        _myInstruments.RequestsCounter.Add(1);

        switch (Random.Shared.Next(0, 10))
        {
            case 0:
                return NotFound();
            case 1:
                throw new InvalidDataException("Test exception");
        }

        Activity.Current?.AddEvent(new("WeatherGeneration started"));

        using var activity = _myInstruments.ActivitySource.StartActivity("GenerateWeatherForecast");
        var weatherForecasts = Enumerable.Range(1, 5).Select(index =>
            {
                var temperatureC = Random.Shared.Next(-20, 40);
                return new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = temperatureC,
                    Summary = GetSummary(temperatureC)
                };
            })
            .ToArray();

        var todayForecast = weatherForecasts.First();

        _myInstruments.SetTodayTemperature(todayForecast.TemperatureC, todayForecast.Summary);
        activity?.SetTag("TemperatureC", todayForecast.TemperatureC);
        activity?.SetTag("Summary", todayForecast.Summary);
        _logger.LogInformation("Today's weather summary: {summary}", todayForecast.Summary);

        return weatherForecasts;
    }

    private static string GetSummary(int temperatureC) =>
        temperatureC switch
        {
            < 10 => "Cold",
            > 30 => "Hot",
            _ => "Normal"
        };
}