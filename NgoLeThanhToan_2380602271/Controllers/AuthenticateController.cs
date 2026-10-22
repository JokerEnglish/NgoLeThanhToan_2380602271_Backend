using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NgoLeThanhToan_2380602271.Models;
using NgoLeThanhToan_2380602271.Services;

namespace NgoLeThanhToan_2380602271.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticateController : ControllerBase
{
    private static readonly string[] PublicRoles = { "SinhVien", "GiangVien" };

    private readonly UserManager<User> _users;
    private readonly IConfiguration _cfg;
    private readonly IEmailSender _email;
    private readonly ILogger<AuthenticateController> _log;

    public AuthenticateController(UserManager<User> users, IConfiguration cfg,
        IEmailSender email, ILogger<AuthenticateController> log)
    {
        _users = users; _cfg = cfg; _email = email; _log = log;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest m)
    {
        var role = string.IsNullOrWhiteSpace(m.Role) ? "SinhVien" : m.Role;
        if (!PublicRoles.Contains(role))
            return BadRequest(new { status = false, message = "Role không hợp lệ" });

        if (await _users.FindByNameAsync(m.Username) != null)
            return BadRequest(new { status = false, message = "Tên đăng nhập đã tồn tại" });
        if (await _users.FindByEmailAsync(m.Email) != null)
            return BadRequest(new { status = false, message = "Email đã được sử dụng" });

        var user = new User
        {
            UserName = m.Username,
            Email = m.Email,
            FullName = m.FullName,
            StudentCode = m.StudentCode
        };
        var result = await _users.CreateAsync(user, m.Password);
        if (!result.Succeeded)
            return BadRequest(new { status = false, message = string.Join("; ", result.Errors.Select(e => e.Description)) });

        await _users.AddToRoleAsync(user, role);
        return Ok(new { status = true, message = "Đăng ký thành công" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest m)
    {
        var user = await _users.FindByNameAsync(m.Username);
        if (user == null || !await _users.CheckPasswordAsync(user, m.Password))
            return Unauthorized(new { status = false, message = "Sai tên đăng nhập hoặc mật khẩu" });

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        user.OtpHash = HashOtp(code, user.Id);
        user.OtpExpiry = DateTime.UtcNow.AddMinutes(5);
        user.OtpAttempts = 0;
        await _users.UpdateAsync(user);

        bool showOtp = _cfg.GetValue<bool>("ShowOtpInResponse");
        try
        {
            await _email.SendAsync(user.Email!, "Mã OTP đăng nhập",
                $"Mã OTP của bạn là {code}. Có hiệu lực trong 5 phút.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Gửi email OTP thất bại");
            if (!showOtp)
                return StatusCode(500, new { status = false, message = "Không gửi được email OTP" });
        }

        return Ok(new
        {
            status = true,
            requiresOtp = true,
            message = "Đã gửi mã OTP tới email của bạn",
            devOtp = showOtp ? code : null
        });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest m)
    {
        var user = await _users.FindByNameAsync(m.Username);
        if (user == null || user.OtpHash == null || user.OtpExpiry == null || user.OtpExpiry < DateTime.UtcNow)
            return BadRequest(new { status = false, message = "OTP không tồn tại hoặc đã hết hạn" });

        if (user.OtpAttempts >= 5)
            return BadRequest(new { status = false, message = "Nhập sai quá nhiều lần, hãy đăng nhập lại" });

        if (HashOtp(m.Otp, user.Id) != user.OtpHash)
        {
            user.OtpAttempts++;
            await _users.UpdateAsync(user);
            return Unauthorized(new { status = false, message = "OTP không đúng" });
        }

        user.OtpHash = null; user.OtpExpiry = null; user.OtpAttempts = 0;
        await _users.UpdateAsync(user);

        var token = await CreateTokenAsync(user);
        return Ok(new { status = true, message = "Đăng nhập thành công", token });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _users.FindByNameAsync(User.Identity!.Name!);
        if (user == null) return NotFound();
        var roles = await _users.GetRolesAsync(user);
        return Ok(new { user.UserName, user.Email, user.FullName, user.StudentCode, roles });
    }

    private async Task<string> CreateTokenAsync(User user)
    {
        var roles = await _users.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var jwt = _cfg.GetSection("JWTKey");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"]!));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(Convert.ToDouble(jwt["TokenExpiryTimeInHour"])),
            Issuer = jwt["ValidIssuer"],
            Audience = jwt["ValidAudience"],
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };
        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private static string HashOtp(string code, string salt) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code + salt)));
}