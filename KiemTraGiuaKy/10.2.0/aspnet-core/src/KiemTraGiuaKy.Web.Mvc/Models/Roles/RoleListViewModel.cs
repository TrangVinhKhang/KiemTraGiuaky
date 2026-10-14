using KiemTraGiuaKy.Roles.Dto;
using System.Collections.Generic;

namespace KiemTraGiuaKy.Web.Models.Roles;

public class RoleListViewModel
{
    public IReadOnlyList<PermissionDto> Permissions { get; set; }
}
