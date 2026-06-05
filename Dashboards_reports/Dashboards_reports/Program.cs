using Dashboards_reports.Data;
using Dashboards_reports.Service;
using Dashboards_reports.CollectionTracker.Data;
using Dashboards_reports.CollectionTracker.Repositories;
using Dashboards_reports.CollectionTracker.Services;
using Microsoft.EntityFrameworkCore;

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var builder = WebApplication.CreateBuilder(args);

var devCorsPolicy = "devCorsPolicy";
builder.Services.AddCors(options =>
{
  options.AddPolicy(devCorsPolicy, builder => {
    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
  });
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
{
  options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

});

builder.Services.AddDbContext<InventoryDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("InventoryConnection"));
});

builder.Services.AddScoped<MRPService>(); //added Jan 5 2026

// ── CollectionTracker services ──
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IScheduledReportRepository, ScheduledReportRepository>();
builder.Services.AddScoped<IKpiTargetRepository, KpiTargetRepository>();
builder.Services.AddHostedService<ReportSchedulerService>();

if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://localhost:5189");
}

var app = builder.Build();

// Global exception handler — returns actual error details so the frontend can display them
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalExceptionHandler");

        logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var innerMsg = ex.InnerException?.Message;
        var errorBody = new
        {
            message = ex.Message,
            inner = innerMsg,
            source = ex.Source,
            endpoint = $"{context.Request.Method} {context.Request.Path}",
            type = ex.GetType().Name
        };

        await context.Response.WriteAsJsonAsync(errorBody);
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}
app.UseCors(devCorsPolicy);

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
