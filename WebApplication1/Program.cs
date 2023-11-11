using LokiLoggingProvider.Options;
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

AddOtelV1(builder.Services);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.UseOpenTelemetryPrometheusScrapingEndpoint(); // add OpenTelemetry.Exporter.Prometheus.AspNetCore nuget package

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
                    configure.StaticLabels.JobName = MyInstruments.MyInstrumentsSourceName;
                    configure.StaticLabels.AdditionalStaticLabels.Add("SystemName", "MySystem");
                }))
        .AddOpenTelemetry() // add the OpenTelemetry.Extensions.Hosting nuget package
        .ConfigureResource(resource => resource.AddService("MySystem"))
        .WithTracing(tracerProviderBuilder =>
                tracerProviderBuilder
                    .AddSource(MyInstruments.MyInstrumentsSourceName)
                    .AddAspNetCoreInstrumentation(// add the pre-release OpenTelemetry.Instrumentation.AspNetCore nuget package
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
        .WithMetrics(b => b
            .AddMeter(MyInstruments.MyInstrumentsSourceName)
            .AddAspNetCoreInstrumentation() // Add OpenTelemetry.Instrumentation.AspNetCore nuget package
            .AddHttpClientInstrumentation() // Add OpenTelemetry.Instrumentation.Http nuget package
            .AddRuntimeInstrumentation() // Add OpenTelemetry.Instrumentation.Runtime nuget package
            .AddProcessInstrumentation() // Add OpenTelemetry.Instrumentation.Process nuget package
            .AddPrometheusExporter()); // add OpenTelemetry.Exporter.Prometheus.AspNetCore nuget package
