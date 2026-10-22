using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace NgoLeThanhToan_2380602271.Controllers;

public abstract class BaseApiController : ControllerBase
{
    protected string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    protected bool IsAdmin => User.IsInRole("Admin");
    protected bool CanManage(string? teacherId) => IsAdmin || teacherId == Uid;
    protected IActionResult Fail(string msg, int code = 400) =>
        StatusCode(code, new { status = false, message = msg });
}