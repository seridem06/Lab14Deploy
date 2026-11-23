var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

app.MapGet("/", () => "Lab14 .NET 8 funcionando");

app.MapGet("/api/saludo", () => new
{
    mensaje = "Hola desde el Laboratorio 14",
    version = "1.0",
    fecha = DateTime.UtcNow
});

app.MapGet("/health", () => Results.Ok("OK"));

app.Run();
