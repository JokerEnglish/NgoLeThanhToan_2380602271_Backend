using System.ComponentModel.DataAnnotations;

namespace NgoLeThanhToan_2380602271.Models;

public class RegisterRequest
{
    [Required] public string Username { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(6)] public string Password { get; set; } = "";
    public string? FullName { get; set; }
    public string? StudentCode { get; set; }
    public string? Role { get; set; }
}

public class LoginRequest
{
    [Required] public string Username { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class VerifyOtpRequest
{
    [Required] public string Username { get; set; } = "";
    [Required] public string Otp { get; set; } = "";
}