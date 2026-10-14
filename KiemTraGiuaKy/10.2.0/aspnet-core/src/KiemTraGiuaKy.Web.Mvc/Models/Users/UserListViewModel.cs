using KiemTraGiuaKy.Roles.Dto;
using System.Collections.Generic;

namespace KiemTraGiuaKy.Web.Models.Users;

public class UserListViewModel
{
    public IReadOnlyList<RoleDto> Roles { get; set; }
}
