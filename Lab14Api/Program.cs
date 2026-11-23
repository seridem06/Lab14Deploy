using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// Render indica el puerto con la variable PORT
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ITareaService, TareaService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapGet("/health", () => Results.Ok("OK")).WithTags("Sistema");

var tareas = app.MapGroup("/api/tareas").WithTags("Tareas");

// GET /api/tareas?completada=true
tareas.MapGet("/", (ITareaService servicio, bool? completada) =>
    Results.Ok(servicio.Listar(completada)));

// GET /api/tareas/1
tareas.MapGet("/{id:int}", (int id, ITareaService servicio) =>
    servicio.Obtener(id) is { } tarea
        ? Results.Ok(tarea)
        : Results.NotFound(new { error = $"No existe la tarea {id}" }));

// POST /api/tareas
tareas.MapPost("/", (CrearTareaRequest? request, ITareaService servicio) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.Titulo))
        return Results.BadRequest(new { error = "El titulo es obligatorio" });

    if (request.Titulo.Trim().Length > 100)
        return Results.BadRequest(new { error = "El titulo no puede superar 100 caracteres" });

    var tarea = servicio.Crear(request.Titulo.Trim());
    return Results.Created($"/api/tareas/{tarea.Id}", tarea);
});

// PUT /api/tareas/1/completar
tareas.MapPut("/{id:int}/completar", (int id, ITareaService servicio) =>
    servicio.Completar(id) is { } tarea
        ? Results.Ok(tarea)
        : Results.NotFound(new { error = $"No existe la tarea {id}" }));

// DELETE /api/tareas/1
tareas.MapDelete("/{id:int}", (int id, ITareaService servicio) =>
    servicio.Eliminar(id)
        ? Results.NoContent()
        : Results.NotFound(new { error = $"No existe la tarea {id}" }));

app.Run();

// ---------- Modelos ----------
public record Tarea(int Id, string Titulo, bool Completada, DateTime CreadaUtc);

public record CrearTareaRequest(string Titulo);

// ---------- Servicio ----------
public interface ITareaService
{
    List<Tarea> Listar(bool? completada);
    Tarea? Obtener(int id);
    Tarea Crear(string titulo);
    Tarea? Completar(int id);
    bool Eliminar(int id);
}

public class TareaService : ITareaService
{
    private readonly ConcurrentDictionary<int, Tarea> _tareas = new();
    private int _ultimoId;

    public TareaService()
    {
        Crear("Publicar en Release");
        Crear("Configurar GitHub Actions");
        Crear("Desplegar en Render");
    }

    public List<Tarea> Listar(bool? completada) =>
        _tareas.Values
            .Where(t => completada is null || t.Completada == completada)
            .OrderBy(t => t.Id)
            .ToList();

    public Tarea? Obtener(int id) =>
        _tareas.TryGetValue(id, out var tarea) ? tarea : null;

    public Tarea Crear(string titulo)
    {
        var id = Interlocked.Increment(ref _ultimoId);
        var tarea = new Tarea(id, titulo, false, DateTime.UtcNow);
        _tareas[id] = tarea;
        return tarea;
    }

    public Tarea? Completar(int id)
    {
        if (!_tareas.TryGetValue(id, out var tarea)) return null;
        var actualizada = tarea with { Completada = true };
        _tareas[id] = actualizada;
        return actualizada;
    }

    public bool Eliminar(int id) => _tareas.TryRemove(id, out _);
}
