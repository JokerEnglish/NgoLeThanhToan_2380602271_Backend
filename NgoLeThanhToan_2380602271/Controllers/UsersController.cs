using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NgoLeThanhToan_2380602271.Models;

namespace NgoLeThanhToan_2380602271.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : BaseApiController
{
    private readonly UserManager<User> _users;
    private readonly ApplicationDbContext _db;
    public UsersController(UserManager<User> users, ApplicationDbContext db) { _users = users; _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _users.Users.OrderBy(u => u.UserName).ToListAsync();
        var result = new List<object>();
        foreach (var u in list)
            result.Add(new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.FullName,
                u.StudentCode,
                roles = await _users.GetRolesAsync(u)
            });
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest m)
    {
        var u = await _users.FindByIdAsync(id);
        if (u == null) return NotFound();
        u.FullName = m.FullName ?? u.FullName;
        u.StudentCode = m.StudentCode ?? u.StudentCode;
        if (!string.IsNullOrWhiteSpace(m.Email)) u.Email = m.Email;
        var r = await _users.UpdateAsync(u);
        return r.Succeeded ? Ok(new { status = true, message = "Đã cập nhật" })
                           : Fail(string.Join("; ", r.Errors.Select(e => e.Description)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (id == Uid) return Fail("Không thể tự xóa tài khoản của mình");
        var u = await _users.FindByIdAsync(id);
        if (u == null) return NotFound();

        _db.Attendances.RemoveRange(_db.Attendances.Where(a => a.StudentId == id));
        _db.Enrollments.RemoveRange(_db.Enrollments.Where(e => e.StudentId == id));
        await _db.SaveChangesAsync();

        var r = await _users.DeleteAsync(u);
        return r.Succeeded ? Ok(new { status = true, message = "Đã xóa người dùng" })
                           : Fail(string.Join("; ", r.Errors.Select(e => e.Description)));
    }
}