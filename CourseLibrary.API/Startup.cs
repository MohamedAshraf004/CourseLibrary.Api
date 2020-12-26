using AutoMapper;
using CourseLibrary.API.Cache;
using CourseLibrary.API.DbContexts;
using CourseLibrary.API.Options;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace CourseLibrary.API
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
            var jwtSettings = new JwtSettings();
            Configuration.Bind(nameof(jwtSettings), jwtSettings);
            services.AddSingleton(jwtSettings);
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<ICourseLibraryRepository, CourseLibraryRepository>();
            services.AddSingleton<IUriService>(provider =>
            {
                var accessor = provider.GetRequiredService<IHttpContextAccessor>();
                var request = accessor.HttpContext.Request;
                var absoluteUri = string.Concat(request.Scheme, "://", request.Host.ToUriComponent(), "/");
                return new UriService(absoluteUri);
            });



            services.AddIdentity<IdentityUser, IdentityRole>()
               .AddEntityFrameworkStores<CourseLibraryContext>();

            ConfigurationForRequestAndValidation(services);
            services.AddControllers(setup =>
            {
                setup.ReturnHttpNotAcceptable = true; //default false that is accept json only must specify content-type and accep
            }).
            AddNewtonsoftJson(setupAction =>
            {
                setupAction.SerializerSettings.ContractResolver =
                   new CamelCasePropertyNamesContractResolver();
            }).AddXmlDataContractSerializerFormatters()
                              .ConfigureApiBehaviorOptions(setupAction =>
                              {
                                  setupAction.InvalidModelStateResponseFactory = context =>
                                  {
                                      var problemDetails = new ValidationProblemDetails(context.ModelState)
                                      {
                                          // add additional info not added by default
                                          Detail = "See the errors field for details.",
                                          Instance = context.HttpContext.Request.Path,
                                          Status = StatusCodes.Status422UnprocessableEntity,
                                          Type = "https://courselibrary.com/modelvalidationproblem",
                                          Title = "One or more validation errors occurred."

                                      };
                                      problemDetails.Extensions.Add("traceId", context.HttpContext.TraceIdentifier);
                                      return new UnprocessableEntityObjectResult(problemDetails)
                                      {
                                          ContentTypes = { "application/problem+json" }
                                      };
                                  };
                              })
                     .SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_3_0);



            services.AddDbContext<CourseLibraryContext>(options =>
            {
                options.UseSqlServer(
                    @"Server=(localdb)\mssqllocaldb;Database=CourseLibraryDB;Trusted_Connection=True;");
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1,0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new HeaderApiVersionReader("X-API-Version"); //for v header
                //when using query string i comment header option and use in query=> api-version=2.0
            });


            //services.AddAuthentication("Bearer").AddJwtBearer("Bearer", options =>
            // {
            //     options.Authority = "https://localhost:51044";
            //     options.RequireHttpsMetadata = false;
            //     options.Audience = "hts-api"; //audinace name from identity server 4
            // });

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Secret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                RequireExpirationTime = false,
                ValidateLifetime = true
            };

            services.AddSingleton(tokenValidationParameters);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(x =>
                {
                    x.SaveToken = true;
                    x.TokenValidationParameters = tokenValidationParameters;
                });


            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins("https://localhost:51044").AllowAnyHeader().AllowAnyMethod();
                });
            });

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());



            //Caching 
            var redisCacheSettings = new RedisCacheSettings();
            Configuration.GetSection(nameof(RedisCacheSettings)).Bind(redisCacheSettings);
            services.AddSingleton(redisCacheSettings);

            if (!redisCacheSettings.Enabled)
            {
                return;
            }

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisCacheSettings.ConnectionString));
            services.AddStackExchangeRedisCache(options => options.Configuration = redisCacheSettings.ConnectionString);
            services.AddSingleton<IResponseCacheService, ResponseCacheService>();
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(option=> {
                    option.Run(async context =>
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("An unexpected fault happened. Try again later.");

                    });
                });
            }

            app.UseStatusCodePages();
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors();
          //  app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
        private static void ConfigurationForRequestAndValidation(IServiceCollection services)
        {
            //services.AddControllers(setup =>
            //{
            //    setup.ReturnHttpNotAcceptable = true; //default false that is accept json only must specify content-type and accep
            //}).AddNewtonsoftJson(setupAction =>
            //{
            //    setupAction.SerializerSettings.ContractResolver =
            //       new CamelCasePropertyNamesContractResolver();
            //})
            //    .AddXmlDataContractSerializerFormatters()
            //                   .ConfigureApiBehaviorOptions(setupAction =>
            //                   {
            //                       setupAction.InvalidModelStateResponseFactory = context =>
            //                       {
            //                           // create a problem details object
            //                           var problemDetailsFactory = context.HttpContext.RequestServices
            //                                           .GetRequiredService<ProblemDetailsFactory>();
            //                           var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(
            //                                   context.HttpContext,
            //                                   context.ModelState);

            //                           // add additional info not added by default
            //                           problemDetails.Detail = "See the errors field for details.";
            //                           problemDetails.Instance = context.HttpContext.Request.Path;

            //                           // find out which status code to use
            //                           var actionExecutingContext =
            //                                             context as Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext;

            //                           // if there are modelstate errors & all keys were correctly
            //                           // found/parsed we're dealing with validation errors
            //                           if ((context.ModelState.ErrorCount > 0) &&
            //                                           (actionExecutingContext?.ActionArguments.Count == context.ActionDescriptor.Parameters.Count))
            //                           {
            //                               problemDetails.Type = "https://courselibrary.com/modelvalidationproblem";
            //                               problemDetails.Status = StatusCodes.Status422UnprocessableEntity;
            //                               problemDetails.Title = "One or more validation errors occurred.";

            //                               return new UnprocessableEntityObjectResult(problemDetails)
            //                               {
            //                                   ContentTypes = { "application/problem+json" }
            //                               };
            //                           }

            //                           // if one of the keys wasn't correctly found / couldn't be parsed
            //                           // we're dealing with null/unparsable input
            //                           problemDetails.Status = StatusCodes.Status400BadRequest;
            //                           problemDetails.Title = "One or more errors on input occurred.";
            //                           return new BadRequestObjectResult(problemDetails)
            //                           {
            //                               ContentTypes = { "application/problem+json" }
            //                           };
            //                       };
            //                   })
            //            .SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_3_0);

            #region formatter for core 2.2
            ////services.AddMvc()
            ////  .AddMvcOptions(o =>
            ////  {
            ////      o.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
            ////  })
            ////  .SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_3_0);
            ////.AddJsonOptions(o =>
            ////{
            ////    if (o.SerializerSettings.ContractResolver != null)
            ////    {
            ////        var castedResolver = o.SerializerSettings.ContractResolver
            ////                               as DefaultContractResolver;
            ////        castedResolver.NamingStrategy = null;
            ////    }
            ////});
            #endregion
        }

    }
}
