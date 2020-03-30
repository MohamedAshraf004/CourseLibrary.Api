using AutoMapper;
using CourseLibrary.API.DbContexts;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using System;

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
            ConfigurationForRequestAndValidation(services);
            

            services.AddScoped<ICourseLibraryRepository, CourseLibraryRepository>();

            services.AddDbContext<CourseLibraryContext>(options =>
            {
                options.UseSqlServer(
                    @"Server=(localdb)\mssqllocaldb;Database=CourseLibraryDB;Trusted_Connection=True;");
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            });

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
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

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
        private static void ConfigurationForRequestAndValidation(IServiceCollection services)
        {
            services.AddControllers(setup =>
            {
                setup.ReturnHttpNotAcceptable = true; //default false that is accept json only must specify content-type and accep
            }).AddNewtonsoftJson(setupAction =>
            {
                setupAction.SerializerSettings.ContractResolver =
                   new CamelCasePropertyNamesContractResolver();
            })
                .AddXmlDataContractSerializerFormatters()
                               .ConfigureApiBehaviorOptions(setupAction =>
                               {
                                   setupAction.InvalidModelStateResponseFactory = context =>
                                   {
                                       // create a problem details object
                                       var problemDetailsFactory = context.HttpContext.RequestServices
                                                       .GetRequiredService<ProblemDetailsFactory>();
                                       var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(
                                               context.HttpContext,
                                               context.ModelState);

                                       // add additional info not added by default
                                       problemDetails.Detail = "See the errors field for details.";
                                       problemDetails.Instance = context.HttpContext.Request.Path;

                                       // find out which status code to use
                                       var actionExecutingContext =
                                                         context as Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext;

                                       // if there are modelstate errors & all keys were correctly
                                       // found/parsed we're dealing with validation errors
                                       if ((context.ModelState.ErrorCount > 0) &&
                                                       (actionExecutingContext?.ActionArguments.Count == context.ActionDescriptor.Parameters.Count))
                                       {
                                           problemDetails.Type = "https://courselibrary.com/modelvalidationproblem";
                                           problemDetails.Status = StatusCodes.Status422UnprocessableEntity;
                                           problemDetails.Title = "One or more validation errors occurred.";

                                           return new UnprocessableEntityObjectResult(problemDetails)
                                           {
                                               ContentTypes = { "application/problem+json" }
                                           };
                                       }

                                       // if one of the keys wasn't correctly found / couldn't be parsed
                                       // we're dealing with null/unparsable input
                                       problemDetails.Status = StatusCodes.Status400BadRequest;
                                       problemDetails.Title = "One or more errors on input occurred.";
                                       return new BadRequestObjectResult(problemDetails)
                                       {
                                           ContentTypes = { "application/problem+json" }
                                       };
                                   };
                               })
                        .SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_3_0);

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
