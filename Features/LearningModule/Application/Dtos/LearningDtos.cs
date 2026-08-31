namespace Abril_Backend.Features.LearningModule.Application.Dtos
{
    // ─────────────────────────── Display (login / inicio) ───────────────────────────

    /// <summary>Video-guía tal como lo consume el frontend para mostrarlo.</summary>
    public class LearningVideoDto
    {
        public string Titulo { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Img { get; set; }
    }

    /// <summary>Grupo/área con sus videos, para renderizar agrupado.</summary>
    public class LearningCategoryDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? AccentColor { get; set; }
        public List<LearningVideoDto> Videos { get; set; } = new();
    }

    // ─────────────────────────────── Admin (CRUD) ───────────────────────────────

    public class LearningVideoAdminDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Img { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; }
    }

    public class LearningCategoryAdminDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? AccentColor { get; set; }
        public int Orden { get; set; }
        public int SurfaceId { get; set; }
        public string SurfaceCode { get; set; } = string.Empty;
        public string SurfaceNombre { get; set; } = string.Empty;
        public bool EsPublicoInterno { get; set; }
        public bool Activo { get; set; }
        public List<int> RoleIds { get; set; } = new();
        public List<LearningVideoAdminDto> Videos { get; set; } = new();
    }

    public class LearningSurfaceDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

    public class LearningRoleOptionDto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }

    /// <summary>Todo lo que la página de administración necesita en una sola petición.</summary>
    public class LearningAdminDataDto
    {
        public List<LearningCategoryAdminDto> Categorias { get; set; } = new();
        public List<LearningSurfaceDto> Superficies { get; set; } = new();
        public List<LearningRoleOptionDto> Roles { get; set; } = new();
    }

    // ─────────────────────────── Create / Edit payloads ───────────────────────────

    public class LearningCategoryCreateDto
    {
        public string Nombre { get; set; } = string.Empty;
        public int SurfaceId { get; set; }
        public string? AccentColor { get; set; }
        public int Orden { get; set; }
        public bool EsPublicoInterno { get; set; }
        public List<int> RoleIds { get; set; } = new();
    }

    public class LearningCategoryEditDto
    {
        public string Nombre { get; set; } = string.Empty;
        public int SurfaceId { get; set; }
        public string? AccentColor { get; set; }
        public int Orden { get; set; }
        public bool EsPublicoInterno { get; set; }
        public List<int> RoleIds { get; set; } = new();
    }

    public class LearningVideoCreateDto
    {
        public int CategoriaId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Img { get; set; }
        public int Orden { get; set; }
    }

    public class LearningVideoEditDto
    {
        public string Titulo { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Img { get; set; }
        public int Orden { get; set; }
    }
}
