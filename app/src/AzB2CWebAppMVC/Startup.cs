using AzureADB2CWeb.Data;
using AzureADB2CWeb.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AzureADB2CWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public static string Tenant = "edulabworksB2C.onmicrosoft.com";
        public static string AzureADB2CHostname = "edulabworksB2C.b2clogin.com";
        public static string ClientID = "644963b4-83fb-4132-b3e5-b40aa976f1e4";
        public static string PolicySignUpSignIn = "B2C_1A_SIGNUP_SIGNIN";
        public static string PolicyEditProfile = "B2C_1_Edit";
        public static string Scope = "https://edulabworksB2C.onmicrosoft.com/azureb2capi/fullaccess";
        public static string ClientSecret = "";

        public static string AuthorityBase = $"https://{AzureADB2CHostname}/{Tenant}/";
        public static string AuthoritySignInUp = $"{AuthorityBase}{PolicySignUpSignIn}/v2.0";
        public static string AuthorityEditProfile = $"{AuthorityBase}{PolicyEditProfile}/v2.0";
        
               

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(1);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(
                Configuration.GetConnectionString("DefaultConnection")));
            services.AddHttpClient();
            services.AddControllersWithViews();
            services.AddScoped<IUserService, UserService>();
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            }).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options => {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Authority = Startup.AuthoritySignInUp;
                options.ClientId = Startup.ClientID;
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.Scope.Add(Startup.Scope);
                options.ClientSecret = Startup.ClientSecret;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
                };
                
            })
            .AddOpenIdConnect("B2C_1_Edit",GetOpenIdConnectOptions("B2C_1_Edit"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }


        private Action<OpenIdConnectOptions> GetOpenIdConnectOptions(string policy) => options =>
        {
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.Authority = Startup.AuthorityEditProfile;
            options.ClientId = Startup.ClientID;
            options.ResponseType = "code";
            options.SaveTokens = true;
            options.Scope.Add(Startup.Scope);
            options.ClientSecret = Startup.ClientSecret;
            options.CallbackPath = "/signin-oidc-" + policy;
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
            };
            options.Events = new OpenIdConnectEvents
            {
                OnMessageReceived = context =>
                {
                    if (!string.IsNullOrEmpty(context.ProtocolMessage.Error) &&
                    !string.IsNullOrEmpty(context.ProtocolMessage.ErrorDescription))
                    {
                        if (context.ProtocolMessage.Error.Contains("access_denied"))
                        {
                            context.HandleResponse();
                            context.Response.Redirect("/");
                        }
                    }
                    return Task.FromResult(0);
                }
            };
        };
    }
}
