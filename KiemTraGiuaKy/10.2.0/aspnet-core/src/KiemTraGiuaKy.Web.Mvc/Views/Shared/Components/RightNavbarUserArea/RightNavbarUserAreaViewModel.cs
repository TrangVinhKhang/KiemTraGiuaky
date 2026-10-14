using KiemTraGiuaKy.Sessions.Dto;

namespace KiemTraGiuaKy.Web.Views.Shared.Components.RightNavbarUserArea;

public class RightNavbarUserAreaViewModel
{
    public GetCurrentLoginInformationsOutput LoginInformations { get; set; }

    public bool IsMultiTenancyEnabled { get; set; }

    public string GetShownLoginName()
    {
        if (LoginInformations?.User == null)
        {
            return "Giảng viên";
        }

        var userName = LoginInformations.User.UserName;

        if (!IsMultiTenancyEnabled)
        {
            return userName;
        }

        return LoginInformations.Tenant == null
            ? ".\\" + userName
            : LoginInformations.Tenant.TenancyName + "\\" + userName;
    }
}

