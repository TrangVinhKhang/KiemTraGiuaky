using Microsoft.AspNetCore.Authorization;
using KiemTraGiuaKy.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace KiemTraGiuaKy.Web.Controllers;

[AllowAnonymous]
public class HomeController : KiemTraGiuaKyControllerBase
{
    public ActionResult Index()
    {
        return RedirectToAction("Index", "Exams");
    }
}
