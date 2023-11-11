using OpenTelemetry;
using OpenTelemetry.Logs;

namespace WebApplication1;

internal class ExceptionLogProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord data)
    {
        if (data.Exception is null) return;

        if (data.Attributes != null && data.Attributes.Any(x => x.Key == "exception.type")) return;

        var attributes = new List<KeyValuePair<string, object?>>();
        if (data.Attributes != null)
            attributes.AddRange(data.Attributes);
        attributes.Add(new("exception.type", data.Exception.GetType().Name));
        attributes.Add(new("exception.message", data.Exception.Message));
        attributes.Add(new("exception.stacktrace", data.Exception.StackTrace));
        data.Attributes = attributes;
    }
}