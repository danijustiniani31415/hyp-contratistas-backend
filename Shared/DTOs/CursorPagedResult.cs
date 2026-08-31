namespace Abril_Backend.Shared.DTOs
{
    public class CursorPagedResult<T>
    {
        public List<T> Data { get; set; } = [];
        public int Total { get; set; }
        public string? NextCursor { get; set; }
        public bool HasMore { get; set; }
    }
}
