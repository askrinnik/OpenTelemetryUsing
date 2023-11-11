﻿using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace WebApplication1;

public class MyInstruments : IDisposable
{
    internal const string MyInstrumentsSourceName = "OtelUsing.AspNetCore";
    private readonly Meter _meter;
    private int _todayTemperature;
    private string _todaySummary = string.Empty;

    public readonly ActivitySource ActivitySource;

    public Counter<int> RequestsCounter { get; }

    public MyInstruments()
    {
        _meter = new Meter(MyInstrumentsSourceName);
        RequestsCounter = _meter.CreateCounter<int>("web-requests", "requests", "The number of requests to the API");
        _meter.CreateObservableGauge("today-temperature", GetTemperature, "Celsius", "The temperature today");
        ActivitySource = new(MyInstrumentsSourceName);
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
        GC.SuppressFinalize(this);
    }
}