namespace Abril_Backend.Shared.Helpers
{
    public static class DateTimeExtensions
    {
        public static DateTime AsUtc(this DateTime dt) => DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        public static DateTime? AsUtc(this DateTime? dt) => dt.HasValue ? DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc) : null;
    }
}
