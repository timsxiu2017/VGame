using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VGame.Hubs;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using VGame.AccountCenter;
using VGame.Game.GameManager;
using VGame.AccountCenter.WalletManager;
using VGame.BetCenter;
using Microsoft.Extensions.Caching.Redis;

namespace VGame
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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1); 
            services.AddCors(options => {
                options.AddPolicy("any",builder=>{
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });
            services.AddDistributedRedisCache(options=>{
                options.InstanceName = Configuration.GetValue<string>("Redis:InstanceName");
                options.Configuration = Configuration.GetValue<string>("Redis:Configuration");
            });
            services.AddSignalR();
            services.AddSingleton<IBetManager,LocalBetManager>();
            services.AddSingleton<IWalletManager,LocalWalletManager>();
            services.AddSingleton<IAccountCenter,LocalAccountCenter>();
            services.AddSingleton<IGameManager,DefaultGameManager>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseMvc();
            app.UseCors("AllowCors");
            
            app.UseSignalR(routes=>{
                routes.MapHub<LaunchHub>("/hubs/game");
            });
            
        }
    }
}
