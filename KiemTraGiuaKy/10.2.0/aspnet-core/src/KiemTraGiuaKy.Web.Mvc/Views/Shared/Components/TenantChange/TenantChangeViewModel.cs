using Abp.AutoMapper;
using KiemTraGiuaKy.Sessions.Dto;

namespace KiemTraGiuaKy.Web.Views.Shared.Components.TenantChange;

[AutoMapFrom(typeof(GetCurrentLoginInformationsOutput))]
public class TenantChangeViewModel
{
    public TenantLoginInfoDto Tenant { get; set; }
}
