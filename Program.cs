using ResolveAi.Api.Data;
using ResolveAi.Api.Repositories;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// LOGGING
// =====================================================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// =====================================================
// CONFIGURAÇÕES
// =====================================================
builder.Configuration.AddEnvironmentVariables();

// =====================================================
// FORWARDED HEADERS (PROXY / EDGE)
// =====================================================
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// =====================================================
// CORS
// =====================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("PublicApi", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// =====================================================
// BANCO DE DADOS
// =====================================================
var conexao = new ConexaoResolveAi(builder.Configuration);
var connectionString = conexao.ConnectionString;

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 36)),
            mySqlOptions =>
            {
                mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null
                );
            }
        );
    });
}

// =====================================================
// DEPENDÊNCIAS
// =====================================================
builder.Services.AddScoped<UsuarioRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =====================================================
// BUILD
// =====================================================
var app = builder.Build();

// =====================================================
// MIDDLEWARE (ORDEM IMPORTA)
// =====================================================
app.UseForwardedHeaders();

app.UseSwagger();

// 🔑 força rota /swagger
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "swagger";
});

app.UseCors("PublicApi");

app.UseAuthorization();

// =====================================================
// ROTAS
// =====================================================
app.MapGet("/health", () => Results.Ok("API ONLINE"));
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapControllers();

// =====================================================
// RUN
// =====================================================
app.Run();
