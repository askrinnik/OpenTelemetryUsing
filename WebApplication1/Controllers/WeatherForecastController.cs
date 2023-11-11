using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace WebApplication1.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly MyInstruments _myInstruments;

    public WeatherForecastController(MyInstruments myInstruments)
    {
        _myInstruments = myInstruments;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public ActionResult<IEnumerable<WeatherForecast>> Get()
    {
        _myInstruments.RequestsCounter.Add(1);

        switch (Random.Shared.Next(0, 10))
        {
            case 0:
                return NotFound();
            case 1:
                return StatusCode(500);
        }

        Activity.Current?.AddEvent(new("WeatherGeneration started"));

        using var activity = _myInstruments.ActivitySource.StartActivity("WeatherGeneration");
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
        activity?.SetStatus(ActivityStatusCode.Ok);
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