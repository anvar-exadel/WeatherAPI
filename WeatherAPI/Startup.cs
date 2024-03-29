using BusinessLogic.interfaces;
using BusinessLogic.services;
using DatabaseAccess;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.configFiles;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeatherAPI
{
    public class Startup
    {
        public IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string conStr = _configuration.GetConnectionString("DefaultConnection");

            GlobalConfiguration.Configuration.UseSqlServerStorage(conStr);

            services.Configure<MailSettings>(_configuration.GetSection("MailSettings"));
            services.AddDbContext<AppDbContext>(o => o.UseSqlServer(conStr));
            services.AddHangfire(x => x.UseSqlServerStorage(conStr));
            services.AddHangfireServer();

            if (_configuration.GetValue<bool>("EmailSettingFlag:LocalSender"))
            {
                services.AddSingleton<IMailService, MailLocalService>();
            }
            else
            {
                services.AddSingleton<ISendRabbit, SendRabbit>();
                services.AddSingleton<IMailService, MailRabbitService>();
            }

            services.AddScoped<IWeatherService, WeatherService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IMailSubService, MailSubService>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["TokenKey"])),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WeatherAPI", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WeatherAPI v1"));

            app.UseHangfireDashboard("/dashboard");

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors(x => x.AllowAnyMethod().AllowAnyHeader().WithOrigins("http:client url"));

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
