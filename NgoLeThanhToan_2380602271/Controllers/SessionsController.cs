using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NgoLeThanhToan_2380602271.Models;
using NgoLeThanhToan_2380602271.Services;

namespace NgoLeThanhToan_2380602271.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionsController : BaseApiController
{
    private readonly ApplicationDbContext _db;
    public SessionsController(ApplicationDbContext db) => _db = db;

    [HttpGet("class/{classId}")]
    public async Task<IActionResult> ByClass(int classId)
    {
        var c = await _db.Classes.FindAsync(classId);
        if (c == null) return NotFound();

        if (User.IsInRole("SinhVien"))
        {
            if (!await _db.Enrollments.AnyAsync(e => e.ClassRoomId == classId && e.StudentId == Uid))
                return Fail("Bạn không thuộc lớp này", 403);
        }
        else if (!CanManage(c.TeacherId)) return Fail("Không có quyền", 403);

        return Ok(await _db.Sessions.Where(s => s.ClassRoomId == classId)
                    .OrderByDescending(s => s.StartTime).ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,GiangVien")]
    public async Task<IActionResult> Create([FromBody] SessionRequest m)
    {
        var c = await _db.Classes.FindAsync(m.ClassRoomId);
        if (c == null) return Fail("Lớp không tồn tại");
        if (!CanManage(c.TeacherId)) return Fail("Không có quyền", 403);

        var s = new ClassSession
        {
            ClassRoomId = m.ClassRoomId,
            Title = m.Title,
            StartTime = m.StartTime,
            EndTime = m.EndTime,
            IsOpen = m.IsOpen
        };
        _db.Sessions.Add(s);
        await _db.SaveChangesAsync();
        return Ok(s);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,GiangVien")]
    public async Task<IActionResult> Update(int id, [FromBody] SessionRequest m)
    {
        var s = await _db.Sessions.Include(x => x.ClassRoom).FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();
        if (!CanManage(s.ClassRoom!.TeacherId)) return Fail("Không có quyền", 403);

        s.Title = m.Title; s.StartTime = m.StartTime; s.EndTime = m.EndTime; s.IsOpen = m.IsOpen;
        await _db.SaveChangesAsync();
        return Ok(s);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,GiangVien")]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _db.Sessions.Include(x => x.ClassRoom).FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();
        if (!CanManage(s.ClassRoom!.TeacherId)) return Fail("Không có quyền", 403);
        _db.Sessions.Remove(s);
        await _db.SaveChangesAsync();
        return Ok(new { status = true, message = "Đã xóa buổi học" });
    }

    // Giảng viên gọi mỗi ~25 giây để làm mới mã QR
    [HttpGet("{id}/qr")]
    [Authorize(Roles = "Admin,GiangVien")]
    public async Task<IActionResult> Qr(int id)
    {
        var s = await _db.Sessions.Include(x => x.ClassRoom).FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();
        if (!CanManage(s.ClassRoom!.TeacherId)) return Fail("Không có quyền", 403);

        var w = QrToken.CurrentWindow();
        var token = QrToken.Make(s.QrSecret, s.Id, w);
        var secondsLeft = (int)((w + 1) * QrToken.StepSeconds - DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        return Ok(new { sessionId = s.Id, token, payload = $"{s.Id}:{token}", secondsLeft });
    }
}