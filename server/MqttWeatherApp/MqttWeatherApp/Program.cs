using MqttWeatherApp;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOptions<MqttOptions>()
    .Bind(builder.Configuration.GetSection(MqttOptions.SectionName))
    .Validate(options =>
        !string.IsNullOrWhiteSpace(options.Host) &&
        options.Port is > 0 and <= 65535 &&
        !string.IsNullOrWhiteSpace(options.Username) &&
        !string.IsNullOrWhiteSpace(options.Password) &&
        options.ReconnectDelaySeconds > 0 &&
        !string.IsNullOrWhiteSpace(options.Topic),
        "Mqtt:Host, Port, Username, Password, Topic, and ReconnectDelaySeconds must be configured with valid values.")
    .ValidateOnStart();
builder.Services.AddSignalR();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                       ?? ["http://localhost:5173"])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));
builder.Services.AddHostedService<WeatherForecastNotifier>();
builder.Services.AddHostedService<WeatherForecastWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthorization();

app.MapControllers();
app.MapHub<WeatherForecastHub>("/hubs/weatherforecast");

app.Run();
