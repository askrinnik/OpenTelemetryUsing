using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace WebApplication1;

public class MyInstruments : IDisposable
{
    public const string GlobalSystemName = "MySystem";
    public const string ApplicationName = "OtelUsing.AspNetCore";
    public const string InstrumentsSourceName = "WeaterInstruments";
    private readonly Meter _meter;
    private int _todayTemperature;
    private string _todaySummary = string.Empty;

    public readonly ActivitySource ActivitySource;

    public Counter<int> RequestsCounter { get; }

    public MyInstruments()
    {
        ActivitySource = new(InstrumentsSourceName);
        _meter = new(InstrumentsSourceName);
        RequestsCounter = _meter.CreateCounter<int>("web-requests", "requests", "The number of requests to the API");
        _meter.CreateObservableGauge("today-temperature", GetTemperature, "Celsius", "The temperature today");
    }

    public void SetTodayTemperature(int temperature, string summary)
    {
        _todayTemperature = temperature;
        _todaySummary = summary;
    }

    private Measurement<int> GetTemperature() => 
        new(_todayTemperature, new KeyValuePair<string, object?>("summary", _todaySummary));

    public void Dispose()
    {
        _meter.Dispose();
        ActivitySource.Dispose();
        GC.SuppressFinalize(this);
    }
}