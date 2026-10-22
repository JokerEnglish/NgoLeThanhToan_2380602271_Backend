using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NgoLeThanhToan_2380602271.Models;
using NgoLeThanhToan_2380602271.Services;

namespace NgoLeThanhToan_2380602271.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : BaseApiController
{
    private static readonly string[] Statuses = { "Present", "Late", "Absent" };
    private static readonly string[] Methods = { "QR", "Face", "Fingerprint", "NFC" };

    private readonly ApplicationDbContext _db;
    public AttendanceController(ApplicationDbContext db) => _db = db;

    // Sinh viên điểm danh: QR động + GPS (+ khuôn mặt/vân tay ở app)
    [HttpPost("checkin")]
    [Authorize(Roles = "SinhVien")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest m)
    {
        var s = await _db.Sessions.Include(x => x.ClassRoom).FirstOrDefaultAsync(x => x.Id == m.SessionId);
        if (s == null) return Fail("Buổi học không tồn tại");
        if (!s.IsOpen) return Fail("Buổi học đã đóng điểm danh");
        if (!Methods.Contains(m.Method)) m.Method = "QR";

        if (!await _db.Enrollments.AnyAsync(e => e.ClassRoomId == s.ClassRoomId && e.StudentId == Uid))
            return Fail("Bạn không thuộc lớp này", 403);

        if (!QrToken.Verify(s.QrSecret, s.Id, m.Token))
            return Fail("Mã QR không hợp lệ hoặc đã hết hạn");

        var cls = s.ClassRoom!;
        if (cls.Latitude != null && cls.Longitude != null)
        {
            if (m.Latitude == null || m.Longitude == null)
                return Fail("Cần bật vị trí để điểm danh");
            var d = GeoHelper.DistanceMeters(cls.Latitude.Value, cls.Longitude.Value,
                                             m.Latitude.Value, m.Longitude.Value);
            if (d > cls.RadiusMeters)
                return Fail($"Bạn cách lớp học {d:F0} m, ngoài phạm vi {cls.RadiusMeters} m");
        }

        if (await _db.Attendances.AnyAsync(a => a.SessionId == s.Id && a.StudentId == Uid))
            return Fail("Bạn đã điểm danh buổi này rồi");

        _db.Attendances.Add(new Attendance
        {
            SessionId = s.Id,
            StudentId = Uid,
            Method = m.Method,
            Latitude = m.Latitude,
            Longitude = m.Longitude,
            CheckInTime = DateTime.UtcNow,
            Status = "Present"
        });
        await _db.SaveChangesAsync();
        return Ok(new { status = true, message = "Điểm danh thành công" });
    }

    // Lịch sử điểm danh của sinh viên
    [HttpGet("my")]
    [Authorize(Roles = "SinhVien")]
    public async Task<IActionResult> My()
    {
        var list = await (from a in _db.Attendances
                          join s in _db.Sessions on a.SessionId equals s.Id
                          join c in _db.Classes on s.ClassRoomId equals c.Id
                          where a.StudentId == Uid
                          orderby a.CheckInTime descending
                          select new
                          {
                              a.Id,
                              ClassName = c.Name,
                              ClassCode = c.Code,
                              SessionTitle = s.Title,
                              a.CheckInTime,
                              a.Method,
                              a.Status
                          })
                         .ToListAsync();
        return Ok(list);
    }

    // Giảng viên xem danh sách điểm danh của một buổi (kể cả sinh viên vắng)
    [HttpGet("session/{sessionId}")]
    [Authorize(Roles = "Admin,GiangVien")]
    public async Task<IActionResult> BySession(int sessionId)
    {
        var s = await _db.Sessions.Include(x => x.ClassRoom).FirstOrDefaultAsync(x => x.Id == sessionId);
        if (s == null) return NotFound();
        if (!CanManage(s.ClassRoom!.TeacherId)) return Fail("Không có quyền", 403);

        var students = await (from e in _db.Enrollments
                              join u in _db.Users on e.StudentId equals u.Id
                              where e.ClassRoomId == s.ClassRoomId
                              select new { u.Id, u.UserName, u.FullName, u.StudentCode }).ToListAsync();
        var att = await _db.Attendances.Where(a => a.SessionId == sessionId).ToListAsync();

        var result = students.Select(st =>
        {
            var a = att.FirstOrDefault(x => x.StudentId == st.Id);
            return new
            {
                AttendanceId = a?.Id,
                st.UserName,
                st.FullName,
                st.StudentCode,
                Status = a?.Status ?? "Absent",
                CheckInTime = a?.CheckInTime,
                Method = a?.Method
            };
        });
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,GiangVien")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest m)
    {
        if (!Statuses.Contains(m.Status)) return Fail("Trạng thái không hợp lệ");
        var a = await _db.Attendances.Include(x => x.Session!.ClassRoom).FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        if (!CanManage(a.Session!.ClassRoom!.TeacherId)) return Fail("Không có quyền", 403);
        a.Status = m.Status;
        await _db.SaveChangesAsync();
        return Ok(new { status = true, message = "Đã cập nhật" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,GiangVien")]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await _db.Attendances.Include(x => x.Session!.ClassRoom).FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        if (!CanManage(a.Session!.ClassRoom!.TeacherId)) return Fail("Không có quyền", 403);
        _db.Attendances.Remove(a);
        await _db.SaveChangesAsync();
        return Ok(new { status = true, message = "Đã xóa" });
    }
}