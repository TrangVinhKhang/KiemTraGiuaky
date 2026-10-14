using Abp.AspNetCore.Mvc.ViewComponents;

namespace KiemTraGiuaKy.Web.Views;

public abstract class KiemTraGiuaKyViewComponent : AbpViewComponent
{
    protected KiemTraGiuaKyViewComponent()
    {
        LocalizationSourceName = KiemTraGiuaKyConsts.LocalizationSourceName;
    }
}
