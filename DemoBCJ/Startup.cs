// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using DemoBCJ.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DemoBCJ
{
    /// <summary>
    ///     The Startup class configures services and the request pipeline.
    /// </summary>
    public class Startup
    {
        /// <summary>
        ///     Gets the configuration that represents a set of key/value application configuration properties.
        /// </summary>
        /// <value>
        ///     The <see cref="IConfiguration" /> that represents a set of key/value application configuration properties.
        /// </value>
        public IConfiguration Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                          .SetBasePath(env.ContentRootPath)
                          .AddJsonFile("appsettings.json", true, true)
                          .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                          .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDefaultFiles()
               .UseStaticFiles()
               .UseBotFramework();
        }

        /// <summary>
        ///     This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="IServiceCollection" /> specifies the contract for a collection of service
        ///     descriptors.
        /// </param>
        /// <seealso cref="IStatePropertyAccessor{T}" />
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/dependency-injection" />
        /// <seealso
        ///     cref="https://docs.microsoft.com/en-us/azure/bot-service/bot-service-manage-channels?view=azure-bot-service-4.0" />
        public void ConfigureServices(IServiceCollection services)
        {
            var secretKey = Configuration.GetSection("botFileSecret")
                                         ?.Value;
            var botFilePath = Configuration.GetSection("botFilePath")
                                           ?.Value;

            // Loads .bot configuration file and adds a singleton that your Bot can access through dependency injection.
            var botConfig = BotConfiguration.Load(botFilePath ?? @".\nlp-with-luis.bot", secretKey);
            services.AddSingleton(sp => botConfig ?? throw new InvalidOperationException($"The .bot config file could not be loaded. ({botConfig})"));

            // Initialize Bot Connected Services clients.
            var connectedServices = new BotServices(botConfig);
            services.AddSingleton(sp => connectedServices);
            services.AddSingleton(sp => botConfig);

            services.AddBot<DemoBCJBot>(options =>
                                        {
                                            // Retrieve current endpoint.
                                            var environment = "development";
                                            var service = botConfig.Services.Where(s => s.Type == "endpoint" && s.Name == environment)
                                                                   .FirstOrDefault();
                                            if (!(service is EndpointService endpointService))
                                            {
                                                throw new InvalidOperationException($"The .bot file does not contain an endpoint with name '{environment}'.");
                                            }

                                            options.CredentialProvider = new SimpleCredentialProvider(endpointService.AppId, endpointService.AppPassword);
                                        });
        }
    }
}