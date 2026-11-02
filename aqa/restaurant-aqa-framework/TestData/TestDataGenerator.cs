namespace RestaurantAqaFramework.TestData;

public static class TestDataGenerator
{
    public static string GenerateReservationId() => $"res-{Guid.NewGuid():N}"[..18];
    public static string GenerateGroupId() => $"group-{Guid.NewGuid():N}"[..18];
    public static string GenerateSessionId(string prefix = "session") => $"{prefix}-{Guid.NewGuid():N}"[..16];
    public static string GenerateEmail(string prefix = "user") => $"{prefix}_{Guid.NewGuid():N}@test.com";
    public static string GenerateName(string role = "Guest") => $"{role}_{Guid.NewGuid().ToString()[..6]}";

    public static DateTime GenerateReservationDate(int daysInFuture = 2) =>
        DateTime.UtcNow.Date.AddDays(daysInFuture).AddHours(18); // 18:00
}

