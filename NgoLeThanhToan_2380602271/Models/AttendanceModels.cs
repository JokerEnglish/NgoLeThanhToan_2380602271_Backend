using System.Text.Json.Serialization;

namespace NgoLeThanhToan_2380602271.Models;

public class ClassRoom
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Room { get; set; }
    public string? TeacherId { get; set; }
    public double? Latitude { get; set; }      // để trống = không kiểm tra GPS
    public double? Longitude { get; set; }
    public int RadiusMeters { get; set; } = 100;
}

public class Enrollment
{
    public int Id { get; set; }
    public int ClassRoomId { get; set; }
    public ClassRoom? ClassRoom { get; set; }
    public string StudentId { get; set; } = "";
}

public class ClassSession
{
    public int Id { get; set; }
    public int ClassRoomId { get; set; }
    public ClassRoom? ClassRoom { get; set; }
    public string Title { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsOpen { get; set; } = true;
    [JsonIgnore] public string QrSecret { get; set; } = Guid.NewGuid().ToString("N");
}

public class Attendance
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public ClassSession? Session { get; set; }
    public string StudentId { get; set; } = "";
    public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
    public string Method { get; set; } = "QR";   // QR, Face, Fingerprint, NFC
    public string Status { get; set; } = "Present"; // Present, Late, Absent
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class SessionRequest
{
    public int ClassRoomId { get; set; }
    public string Title { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsOpen { get; set; } = true;
}

public class EnrollRequest
{
    public int ClassRoomId { get; set; }
    public string Student { get; set; } = "";   // username hoặc mã số sinh viên
}

public class CheckInRequest
{
    public int SessionId { get; set; }
    public string Token { get; set; } = "";
    public string Method { get; set; } = "QR";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class UpdateStatusRequest { public string Status { get; set; } = "Present"; }

public class UpdateUserRequest
{
    public string? FullName { get; set; }
    public string? StudentCode { get; set; }
    public string? Email { get; set; }
}