using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Middlewares;
using SWP391.Repositories;
using SWP391.Service;
using SWP391.Models.Dashboard;
using SWP391.Models.Report;
using SWP391.Models.Admin;
using SWP391.Models.Notification;
using SWP391.Models.Integration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

namespace SWP391
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // CORS for the Frontend
            builder.Services.AddCors(options => 
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Add services to the container.
            builder.Services.AddControllers();

            // Set up JWT Authentication ====================
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
                    ValidAudience = builder.Configuration["JWT:ValidAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"] ?? "SuperSecretKeyForJWTWhichMustBeMoreThan16Chars!"))
                };
            });
            // ===============================================

            // === [TH�M M?I] C?u h�nh Policy-Based Authorization ===
            builder.Services.AddAuthorization(options =>
            {
                // Ch�nh s�ch: Ch? c� Administrator m?i ???c ph�p
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrator"));
                
                // Ch�nh s�ch: Cho ph�p Administrator HO?C Researcher (Nh� nghi�n c?u)
                options.AddPolicy("CanPublishArticle", policy => policy.RequireRole("Administrator", "Researcher"));
                
                // Ch�nh s�ch: Y�u c?u l� Member tr? l�n
                options.AddPolicy("IsMember", policy => policy.RequireRole("Administrator", "Researcher", "Member"));
            });
            // ===============================================

            builder.Services.AddDbContext<ScientificTrendDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
                
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<AccountRepository>();
            builder.Services.AddScoped<AccountService>();
            builder.Services.AddScoped<PaperRepository>();
            builder.Services.AddScoped<PaperService>();
            builder.Services.AddScoped<TrendRepository>();
            builder.Services.AddScoped<TrendService>();
            builder.Services.AddScoped<AcademicDataIntegrationService>();
            builder.Services.AddScoped<DataSyncService>();
            builder.Services.AddScoped<DashboardRepository>();
            builder.Services.AddScoped<DashboardService>();
            builder.Services.AddScoped<ReportService>();
            builder.Services.AddScoped<NotificationRepository>();
            builder.Services.AddScoped<NotificationService>();
            builder.Services.AddScoped<NotificationTriggerService>();
            builder.Services.AddScoped<AdminRepository>();
            builder.Services.AddScoped<AdminService>();
            builder.Services.AddScoped<BookmarkRepository>();
            builder.Services.AddScoped<BookmarkService>();
            builder.Services.AddScoped<AuthorRepository>();
            builder.Services.AddScoped<AuthorService>();
            builder.Services.AddScoped<FollowRepository>();
            builder.Services.AddScoped<FollowService>();
            builder.Services.AddHostedService<TrendComputeBackgroundService>();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            // Swagger configuration to include JWT authentication in the UI
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Enter token by format {token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                        },
                        new List<string>()
                    }
                });
            });
            // ===========================================================

            var app = builder.Build();
            
            // Configure the HTTP request pipeline.
            app.UseMiddleware<ExceptionMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors(); 
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
