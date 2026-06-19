using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Middlewares;
using SWP391.Repositories;
using SWP391.Service;
using SWP391.Service.Reports.Pdf;
using SWP391.Models.Dashboard;
using SWP391.Models.Report;
using SWP391.Models.Admin;
using SWP391.Models.Notification;
using SWP391.Models.Integration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;

namespace SWP391
{
    public class Program
    {
        public static void Main(string[] args)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReact",policy =>
                    {
                        policy
                            .WithOrigins("http://localhost:5173",
                                         "http://127.0.0.1:5173")
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    });
            });
            builder.Services.AddControllers();
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
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        const string bearerPrefix = "Bearer ";
                        var authorizationHeader = context.Request.Headers["Authorization"].ToString();

                        if (!authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }

                        var token = authorizationHeader[bearerPrefix.Length..].Trim();
                        if (string.IsNullOrWhiteSpace(token))
                        {
                            return;
                        }

                        var tokenHash = AccountService.HashToken(token);
                        var accountRepository = context.HttpContext.RequestServices.GetRequiredService<AccountRepository>();
                        var isRevoked = await accountRepository.IsTokenRevokedAsync(tokenHash);

                        if (isRevoked)
                        {
                            context.Fail("Token has been revoked.");
                        }
                    }
                };
            });
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrator"));
                
                options.AddPolicy("CanPublishArticle", policy => policy.RequireRole("Administrator", "Researcher"));
                
                options.AddPolicy("IsMember", policy => policy.RequireRole("Administrator", "Researcher", "Member"));
            });
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
            builder.Services.AddScoped<DashboardReportRepository>();
            builder.Services.AddScoped<DashboardService>();
            builder.Services.AddScoped<IReportPdfRenderer, QuestPdfReportRenderer>();
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
            builder.Services.AddScoped<ActivityLogRepository>();
            builder.Services.AddScoped<ActivityLogService>();
            builder.Services.AddHostedService<TrendComputeBackgroundService>();
            builder.Services.Configure<DataSyncSchedulerOptions>(
                    builder.Configuration.GetSection(DataSyncSchedulerOptions.SectionName));
            builder.Services.AddHostedService<DataSyncSchedulerHostedService>();
            builder.Services.AddEndpointsApiExplorer();
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
            var app = builder.Build();
            app.UseMiddleware<ExceptionMiddleware>();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseHttpsRedirection();
            app.UseCors("AllowReact"); 
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
