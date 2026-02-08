using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AspAuth.Local.Observe;

public static class OpenTelemetryStartup
{
    public static IHostApplicationBuilder ConfigureOtel(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging => {
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: builder.Environment.ApplicationName))
            .WithMetrics(metrics =>
            {
                metrics
                  .AddAspNetCoreInstrumentation()
                  .AddHttpClientInstrumentation();

                // metrics.AddRuntimeInstrumentation()
                //     .AddMeter("Microsoft.AspNet.Hosting",
                //         "Microsoft.AspNet.Server.Kestrel",
                //         "System.Net.Http"
                //     );
            })
            .WithTracing(tracing => 
               tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddNpgsql());

                // .AddConsoleExporter()

        return builder;
    }

    public static IHostApplicationBuilder AddOtelExporters(this IHostApplicationBuilder builder)
    {
        var otelEndpoint = builder.Configuration.GetValue<string>("OTEL_EXPORTER_OTLP_ENDPOINT");
        //var otelProtocol = builder.Configuration.GetValue<string>("OTEL_EXPORTER_OTLP_TRACES_PROTOCOL");
        var hasExporter = !string.IsNullOrWhiteSpace(otelEndpoint);

        if (hasExporter)
        {
            builder.Services.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryTracerProvider(traces => traces.AddOtlpExporter());
        }

        return builder;
    }
}