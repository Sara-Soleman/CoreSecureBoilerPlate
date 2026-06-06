
using Application;
using CoreSecureBoilerPlate.Endpoints;
using CoreSecureBoilerPlate.Middleware;
using Infrastructure;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;


namespace CoreSecureBoilerPlate
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
             builder.Services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    context.ProblemDetails.Extensions["correlationId"] =
                        context.HttpContext.Response.Headers["X-Correlation-ID"].ToString();
                };
            }); // Generates baseline standard metadata
            //Forcing the system to ignore capital/lowercase case when reading JSON
            builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
            {
                options.SerializerOptions.PropertyNameCaseInsensitive = true;
            });
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                // Allow the serializer to read numbers written inside string quotes automatically
                options.SerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
            });


            // --- Register Health Check Infrastructures ---
            builder.Services.AddHealthChecks()
                // 1. Tag basic self-liveness check
                .AddCheck("Self", () => HealthCheckResult.Healthy("System is running stable."), tags: ["liveness"])
                // 2. Add SQL Server infrastructure database validation
                .AddDbContextCheck<ApplicationDbContext>(
                    name: "Database-Check",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["readiness"]);


            // Add services to the container.

            //  (Dependency Injection)
            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructureServices(builder.Configuration);
           
           

            //SeriLog
            Log.Logger = new LoggerConfiguration()
             .ReadFrom.Configuration(builder.Configuration)
             .MinimumLevel.Information()
            .Enrich.FromLogContext() //Enable reading of added properties such as CorrelationId
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}") 
            .CreateLogger();
            builder.Host.UseSerilog();

            //  JWT
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "SecureBankingIssuer",
                    ValidAudience = "SecureBankingAudience",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SUPER_SECRET_KEY_NEVER_SHARE_IT_MAKE_IT_VERY_LONG_1234567890!"))
                };
            });

            // Rate Limiting 
            builder.Services.AddRateLimiter(options =>
            {
                // Defining system behavior when blocking a user (returning code 429 Too Many Requests)
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // A special policy for protecting the login point based on the user's IP address
                options.AddPolicy("LoginPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 5,                          // Allow only 5 attempts
                            Window = TimeSpan.FromMinutes(1),         // Within one minute
                            QueueLimit = 0                            // Reject any additional requests immediately without queuing
                        }));
                options.AddPolicy("ProductPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "Global",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 60, // Allow only 60 requests
                            Window = TimeSpan.FromMinutes(1) // Within one minute
                        }));
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";

                    var responseObj = new
                    {
                        Error = "You have exceeded the allowed number of login attempts. You have been temporarily blocked for one minute to protect your account."
                    };

                    await context.HttpContext.Response.WriteAsJsonAsync(responseObj, cancellationToken);
                };
            });


            builder.Services.AddScoped<JwtTokenGenerator>();

            // --- Configure Cross-Origin Resource Sharing (CORS) Production-Grade Policies ---
            builder.Services.AddCors(options =>
            {
                // Strict production environment configuration boundary
                options.AddPolicy("ProductionCorsPolicy", policy =>
                {
                    policy.WithOrigins("https://yourbankclient.com", "https://admin.yourbankclient.com") // Only trust white-listed banking domains
                          .AllowAnyMethod() // Allow HTTP HTTP verbs: GET, POST, PUT, DELETE
                          .AllowAnyHeader() // Allow customized HTTP header fields like X-Correlation-ID
                          .AllowCredentials(); // Crucial: Allows the browser to send HttpOnly cookies securely across domains
                });

                // Flexible local development environment setup
                options.AddPolicy("DevelopmentCorsPolicy", policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "http://localhost:4200") // Trust default local React/Angular development servers
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });


            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();
            app.UseExceptionHandler();
            app.UseMiddleware<CorrelationIdMiddleware>();
            try
            {
                
                Log.Information("Starting up the system and initializing infrastructure services...");
                // The order here is critical to the life or death of the banking project:
                app.UseAuthentication(); // 1. Who are you?
                app.UseAuthorization();  // 2. What are your permissions?
                
            // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                        app.UseCors("DevelopmentCorsPolicy");
                        app.MapOpenApi();
                    app.MapScalarApiReference();
                }
                else
                {
                    app.UseCors("ProductionCorsPolicy");
                }


                // --- Map Liveness Endpoint ---
                app.MapHealthChecks("/health/live", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("liveness"),
                    ResponseWriter = WriteResponseAsync
                });

                // --- Map Readiness Endpoint ---
                app.MapHealthChecks("/health/ready", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("readiness"),
                    ResponseWriter = WriteResponseAsync
                });

                app.UseRateLimiter(); 
                app.UseHttpsRedirection();

            app.UseAuthorization();

            // Activating the link maps we created for products using Minimal APIs
            app.MapProductEndpoints();
            app.MapIdentityEndpoints();
            app.MapControllers();

            app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to start the application unexpectedly!");
            }
            finally
            {
                Log.CloseAndFlush(); // Safely close the files when the server stops
            }
        }

        // --- Structured JSON Response Writer ---
        static Task WriteResponseAsync(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var options = new JsonWriterOptions { Indented = true };
            using var memoryStream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(memoryStream, options))
            {
                writer.WriteStartObject();
                writer.WriteString("status", report.Status.ToString());
                writer.WriteString("totalDuration", report.TotalDuration.ToString());

                writer.WriteStartObject("results");
                foreach (var entry in report.Entries)
                {
                    writer.WriteStartObject(entry.Key);
                    writer.WriteString("status", entry.Value.Status.ToString());
                    writer.WriteString("description", entry.Value.Description);
                    writer.WriteString("duration", entry.Value.Duration.ToString());

                    if (entry.Value.Exception != null)
                    {
                        writer.WriteString("exception", entry.Value.Exception.Message);
                    }

                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            return context.Response.WriteAsync(Encoding.UTF8.GetString(memoryStream.ToArray()));
        }
    }



}
