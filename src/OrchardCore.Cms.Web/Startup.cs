using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace OrchardCore.Cms.Web
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services){
            services.AddOrchardCms();
            //配置跨域处理，允许所有来源：
            services.AddCors(options =>
            options.AddPolicy("JZSoftCros",
            p => p.AllowAnyOrigin())
            );

            #region 跨域
            //允许一个或多个具体来源:
            //services.AddCors(options =>
            //{


            //    Policy 名稱 CorsPolicy 是自訂的，可以自己改
            //    options.AddPolicy("跨域规则的名称", policy =>
            //    {
            //        // 設定允許跨域的來源，有多個的話可以用 `,` 隔開
            //        policy.WithOrigins("http://localhost:52652", "http://127.0.0.1")
            //                .AllowAnyHeader()
            //                .AllowAnyMethod()
            //                .AllowCredentials();
            //    });
            //});
            #endregion
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCors("JZSoftCros");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseOrchardCore();

        }
    }
}