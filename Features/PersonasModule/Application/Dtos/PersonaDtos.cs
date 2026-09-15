namespace Abril_Backend.Features.PersonasModule.Application.Dtos
{
    /// <summary>Fila de la lista de personas — solo lo necesario para la tabla (patrón SAP Fiori confirmado en SISTEMA-DE-DISENO.md).</summary>
    public class PersonaListItemDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = null!;
        public string NumeroDocumento { get; set; } = null!;
        public string? TipoVinculoNombre { get; set; }
        public string? CargoNombre { get; set; }
        public string? EstadoVinculo { get; set; } // ACTIVO / CESADO / SUSPENDIDO / SIN_VINCULO
        public bool TieneUsuario { get; set; }
    }

    public class PersonaListResponseDto
    {
        public List<PersonaListItemDto> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>Alta de persona + su primer vínculo laboral en un solo paso (así se usa siempre en la práctica).</summary>
    public class PersonaCreateDto
    {
        public string Nombres { get; set; } = null!;
        public string Apellidos { get; set; } = null!;
        public string TipoDocumento { get; set; } = "DNI";
        public string NumeroDocumento { get; set; } = null!;
        public string? Telefono { get; set; }
        public string? EmailPersonal { get; set; }

        public short TipoVinculoId { get; set; }
        public int? EmpresaContratistaId { get; set; }
        public int? CargoId { get; set; }
        public DateOnly FechaInicio { get; set; }
    }

    public class VinculoLaboralDto
    {
        public int Id { get; set; }
        public string TipoVinculoNombre { get; set; } = null!;
        public string? EmpresaContratistaNombre { get; set; }
        public string? CargoNombre { get; set; }
        public DateOnly FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }
        public string Estado { get; set; } = null!;
        public string? MotivoCese { get; set; }
    }

    public class PersonaDetailDto
    {
        public int Id { get; set; }
        public string Nombres { get; set; } = null!;
        public string Apellidos { get; set; } = null!;
        public string TipoDocumento { get; set; } = null!;
        public string NumeroDocumento { get; set; } = null!;
        public string? Telefono { get; set; }
        public string? EmailPersonal { get; set; }
        public bool Activo { get; set; }
        public List<VinculoLaboralDto> Vinculos { get; set; } = new();
        public long? UsuarioSistemaId { get; set; }
        public string? EmailLogin { get; set; }
        public List<LbAsignacionDto> Asignaciones { get; set; } = new();
    }

    /// <summary>Cierra el vínculo vigente (si hay) y abre uno nuevo — nunca se pisa el anterior.</summary>
    public class NuevoVinculoDto
    {
        public short TipoVinculoId { get; set; }
        public int? EmpresaContratistaId { get; set; }
        public int? CargoId { get; set; }
        public DateOnly FechaInicio { get; set; }
        public string? MotivoCeseAnterior { get; set; }
    }

    /// <summary>Crea el usuario_sistema (o lo reactiva si ya existía y estaba inactivo).</summary>
    public class CrearUsuarioDto
    {
        public string EmailLogin { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class CatalogoItemDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
    }

    public class CatalogosPersonasDto
    {
        public List<CatalogoItemDto> TiposVinculo { get; set; } = new();
        public List<CatalogoItemDto> Cargos { get; set; } = new();
        public List<CatalogoItemDto> EmpresasContratistas { get; set; } = new();
        public List<CatalogoItemDto> Roles { get; set; } = new();
        public List<CatalogoItemDto> Proyectos { get; set; } = new();
    }

    public class NuevaAsignacionDto
    {
        public short RolId { get; set; }
        public int? ProyectoId { get; set; }
        public int? AlmacenId { get; set; }
    }
}
