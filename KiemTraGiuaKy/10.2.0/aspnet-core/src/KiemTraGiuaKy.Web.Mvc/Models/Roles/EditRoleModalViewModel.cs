using Abp.AutoMapper;
using KiemTraGiuaKy.Roles.Dto;
using KiemTraGiuaKy.Web.Models.Common;

namespace KiemTraGiuaKy.Web.Models.Roles;

[AutoMapFrom(typeof(GetRoleForEditOutput))]
public class EditRoleModalViewModel : GetRoleForEditOutput, IPermissionsEditViewModel
{
    public bool HasPermission(FlatPermissionDto permission)
    {
        return GrantedPermissionNames.Contains(permission.Name);
    }
}
