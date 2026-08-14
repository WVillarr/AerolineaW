using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;
using AerolineaW.Api.ErrorHandling;
using AerolineaW.Api.Validation;
using AerolineaW.Application;
using AerolineaW.Infrastructure;
using AerolineaW.Infrastructure.Persistence;
using Asp.Versioning;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment);

builder.Services
    .AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks().AddDbContextCheck<AirlineDbContext>();

var app = builder.Build();

app.UseExceptionHandler();

// Matches the original project's convention: Swagger only when ASPNETCORE_ENVIRONMENT is
// Development, set by the launch profiles below.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>Exposed so <c>WebApplicationFactory&lt;Program&gt;</c> can boot this app in integration tests.</summary>
public partial class Program;
