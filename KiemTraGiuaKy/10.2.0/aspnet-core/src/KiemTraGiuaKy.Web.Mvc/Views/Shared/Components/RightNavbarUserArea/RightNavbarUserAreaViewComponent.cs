using Abp.Configuration.Startup;
using KiemTraGiuaKy.Sessions;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KiemTraGiuaKy.Web.Views.Shared.Components.RightNavbarUserArea;

public class RightNavbarUserAreaViewComponent : KiemTraGiuaKyViewComponent
{
    private readonly ISessionAppService _sessionAppService;
    private readonly IMultiTenancyConfig _multiTenancyConfig;

    public RightNavbarUserAreaViewComponent(
        ISessionAppService sessionAppService,
        IMultiTenancyConfig multiTenancyConfig)
    {
        _sessionAppService = sessionAppService;
        _multiTenancyConfig = multiTenancyConfig;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new RightNavbarUserAreaViewModel
        {
            LoginInformations = await _sessionAppService.GetCurrentLoginInformations(),
            IsMultiTenancyEnabled = _multiTenancyConfig.IsEnabled,
        };

        return View(model);
    }
}

