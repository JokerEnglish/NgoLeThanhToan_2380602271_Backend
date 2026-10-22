using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NgoLeThanhToan_2380602271.Models;

namespace NgoLeThanhToan_2380602271.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClassesController : BaseApiController
{
    private readonly ApplicationDbContext _db;
    public ClassesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        IQueryable<ClassRoom> q = _db.Classes;
        if (User.IsInRole("GiangVien")) q = q.Where(c => c.TeacherId == Uid);
        else if (User.IsInRole("SinhVien"))
            q = q.Where(c => _db.Enrollments.Any(e => e.ClassRoomId == c.Id && e.StudentId == Uid));
        return Ok(await q.OrderBy(c => c.Code).ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var c = await _db.Classes.FindAsync(id);
        return c == null ? NotFound() : Ok(c);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,GiangVien")]
    public async Task<IActionResult> Create([FromBody] ClassRoom m)
    {
        m.Id = 0;
        if (User.IsInRole("GiangVien")) m.TeacherId = Uid;
        if (await _db.Classes.AnyAsync(c => c.Code == m.Code))
            return Fail("Mã lớp đã tồn tại");
        _db.Classes.Add(m);
        await _db.SaveChangesAsync();
        return Ok(m);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,GiangVien")]
    public async Task<IActionResult> Update(int id, [FromBody] ClassRoom m)
    {
        var c = await _db.Classes.FindAsync(id);
        if (c == null) return NotFound();
        if (!CanManage(c.TeacherId)) return Fail("Không có quyền", 403);

        c.Code = m.Code; c.Name = m.Name; c.Room = m.Room;
        c.Latitude = m.Latitude; c.Longitude = m.Longitude; c.RadiusMeters = m.RadiusMeters;
        if (IsAdmin && !string.IsNullOrEmpty(m.TeacherId)) c.TeacherId = m.TeacherId;
        await _db.SaveChangesAsync();
        return Ok(c);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,GiangVien")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Classes.FindAsync(id);
        if (c == null) return NotFound();
        if (!CanManage(c.TeacherId)) return Fail("Không có quyền", 403);
        _db.Classes.Remove(c);
        await _db.SaveChangesAsync();
        return Ok(new { status = true, message = "Đã xóa lớp" });
    }
}