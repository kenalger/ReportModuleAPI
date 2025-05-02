using Dashboards_reports.Data;
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


var app = builder.Build();

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
