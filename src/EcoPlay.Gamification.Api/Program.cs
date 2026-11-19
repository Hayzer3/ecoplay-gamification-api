using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.RateLimiting;
using EcoPlay.Gamification.Api.Domain;
using EcoPlay.Gamification.Api.Health;
using EcoPlay.Gamification.Api.Persistence;
using EcoPlay.Gamification.Api.Security;
using EcoPlay.Gamification.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Gamification")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=EcoPlay;Trusted_Connection=True;TrustServerCertificate=True";

builder.Services.AddDbContext<GamificationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<IdempotencyService>();
builder.Services.AddScoped<GamificationService>();
builder.Services.AddHttpClient("java-api", client => client.Timeout = TimeSpan.FromSeconds(3));
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]).AddCheck<JavaApiHealthCheck>("java-api", tags: ["ready"]);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("quiz", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue("sub") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
});

var pem = builder.Configuration["Jwt:PublicKeyPem"];
using var fallbackRsa = RSA.Create(2048);
var rsa = RSA.Create();
try { rsa.ImportFromPem(string.IsNullOrWhiteSpace(pem) ? fallbackRsa.ExportSubjectPublicKeyInfoPem() : pem); }
catch (Exception ex) when (ex is ArgumentException or CryptographicException)
{
    throw new InvalidOperationException("Jwt:PublicKeyPem não contém uma chave pública RSA PEM válida.", ex);
}
var signingKey = new RsaSecurityKey(rsa);
builder.Services.AddSingleton(signingKey);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        ValidateIssuer = !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Issuer"]),
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Audience"]),
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = "sub",
        RoleClaimType = "papel"
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Cidadao", policy => policy.RequireAuthenticatedUser().RequireRole(RoleNames.Cidadao));
    options.AddPolicy("Sistema", policy => policy.RequireAuthenticatedUser().RequireRole(RoleNames.Sistema));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "EcoPlay Gamificação API", Version = "v1", Description = "API de XP, quiz, missões, ranking e impacto do EcoPlay." });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header, Description = "JWT RSA emitido pela API Java." });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
});

var app = builder.Build();
if (!app.Environment.IsEnvironment("Testing") && builder.Configuration.GetValue("Database:ApplyMigrations", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<GamificationDbContext>();
    await db.Database.MigrateAsync();
    await CatalogSeeder.SeedAsync(db);
}
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (HealthCheckService checks, HttpContext context) =>
{
    var report = await checks.CheckHealthAsync(_ => true, context.RequestAborted);
    context.Response.StatusCode = report.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy ? 200 : 503;
    return Results.Json(new { status = report.Status.ToString().ToLowerInvariant(), checks = report.Entries.ToDictionary(x => x.Key, x => new { status = x.Value.Status.ToString().ToLowerInvariant(), description = x.Value.Description }) });
}).AllowAnonymous().WithName("Health").WithSummary("Verifica API, Azure SQL e API Java configurada.");

var api = app.MapGroup("/api");
api.MapGet("/progresso", async (ClaimsPrincipal user, GamificationService service, CancellationToken ct) =>
{
    var id = Subject(user); var bairro = user.FindFirstValue("bairro");
    return Results.Ok(await service.ProgressoAsync(id, bairro, ct));
}).RequireAuthorization("Cidadao").WithName("GetProgresso").WithSummary("Retorna XP, nível, medalhas e bairro do cidadão.");

api.MapGet("/missoes", async (ClaimsPrincipal user, GamificationService service, CancellationToken ct) =>
    Results.Ok(await service.MissoesAsync(Subject(user), ct)))
    .RequireAuthorization("Cidadao").WithName("GetMissoes").WithSummary("Lista missões e progresso do cidadão.");

api.MapGet("/quiz/hoje", async (GamificationService service, CancellationToken ct) => Results.Ok(await service.QuizHojeAsync(ct)))
    .RequireAuthorization("Cidadao").WithName("GetQuizHoje").WithSummary("Lista as perguntas ativas do quiz diário.");

api.MapPost("/quiz/respostas", async (RespostaQuizRequest request, ClaimsPrincipal user, GamificationService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.ResponderQuizAsync(Subject(user), request, ct)); }
    catch (KeyNotFoundException ex) { return Results.NotFound(new { erro = ex.Message }); }
    catch (ArgumentException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["resposta"] = [ex.Message] }); }
}).RequireAuthorization("Cidadao").RequireRateLimiting("quiz").WithName("ResponderQuiz").WithSummary("Registra uma resposta; concede 5 XP por acerto até 50 XP por dia.");

api.MapGet("/ranking", async (string? bairro, GamificationService service, CancellationToken ct) => Results.Ok(await service.RankingAsync(bairro, ct)))
    .RequireAuthorization("Cidadao").WithName("GetRanking").WithSummary("Lista os 50 melhores por XP, opcionalmente filtrados por bairro.");

api.MapPost("/xp/creditos", async (CreditoXpRequest request, HttpRequest http, GamificationService service, CancellationToken ct) =>
{
    var key = http.Headers["Idempotency-Key"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(key)) return Results.Problem("O header Idempotency-Key é obrigatório.", statusCode: 400);
    try { return Results.Ok(await service.CreditarAsync(key, request, ct)); }
    catch (IdempotencyConflictException ex) { return Results.Conflict(new { erro = ex.Message }); }
    catch (ArgumentException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["credito"] = [ex.Message] }); }
}).RequireAuthorization("Sistema").WithName("CreditarXp").WithSummary("Credita evento confirmado de entrega/identificação de forma idempotente.");

api.MapPost("/missoes/reforco", async (ReforcoRequest request, HttpRequest http, GamificationService service, CancellationToken ct) =>
{
    var key = http.Headers["Idempotency-Key"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(key)) return Results.Problem("O header Idempotency-Key é obrigatório.", statusCode: 400);
    try { return Results.Ok(await service.CriarReforcoAsync(key, request, ct)); }
    catch (IdempotencyConflictException ex) { return Results.Conflict(new { erro = ex.Message }); }
    catch (KeyNotFoundException ex) { return Results.NotFound(new { erro = ex.Message }); }
    catch (ArgumentException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["reforco"] = [ex.Message] }); }
}).RequireAuthorization("Sistema").WithName("CriarMissaoReforco").WithSummary("Cria missão de reforço após entrega recusada, sem conceder XP automaticamente.");

app.Run();

static string Subject(ClaimsPrincipal user) => user.FindFirstValue("userId") ?? user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
    ?? throw new InvalidOperationException("JWT autenticado sem claim userId ou sub.");

public partial class Program { }
