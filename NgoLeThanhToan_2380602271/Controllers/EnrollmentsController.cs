using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NgoLeThanhToan_2380602271.Models;

namespace NgoLeThanhToan_2380602271.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,GiangVien")]
public class EnrollmentsController : BaseApiController
{
    private readonly ApplicationDbContext _db;
    public EnrollmentsController(ApplicationDbContext db) => _db = db;

    [HttpGet("class/{classId}")]
    public async Task<IActionResult> ByClass(int classId)
    {
        var c = await _db.Classes.FindAsync(classId);
        if (c == null) return NotFound();
        if (!CanManage(c.TeacherId)) return Fail("Không có quyền", 403);

        var list = await (from e in _db.Enrollments
                          join u in _db.Users on e.StudentId equals u.Id
                          where e.ClassRoomId == classId
                          select new { e.Id, e.StudentId, u.UserName, u.FullName, u.StudentCode, u.Email })
                         .ToListAsync();
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] EnrollRequest m)
    {
        var c = await _db.Classes.FindAsync(m.ClassRoomId);
        if (c == null) return NotFound();
        if (!CanManage(c.TeacherId)) return Fail("Không có quyền", 403);

        var u = await _db.Users.FirstOrDefaultAsync(x => x.UserName == m.Student || x.StudentCode == m.Student);
        if (u == null) return Fail("Không tìm thấy sinh viên");
        if (await _db.Enrollments.AnyAsync(e => e.ClassRoomId == c.Id && e.StudentId == u.Id))
            return Fail("Sinh viên đã có trong lớp");

        _db.Enrollments.Add(new Enrollment { ClassRoomId = c.Id, StudentId = u.Id });
        await _db.SaveChangesAsync();
        return Ok(new { status = true, message = "Đã thêm sinh viên vào lớp" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(int id)
    {
        var e = await _db.Enrollments.Include(x => x.ClassRoom).FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return NotFound();
        if (!CanManage(e.ClassRoom!.TeacherId)) return Fail("Không có quyền", 403);
        _db.Enrollments.Remove(e);
        await _db.SaveChangesAsync();
        return Ok(new { status = true, message = "Đã xóa khỏi lớp" });
    }
}