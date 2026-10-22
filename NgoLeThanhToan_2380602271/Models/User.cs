using Microsoft.AspNetCore.Identity;

namespace NgoLeThanhToan_2380602271.Models;

public class User : IdentityUser
{
    public string? FullName { get; set; }
    public string? StudentCode { get; set; }
    public string? OtpHash { get; set; }
    public DateTime? OtpExpiry { get; set; }
    public int OtpAttempts { get; set; }
}