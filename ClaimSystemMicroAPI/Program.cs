using ClaimSystemMicroAPI.Data;
using ClaimSystemMicroAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace ClaimSystemMicroAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add Database Context
            builder.Services.AddDbContext<ClaimAPIDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


            // Add Auth Service
            builder.Services.AddScoped<AuthService>();

            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowMVC", policy =>
                {
                    policy.WithOrigins("https://localhost:", "http://localhost:") 
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // Add logging
            builder.Services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Use CORS
            app.UseCors("AllowMVC");

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
