namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Catalogos
{
    public class AreaSimpleDto
    {
        public string Area { get; set; } = string.Empty;
    }

    public class CatSubareaDto
    {
        public int Id { get; set; }
        public string Subarea { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string? Jefatura { get; set; }
    }
}
