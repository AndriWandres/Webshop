using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using Webshop.Api.Configuration;
using Webshop.Api.Database;
using Webshop.Api.Services;
using Webshop.Api.SignalR.Hubs;

namespace Webshop.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add Database Context
            services.AddDbContext<WebshopContext>(builder =>
            {
                builder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            });

            var jwtSection = Configuration.GetSection("Jwt");
            var jwtSettings = jwtSection.Get<JwtSettings>();
            var jwtSecret = Encoding.ASCII.GetBytes(jwtSettings.Secret);

            // Add JWT Configuration
            services.Configure<JwtSettings>(jwtSection);

            // Add JWT Bearer Authentication
            services.AddAuthentication(builder =>
            {
                builder.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                builder.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(builder =>
            {
                builder.RequireHttpsMetadata = false;
                builder.SaveToken = true;
                builder.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(jwtSecret),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                };

                // Attach SignalR Access Token
                builder.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        PathString path = context.HttpContext.Request.Path;
                        string token = context.Request.Query["access_token"];

                        if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/hubs/webshop"))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            // Add SignalR Websocket
            services.AddSignalR();

            // Add AutoMapper
            services.AddAutoMapper(typeof(Startup));

            // Add Swagger
            services.AddSwaggerGen(options =>
            {
                // Swagger Document
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Webshop REST API",
                    Description = "REST API for Webshop E-Commerce Application"
                });

                // Add Bearer Security Definition
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                // Add Bearer Security Requirement
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new string[] { }
                    }
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });

            // Add Custom Services
            services.AddScoped<CryptoService>();
            services.AddScoped<AuthService>();
            services.AddScoped<ProductService>();

            // Add Cross-Origin-Resource-Sharing
            services.AddCors();

            // Add Support for controllers
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Use JWT Bearer Authentication
            app.UseAuthentication();

            // Use Swaggger UI
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Webshop REST API");
                options.DocExpansion(DocExpansion.None);
            });

            // Use Cross-Origin-Resource-Sharing (please re-configure for production)
            app.UseCors(builder =>
            {
                builder
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin();
            });

            // Use Routing
            app.UseRouting();

            // Use Authorization
            app.UseAuthorization();

            // Map Controller + SignalR Endpoints
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<WebshopHub>("/webshop");
            });
        }
    }
}
