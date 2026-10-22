using System.Security.Cryptography;
using System.Text;

namespace NgoLeThanhToan_2380602271.Services;

public static class QrToken
{
    public const int StepSeconds = 30;

    public static long CurrentWindow() => DateTimeOffset.UtcNow.ToUnixTimeSeconds() / StepSeconds;

    public static string Make(string secret, int sessionId, long window)
    {
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var bytes = h.ComputeHash(Encoding.UTF8.GetBytes($"{sessionId}:{window}"));
        return Convert.ToHexString(bytes)[..16];
    }

    // chấp nhận mã của khung 30s hiện tại và khung trước (tránh trễ mạng)
    public static bool Verify(string secret, int sessionId, string token)
    {
        token = (token ?? "").Trim().ToUpperInvariant();
        var w = CurrentWindow();
        return Make(secret, sessionId, w) == token || Make(secret, sessionId, w - 1) == token;
    }
}

public static class GeoHelper
{
    public static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        double dLat = (lat2 - lat1) * Math.PI / 180, dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}