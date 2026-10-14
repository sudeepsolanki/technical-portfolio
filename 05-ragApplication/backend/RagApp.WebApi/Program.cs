using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI;
using RagApp.Application;
using RagApp.Infrastructure;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// Database & Infrastructure
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=ragdb;Username=postgres;Password=postgres";
builder.Services.AddInfrastructure(connectionString);

// Ingestion tuning (batch size / concurrency for embeddings, etc.)
builder.Services.AddOptions<RagApp.Application.RagOptions>()
    .Bind(builder.Configuration.GetSection(RagApp.Application.RagOptions.SectionName));

// AI provider (Ollama) via Microsoft.Extensions.AI + OpenAI-compatible client.
// These are only fallbacks; the real values come from appsettings.json / environment.
var aiRouterUrl = (builder.Configuration["AI_ROUTER_URL"] ?? "http://localhost:11434/v1").TrimEnd('/');
var apiKey = builder.Configuration["AI_API_KEY"] ?? "ollama";
var chatModel = builder.Configuration["AI_CHAT_MODEL"] ?? "qwen2.5:3b";
var embeddingModel = builder.Configuration["AI_EMBEDDING_MODEL"] ?? "nomic-embed-text";

var openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
{
    Endpoint = new Uri(aiRouterUrl)
});

builder.Services.AddSingleton(openAIClient);
builder.Services.AddChatClient(openAIClient.GetChatClient(chatModel).AsIChatClient());
builder.Services.AddEmbeddingGenerator(openAIClient.GetEmbeddingClient(embeddingModel).AsIEmbeddingGenerator());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Return clean JSON errors instead of raw 500 stack traces.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var statusCode = exception is AIGatewayException
            ? StatusCodes.Status502BadGateway
            : StatusCodes.Status500InternalServerError;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = exception?.Message ?? "An unexpected error occurred." });
    });
});

// Bring the database schema up-to-date on startup (fresh docker-compose bring-up),
// then create a pgvector HNSW index to accelerate similarity search.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RagDbContext>();
    dbContext.Database.EnsureCreated();

    // EnsureCreated() never alters an existing schema, so add the async-ingestion
    // columns idempotently (existing rows are assumed already "ready"). Default for
    // the Status column keeps pre-existing documents queryable.
    dbContext.Database.ExecuteSqlRaw("""ALTER TABLE "Documents" ADD COLUMN IF NOT EXISTS "Status" text NOT NULL DEFAULT 'ready'""");
    dbContext.Database.ExecuteSqlRaw("""ALTER TABLE "Documents" ADD COLUMN IF NOT EXISTS "Error" text NULL""");
    dbContext.Database.ExecuteSqlRaw("""ALTER TABLE "Documents" ADD COLUMN IF NOT EXISTS "TotalChunks" integer NOT NULL DEFAULT 0""");
    dbContext.Database.ExecuteSqlRaw("""ALTER TABLE "Documents" ADD COLUMN IF NOT EXISTS "ChunksCompleted" integer NOT NULL DEFAULT 0""");

    try
    {
        dbContext.Database.ExecuteSqlRaw(
            """CREATE INDEX IF NOT EXISTS "IX_DocumentChunks_Embedding" ON "DocumentChunks" USING hnsw ("Embedding" vector_cosine_ops)""");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Could not create the HNSW index; continuing without it.");
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();