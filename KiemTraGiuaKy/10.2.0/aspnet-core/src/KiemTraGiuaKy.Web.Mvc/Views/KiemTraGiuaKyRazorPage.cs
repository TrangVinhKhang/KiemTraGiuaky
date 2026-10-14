using Abp.AspNetCore.Mvc.Views;
using Abp.Runtime.Session;
using Microsoft.AspNetCore.Mvc.Razor.Internal;

namespace KiemTraGiuaKy.Web.Views;

public abstract class KiemTraGiuaKyRazorPage<TModel> : AbpRazorPage<TModel>
{
    [RazorInject]
    public IAbpSession AbpSession { get; set; }

    protected KiemTraGiuaKyRazorPage()
    {
        LocalizationSourceName = KiemTraGiuaKyConsts.LocalizationSourceName;
    }
}
