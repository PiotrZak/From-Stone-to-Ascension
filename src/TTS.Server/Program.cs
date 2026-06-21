using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering();
    silo.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "tts-dev";
        options.ServiceId = "tts-server";
    });
});

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("TTS Orleans silo starting on localhost...");
logger.LogInformation("ClusterId=tts-dev ServiceId=tts-server");
logger.LogInformation("Connect clients via IClusterClient (port 11111 default).");

await host.RunAsync();
