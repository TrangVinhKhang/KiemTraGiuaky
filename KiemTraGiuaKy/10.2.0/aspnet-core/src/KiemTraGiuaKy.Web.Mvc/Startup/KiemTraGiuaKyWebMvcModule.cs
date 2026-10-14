using Abp.Modules;
using Abp.Reflection.Extensions;
using KiemTraGiuaKy.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace KiemTraGiuaKy.Web.Startup;

[DependsOn(typeof(KiemTraGiuaKyWebCoreModule))]
public class KiemTraGiuaKyWebMvcModule : AbpModule
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfigurationRoot _appConfiguration;

    public KiemTraGiuaKyWebMvcModule(IWebHostEnvironment env)
    {
        _env = env;
        _appConfiguration = env.GetAppConfiguration();
    }

    public override void PreInitialize()
    {
        Configuration.Navigation.Providers.Add<KiemTraGiuaKyNavigationProvider>();
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(KiemTraGiuaKyWebMvcModule).GetAssembly());
    }
}
