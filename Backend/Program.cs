using Backend.Configuration;
using Backend.Data;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Backend.Services.Interfaces;
using Pgvector.EntityFrameworkCore;

namespace Backend;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // ═════════════════════════════════════════════════════════════════
        // DATABASE CONFIGURATION
        // ═════════════════════════════════════════════════════════════════
        builder.Services.AddDbContext<ApplicationDbContext>(
            options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.UseVector()
                )
        );

        // ═════════════════════════════════════════════════════════════════
        // OPTIONS PATTERN CONFIGURATION
        // ═════════════════════════════════════════════════════════════════
        builder.Services.Configure<OllamaOptions>(
            builder.Configuration.GetSection("Ollama")
        );

        builder.Services.Configure<SemanticSearchOptions>(
            builder.Configuration.GetSection("SemanticSearch")
        );

        builder.Services.Configure<RetrievalOptions>(
            builder.Configuration.GetSection("Retrieval")
        );

        builder.Services.Configure<OllamaLLMOptions>(
            builder.Configuration.GetSection("OllamaLLM")
        );

        // ═════════════════════════════════════════════════════════════════
        // SERVICE REGISTRATION (Dependency Injection)
        // ═════════════════════════════════════════════════════════════════

        builder.Services.AddScoped<GoogleAuthService>();
        builder.Services.AddScoped<JwtTokenService>();
        builder.Services.AddScoped<EmailAuthService>();
        builder.Services.AddScoped<PasswordService>();

        // Document Management Pipeline
        builder.Services.AddScoped<IDocumentService, DocumentService>();
        builder.Services.AddScoped<ITextExtractionService, TextExtractionService>();
        builder.Services.AddScoped<ITextChunkingService, TextChunkingService>();
        builder.Services.AddScoped<IEmbeddingService, OllamaEmbeddingService>();

        // Semantic Search Layer
        builder.Services.AddScoped<ISemanticSearchService, SemanticSearchService>();

        // Retrieval Layer
        builder.Services.AddScoped<IRetrievalService, RetrievalService>();

        // LLM Layer
        builder.Services.AddScoped<ILLMService, OllamaLLMService>();

        // Chat Service (RAG)
        builder.Services.AddScoped<IChatService, ChatService>();

        // Conversation Management
        builder.Services.AddScoped<IConversationService, ConversationService>();

        var jwtSettings = builder.Configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not found in configuration");

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                policy.WithOrigins(builder.Configuration["Frontend:Url"])
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        var app = builder.Build();

        // ═════════════════════════════════════════════════════════════════
        // ENSURE PGVECTOR EXTENSION IS ENABLED
        // ═════════════════════════════════════════════════════════════════
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                await dbContext.EnsurePgvectorExtensionAsync();
            }
            catch (Exception)
            {
            }
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseCors("Frontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}