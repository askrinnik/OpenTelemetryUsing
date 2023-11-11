using Azure.Monitor.OpenTelemetry.AspNetCore;
using LokiLoggingProvider.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WebApplication1;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<MyInstruments>();

if(args.Length == 0)
    throw new ArgumentException("Specify the solution number as the first argument");

Console.WriteLine($"Solution number: {args[0]}");
if (args[0] == "1")
    AddOtelV1(builder.Services);
else if (args[0] == "2")
    AddOtelV2(builder.Services);
else if (args[0] == "3")
    AddAzureMonitor(builder.Services);
else
    throw new ArgumentException("Invalid solution number as the first argument");

var app = builder.Build();

if (args[0] == "1")
    app.UseOpenTelemetryPrometheusScrapingEndpoint(); // add OpenTelemetry.Exporter.Prometheus.AspNetCore nuget package

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

static void AddOtelV1(IServiceCollection serviceCollection) =>
    serviceCollection
        .AddLogging(logBuilder =>
            logBuilder
                .AddConsole()
                .AddLoki(configure =>
                {
                    configure.Client = PushClient.Grpc; //port 9095
                    configure.Formatter = Formatter.Json;
                    configure.StaticLabels.JobName = MyInstruments.ApplicationName;
                    configure.StaticLabels.AdditionalStaticLabels.Add("SystemName", MyInstruments.GlobalSystemName);
                }))
        .AddOpenTelemetry() // add the OpenTelemetry.Extensions.Hosting nuget package
        .ConfigureResource(resourceBuilder => resourceBuilder
            .AddService(MyInstruments.ApplicationName)
            .AddAttributes(new Dictionary<string, object>
            {
                ["SystemName"] = MyInstruments.GlobalSystemName
            }))
        .WithTracing(tracerProviderBuilder => tracerProviderBuilder
                .AddSource(MyInstruments.InstrumentsSourceName)
                .AddAspNetCoreInstrumentation( // add the pre-release OpenTelemetry.Instrumentation.AspNetCore nuget package
                    options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext =>
                        {
                            var pathValue = httpContext.Request.Path.Value;
                            return pathValue is null
                                   || (pathValue != "/metrics" && !pathValue.StartsWith("/swagger") &&
                                       !pathValue.StartsWith("/_vs") && !pathValue.StartsWith("/_framework"));
                        };
                    })
                .AddOtlpExporter() // port 4317 // add the OpenTelemetry.Exporter.OpenTelemetryProtocol nuget package
        )
        .WithMetrics(meterProviderBuilder => meterProviderBuilder
            .AddMeter(MyInstruments.InstrumentsSourceName)
            .AddAspNetCoreInstrumentation() // Add OpenTelemetry.Instrumentation.AspNetCore nuget package
            .AddHttpClientInstrumentation() // Add OpenTelemetry.Instrumentation.Http nuget package
            .AddRuntimeInstrumentation() // Add OpenTelemetry.Instrumentation.Runtime nuget package
            .AddProcessInstrumentation() // Add OpenTelemetry.Instrumentation.Process nuget package
            .AddPrometheusExporter()); // add OpenTelemetry.Exporter.Prometheus.AspNetCore nuget package

static void AddOtelV2(IServiceCollection services) => services
    .AddLogging(loggingBuilder =>
    {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddConsole();
        loggingBuilder.AddOpenTelemetry(loggerOptions =>
        {
            var resBuilder = ResourceBuilder.CreateDefault()
                .AddService(MyInstruments.ApplicationName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["SystemName"] = MyInstruments.GlobalSystemName, // see .\dependencies\config\otel-collector\config.yaml how to add 'SystemName' as a resource label
                });
            loggerOptions.SetResourceBuilder(resBuilder);
            loggerOptions.IncludeFormattedMessage = true;
            loggerOptions.IncludeScopes = true;
            loggerOptions.AddProcessor(new ExceptionLogProcessor());
            loggerOptions.AddOtlpExporter();
        });
    })
    .AddOpenTelemetry()
    .ConfigureResource(resourceBuilder => resourceBuilder
        .AddService(MyInstruments.ApplicationName)
        .AddAttributes(new Dictionary<string, object>
        {
            ["SystemName"] = MyInstruments.GlobalSystemName
        }))
    .WithTracing(tracerProviderBuilder =>
            tracerProviderBuilder.AddSource(MyInstruments.InstrumentsSourceName)
                .AddAspNetCoreInstrumentation( // add the pre-release OpenTelemetry.Instrumentation.AspNetCore nuget package
                    options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext =>
                        {
                            var pathValue = httpContext.Request.Path.Value;
                            return pathValue is null
                                   || (pathValue != "/metrics" && !pathValue.StartsWith("/swagger") &&
                                       !pathValue.StartsWith("/_vs") && !pathValue.StartsWith("/_framework"));
                        };
                    })
                .AddHttpClientInstrumentation()
                .AddOtlpExporter() // port 4317 // add the OpenTelemetry.Exporter.OpenTelemetryProtocol nuget package
    )
    .WithMetrics(m => m
            .AddMeter(MyInstruments.InstrumentsSourceName)
            .AddAspNetCoreInstrumentation() // Add OpenTelemetry.Instrumentation.AspNetCore nuget package
            .AddHttpClientInstrumentation() // Add OpenTelemetry.Instrumentation.Http nuget package
            .AddRuntimeInstrumentation() // Add OpenTelemetry.Instrumentation.Runtime nuget package
            .AddProcessInstrumentation() // Add OpenTelemetry.Instrumentation.Process nuget package
            .AddOtlpExporter() // port 4317 // add the OpenTelemetry.Exporter.OpenTelemetryProtocol nuget package
    );

static void AddAzureMonitor(IServiceCollection services) => services
    .AddOpenTelemetry()
    .UseAzureMonitor() // Add Azure.Monitor.OpenTelemetry.AspNetCore nuget package
    .ConfigureResource(resourceBuilder => resourceBuilder
        .AddService(MyInstruments.ApplicationName)
        .AddAttributes(new Dictionary<string, object>
        {
            ["SystemName"] = MyInstruments.GlobalSystemName
        }))
    .WithTracing(tracerProviderBuilder =>
            tracerProviderBuilder.AddSource(MyInstruments.InstrumentsSourceName)
                .AddAspNetCoreInstrumentation( // add the pre-release OpenTelemetry.Instrumentation.AspNetCore nuget package
                    options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext =>
                        {
                            var pathValue = httpContext.Request.Path.Value;
                            return pathValue is null
                                   || (pathValue != "/metrics" && !pathValue.StartsWith("/swagger") &&
                                       !pathValue.StartsWith("/_vs") && !pathValue.StartsWith("/_framework"));
                        };
                    })
                .AddHttpClientInstrumentation()
    )
    .WithMetrics(m => m
            .AddMeter(MyInstruments.InstrumentsSourceName)
            .AddAspNetCoreInstrumentation() // Add OpenTelemetry.Instrumentation.AspNetCore nuget package
            .AddHttpClientInstrumentation() // Add OpenTelemetry.Instrumentation.Http nuget package
            .AddRuntimeInstrumentation() // Add OpenTelemetry.Instrumentation.Runtime nuget package
            .AddProcessInstrumentation() // Add OpenTelemetry.Instrumentation.Process nuget package
    );
