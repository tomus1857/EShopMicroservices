using BuildingBlocks.Behaviors;

// pomocnik do budowania aplikacji webowej
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
// typeof - pozwala pobra� opis klasy - metadane o typie - zwraca obiekt
// System.Type z kt�rego moge odczytac "pe�na nazwe typu", "czy to klasa czy interferjs", " przestrzen nazw".

// Program - klasa g��wna aplikacji - u nas jej nie ma ale kompilator j� tworzy
var assembly = typeof(Program).Assembly;

// builder.Services to kolekcja rejestracji us�ug - implementuje IServiceCollection.
// Do kolekcji wrzucam wszystko co bedzie potem wstrzykiwane do konstruktor�w
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(assembly); // skanuje wskazany assembly w poszukiwaniu handler�w i request�w
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>)); // Dodaje zachowanie w potoku "przechwwytywacz" ��da� np. logowanie, walidacja itd
});
builder.Services.AddValidatorsFromAssembly(assembly); // skanuje assembly w poszukiwaniu klas implementuj�cych IValidator<T> i rejestruje je w kontenerze DI

builder.Services.AddCarter(); // dodaje obs�ug� Carter - framework do tworzenia endpoint�w w stylu funkcyjnym

builder.Services.AddMarten(options =>
{
    options.Connection(builder.Configuration.GetConnectionString("Database")!);
}).UseLightweightSessions();

var app = builder.Build(); // obiekt kt�ry ma w sobie wszystkie serwisy, middleware i konfiguracje

// Configure the HTTP request pipeline.
app.MapCarter(); // przechodzi przez wszystkie modu�y Cartera i mapuje endpointy
// ka�dey request przechodzi przez pipeline middleware 
// useExceptionHandler to specjalna ga��� wywow�ywana wtedy gy wystapi wyjatek
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
app.Run(); // uruchamia serwer Kestrel i nas�uchuje na ��dania HTTP

