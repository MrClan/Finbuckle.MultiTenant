﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthenticationOptionsSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().
                SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).
                AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    // Required for Safari 12 issue and OpenID Connect.
                    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                }).
                AddFacebook("Facebook", options =>
                {
                    // These configuration settings should be set via user-secrets or environment variables!
                    options.AppId = Configuration.GetValue<string>("FacebookAppId");
                    options.AppSecret = Configuration.GetValue<string>("FacebookAppSecret");
                    options.Scope.Add("email");
                    options.Fields.Add("name");
                    options.Fields.Add("email");
                }).
                AddGoogle("Google", options =>
                {
                    // These configuration settings should be set via user-secrets or environment variables!
                    options.ClientId = Configuration.GetValue<string>("GoogleClientId");
                    options.ClientSecret = Configuration.GetValue<string>("GoogleClientSecret");
                    options.AuthorizationEndpoint = string.Concat(options.AuthorizationEndpoint, "?prompt=consent");
                }).
                AddOpenIdConnect("OpenIdConnect", options =>
                {
                    // These configuration settings should be set via user-secrets or environment variables!
                    options.ClientId = Configuration.GetValue<string>("OpenIdConnectClientId");
                    options.Authority = Configuration.GetValue<string>("OpenIdConnectAuthority");
                });

            services.AddMultiTenant().
                WithConfigurationStore().
                WithRouteStrategy(ConfigRoutes).
                WithRemoteAuthentication(). // Important!
                WithPerTenantOptions<AuthenticationOptions>((options, tenantInfo) =>
                {
                    // Allow each tenant to have a different default challenge scheme.
                    if (tenantInfo.Items.TryGetValue("ChallengeScheme", out object challengeScheme))
                    {
                        options.DefaultChallengeScheme = (string)challengeScheme;
                    }
                }).
                WithPerTenantOptions<CookieAuthenticationOptions>((options, tenantInfo) =>
                {
                    // Set a unique cookie name for this tenant.
                    options.Cookie.Name = tenantInfo.Id + "-cookie";

                    // Note the paths set take our routing strategy into account.
                    options.LoginPath = "/" + tenantInfo.Identifier + "/Home/Login";
                    options.Cookie.Path = "/" + tenantInfo.Identifier;
                });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseMultiTenant();
            app.UseAuthentication();
            app.UseMvc(ConfigRoutes);
        }

        private void ConfigRoutes(IRouteBuilder routes)
        {
            routes.MapRoute("Default", "{__tenant__=}/{controller=Home}/{action=Index}");
        }
    }
}