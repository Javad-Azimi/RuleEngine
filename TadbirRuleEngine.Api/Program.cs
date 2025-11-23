using Microsoft.EntityFrameworkCore;
using TadbirRuleEngine.Api.Data;
using TadbirRuleEngine.Api.Services;
using TadbirRuleEngine.Api.Mapping;
using TadbirRuleEngine.Api.Rules;
using TadbirRuleEngine.Api.Schedulers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/tadbir-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Include;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<TadbirDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Core Services
builder.Services.AddScoped<ISwaggerSourceService, SwaggerSourceService>();
builder.Services.AddScoped<IOpenApiParserService, OpenApiParserService>();
builder.Services.AddScoped<IApiCatalogService, ApiCatalogService>();
builder.Services.AddScoped<IAuthTokenService, AuthTokenService>();
builder.Services.AddScoped<IRuleEngineService, RuleEngineService>();
builder.Services.AddScoped<IPolicyExecutorService, PolicyExecutorService>();
builder.Services.AddScoped<IMappingService, MappingService>();
builder.Services.AddScoped<ISchedulerService, SchedulerService>();
builder.Services.AddScoped<IExecutionLogService, ExecutionLogService>();

// HTTP Client
builder.Services.AddHttpClient();

// CORS for any origins (*)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});



var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazor");
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TadbirDbContext>();
	//do migrations here
    context.Database.Migrate();
}

app.Run();