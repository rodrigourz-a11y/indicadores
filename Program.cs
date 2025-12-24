using Indicadores.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar el puerto desde la variable de entorno (Railway)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Servicios
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTodos", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddHttpClient<SiiScraperService>();
builder.Services.AddHttpClient<PreviredScraperService>();
builder.Services.AddScoped<IndicadorRepository>();

var app = builder.Build();

app.UseCors("PermitirTodos");
app.UseRouting();
app.MapControllers();

app.Run();