using api_app.Database;
using api_app.Repositories;
using api_app.Repositories.Interfaces;
using api_app.Services;
using api_app.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<DbConnection>();

// Register ApplicationDbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost\\SQLEXPRESS;Database=ListDB;Trusted_Connection=True;TrustServerCertificate=True;";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectUserRepository, ProjectUserRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin =>
              {
                  if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                  {
                      return true;
                  }

                  if (origin.Equals("http://localhost:3000", StringComparison.OrdinalIgnoreCase) ||
                  origin.Equals("http://localhost:4321", StringComparison.OrdinalIgnoreCase) ||
                  origin.Equals("http://127.0.0.1:5500", StringComparison.OrdinalIgnoreCase) ||
                      origin.Equals("http://localhost:5173", StringComparison.OrdinalIgnoreCase))
                  {
                      return true;
                  }


                  if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                  {
                      return false;
                  }

                  return uri.Host.EndsWith(".ngrok-free.dev", StringComparison.OrdinalIgnoreCase);
              })
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
var app = builder.Build();

var runDbFresh = args.Contains("--db:fresh", StringComparer.OrdinalIgnoreCase);
var runDbSeed = args.Contains("--db:seed", StringComparer.OrdinalIgnoreCase);

// Auto-apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (runDbFresh)
    {
        db.Database.EnsureDeleted();
    }

    db.Database.Migrate();

    if (runDbFresh || runDbSeed)
    {
        DatabaseSeeder.SeedAsync(db).GetAwaiter().GetResult();
    }
}

if (runDbFresh || runDbSeed)
{
    Console.WriteLine(runDbFresh
        ? "Database fresh + seed selesai."
        : "Database seed selesai.");
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
