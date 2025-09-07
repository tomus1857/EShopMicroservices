using BuildingBlocks.Behaviors;

// pomocnik do budowania aplikacji webowej
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
// typeof - pozwala pobraæ opis klasy - metadane o typie - zwraca obiekt
// System.Type z którego moge odczytac "pe³na nazwe typu", "czy to klasa czy interferjs", " przestrzen nazw".

// Program - klasa g³ówna aplikacji - u nas jej nie ma ale kompilator j¹ tworzy
var assembly = typeof(Program).Assembly;

// builder.Services to kolekcja rejestracji us³ug - implementuje IServiceCollection.
// Do kolekcji wrzucam wszystko co bedzie potem wstrzykiwane do konstruktorów
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(assembly); // skanuje wskazany assembly w poszukiwaniu handlerów i requestów
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>)); // Dodaje zachowanie w potoku "przechwwytywacz" ¿¹dañ np. logowanie, walidacja itd
});
builder.Services.AddValidatorsFromAssembly(assembly); // skanuje assembly w poszukiwaniu klas implementuj¹cych IValidator<T> i rejestruje je w kontenerze DI

builder.Services.AddCarter(); // dodaje obs³ugê Carter - framework do tworzenia endpointów w stylu funkcyjnym

builder.Services.AddMarten(options =>
{
    options.Connection(builder.Configuration.GetConnectionString("Database")!);
}).UseLightweightSessions();

var app = builder.Build(); // obiekt który ma w sobie wszystkie serwisy, middleware i konfiguracje

// Configure the HTTP request pipeline.
app.MapCarter(); // przechodzi przez wszystkie modu³y Cartera i mapuje endpointy
// ka¿dey request przechodzi przez pipeline middleware 
// useExceptionHandler to specjalna ga³¹Ÿ wywow³ywana wtedy gy wystapi wyjatek
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        var exceptionHandlerPathFeature =
            context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        if (exceptionHandlerPathFeature?.Error is FluentValidation.ValidationException validationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
            var result = System.Text.Json.JsonSerializer.Serialize(new { Errors = errors });
            await context.Response.WriteAsync(result);
        }
        else
        {
            var result = System.Text.Json.JsonSerializer.Serialize(new { Error = "An unexpected error occurred." });
            await context.Response.WriteAsync(result);
        }
    });
});
app.Run(); // uruchamia serwer Kestrel i nas³uchuje na ¿¹dania HTTP

