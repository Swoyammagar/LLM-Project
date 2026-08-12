using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace Backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
        public DbSet<Conversation> Conversations => Set<Conversation>();
        public DbSet<Message> Messages => Set<Message>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ═════════════════════════════════════════════════════════════════
            // PGVECTOR CONFIGURATION
            // ═════════════════════════════════════════════════════════════════
            // Enable pgvector extension
            modelBuilder.HasPostgresExtension("vector");

            // Configure User entity
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.GoogleId)
                .IsUnique();

            // Configure RefreshToken entity
            modelBuilder.Entity<RefreshToken>()
                .HasKey(rt => rt.Id);

            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Document entity
            modelBuilder.Entity<Document>()
                .HasKey(d => d.Id);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.User)
                .WithMany(u => u.Documents)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Document>()
                .HasIndex(d => d.UserId);

            // ═════════════════════════════════════════════════════════════════
            // DOCUMENT CHUNK CONFIGURATION (with pgvector support)
            // ═════════════════════════════════════════════════════════════════
            modelBuilder.Entity<DocumentChunk>()
                .HasKey(dc => dc.Id);

            modelBuilder.Entity<DocumentChunk>()
                .HasOne(dc => dc.Document)
                .WithMany(d => d.Chunks)
                .HasForeignKey(dc => dc.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentChunk>()
                .HasIndex(dc => dc.DocumentId);

            modelBuilder.Entity<DocumentChunk>()
                .HasIndex(dc => new { dc.DocumentId, dc.ChunkIndex });

            // IMPORTANT: Configure Vector property with pgvector support
            // This tells EF Core to use pgvector's vector type
            modelBuilder.Entity<DocumentChunk>()
                .Property(dc => dc.Embedding)
                .HasColumnType("vector(384)");

            // ═════════════════════════════════════════════════════════════════
            // CONVERSATION CONFIGURATION
            // ═════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Conversation>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.User)
                .WithMany(u => u.Conversations)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Conversation>()
                .HasIndex(c => c.UserId);

            modelBuilder.Entity<Conversation>()
                .HasIndex(c => new { c.UserId, c.IsDeleted });

            modelBuilder.Entity<Conversation>()
                .HasIndex(c => new { c.UserId, c.UpdatedAt });

            // ═════════════════════════════════════════════════════════════════
            // MESSAGE CONFIGURATION
            // ═════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Message>()
                .HasKey(m => m.Id);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasIndex(m => m.ConversationId);

            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.ConversationId, m.CreatedAt });

            modelBuilder.Entity<Message>()
                .Property(m => m.Question)
                .HasColumnType("text");

            modelBuilder.Entity<Message>()
                .Property(m => m.Answer)
                .HasColumnType("text");

            modelBuilder.Entity<Message>()
                .Property(m => m.RetrievedContext)
                .HasColumnType("text");

            modelBuilder.Entity<Message>()
                .Property(m => m.DocumentReferences)
                .HasColumnType("jsonb");
        }

        /// <summary>
        /// Ensures pgvector extension is enabled in PostgreSQL.
        /// Must be called after the database is created but before using vectors.
        /// </summary>
        public async Task EnsurePgvectorExtensionAsync()
        {
            try
            {
                await Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS vector;");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to enable pgvector extension. Ensure pgvector is installed in PostgreSQL. " +
                    "Run: CREATE EXTENSION IF NOT EXISTS vector;",
                    ex);
            }
        }
    }
}