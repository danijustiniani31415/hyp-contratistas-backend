namespace Abril_Backend.Features.PersonasModule.Application.Dtos
{
    /// <summary>Una persona con vínculo vigente a la que le falta al menos un dato requerido para planilla.</summary>
    public class PersonaDatoFaltanteDto
    {
        public int PersonaId { get; set; }
        public string NombreCompleto { get; set; } = null!;
        public string? CargoNombre { get; set; }
        public string? ProyectoNombre { get; set; }
        /// <summary>Nombres legibles de lo que falta: "Categoría laboral", "Sueldo/Jornal", "Banco", "Tareo del mes actual", etc.</summary>
        public List<string> CamposFaltantes { get; set; } = new();
    }

    public class DashboardPlanillaDto
    {
        public int TotalPersonasActivas { get; set; }
        public int TotalConDatosCompletos { get; set; }
        public int TotalConDatosFaltantes { get; set; }
        public List<PersonaDatoFaltanteDto> Faltantes { get; set; } = new();
    }

    public class ReniecPersonaDto
    {
        public string Nombres { get; set; } = null!;
        public string Apellidos { get; set; } = null!;
    }

    public class CargoDetailDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public bool Activo { get; set; }
    }

    public class CargoCreateDto
    {
        public string Nombre { get; set; } = null!;
    }

    public class CargoUpdateDto
    {
        public string Nombre { get; set; } = null!;
        public bool Activo { get; set; } = true;
    }

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

    /// <summary>Fase 1 del motor de Planillas — datos fijos, todos opcionales (se completan cuando
    /// se tenga la info; nada de esto bloquea el alta de la persona).</summary>
    public class PersonaPlanillaDto
    {
        /// <summary>Correlativo autogenerado (H&P-E001...) — nunca se recibe del cliente, se ignora si llega.</summary>
        public string? CodigoTrabajador { get; set; }
        public string? Banco { get; set; }
        public string? NumeroCuenta { get; set; }
        public string? Cusp { get; set; }
        public string? TipoAfpOnp { get; set; }
        /// <summary>OBRERO o EMPLEADO.</summary>
        public string? CategoriaLaboral { get; set; }
        public decimal? SueldoBase { get; set; }
        public decimal? Jornal { get; set; }
        public bool AsignacionFamiliar { get; set; }
        public bool Sctr { get; set; }
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
        /// <summary>Sede/proyecto donde trabaja (Central Lima, Las Bravas, ...).</summary>
        public int? ProyectoId { get; set; }
        public DateOnly FechaInicio { get; set; }

        public PersonaPlanillaDto? Planilla { get; set; }
    }

    /// <summary>Edición de los datos básicos de una persona ya creada — nombres, documento y
    /// contacto. El vínculo laboral (cargo, proyecto, tipo) se edita aparte vía NuevoVinculoDto,
    /// no acá: cambiar de cargo/proyecto es un evento con su propia fecha, no una corrección de dato.</summary>
    public class PersonaUpdateDto
    {
        public string Nombres { get; set; } = null!;
        public string Apellidos { get; set; } = null!;
        public string TipoDocumento { get; set; } = "DNI";
        public string NumeroDocumento { get; set; } = null!;
        public string? Telefono { get; set; }
        public string? EmailPersonal { get; set; }
    }

    public class VinculoLaboralDto
    {
        public int Id { get; set; }
        public string TipoVinculoNombre { get; set; } = null!;
        public string? EmpresaContratistaNombre { get; set; }
        public string? CargoNombre { get; set; }
        public string? ProyectoNombre { get; set; }
        public DateOnly FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }
        public string Estado { get; set; } = null!;
        public string? MotivoCese { get; set; }
    }

    /// <summary>Fila de la tabla de accesos del detalle de persona — a diferencia de LbAsignacionDto
    /// (pensado para claims del JWT), esta trae nombres legibles + el Id para poder revocar.</summary>
    public class AsignacionDetalleDto
    {
        public long Id { get; set; }
        public string RolNombre { get; set; } = null!;
        public bool EsGlobal { get; set; }
        public string? ProyectoNombre { get; set; }
        public string? AlmacenNombre { get; set; }
        public DateOnly FechaInicio { get; set; }
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
        public PersonaPlanillaDto? Planilla { get; set; }
        public List<VinculoLaboralDto> Vinculos { get; set; } = new();
        public long? UsuarioSistemaId { get; set; }
        public string? EmailLogin { get; set; }
        public List<AsignacionDetalleDto> Asignaciones { get; set; } = new();
    }

    /// <summary>Cierra el vínculo vigente (si hay) y abre uno nuevo — nunca se pisa el anterior.</summary>
    public class NuevoVinculoDto
    {
        public short TipoVinculoId { get; set; }
        public int? EmpresaContratistaId { get; set; }
        public int? CargoId { get; set; }
        public int? ProyectoId { get; set; }
        public DateOnly FechaInicio { get; set; }
        public string? MotivoCeseAnterior { get; set; }
    }

    /// <summary>
    /// Da acceso al sistema a una persona (o lo reactiva si ya tenía uno inactivo). Sin
    /// contraseña a propósito — mismo patrón de "invitación" que Odoo/Google Workspace/GitHub:
    /// el admin nunca escribe ni conoce la contraseña de otra persona. El sistema genera una
    /// interna inutilizable y manda un correo de activación con enlace de un solo uso (mismo
    /// mecanismo que "olvidé mi contraseña", ver LbAuthService).
    /// </summary>
    public class CrearUsuarioDto
    {
        public string EmailLogin { get; set; } = null!;
    }

    /// <summary>
    /// Cambia el correo de login de un usuario ya activo. Se notifica al correo ANTERIOR (no al
    /// nuevo) como aviso de seguridad — si alguien no autorizado cambió el correo, la persona
    /// dueña de la cuenta original se entera ahí, no en la bandeja nueva que quizás no controla.
    /// </summary>
    public class CambiarEmailDto
    {
        public string NuevoEmail { get; set; } = null!;
    }

    public class CatalogoItemDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
    }

    /// <summary>Rol con su bandera de alcance — el front la usa para exigir Proyecto solo cuando el rol no es global.</summary>
    public class RolCatalogoItemDto : CatalogoItemDto
    {
        public bool EsGlobal { get; set; }
    }

    /// <summary>Tipo de vínculo con su código — el front lo usa para mostrar "Empresa contratista"
    /// solo cuando el código es CONTRATISTA (Planilla/Locador/Practicante son siempre H&P directo).</summary>
    public class TipoVinculoCatalogoItemDto : CatalogoItemDto
    {
        public string Codigo { get; set; } = null!;
    }

    /// <summary>Almacén con su proyecto (null = almacén CENTRAL) — el front filtra por el proyecto elegido en la asignación.</summary>
    public class AlmacenCatalogoItemDto : CatalogoItemDto
    {
        public int? ProyectoId { get; set; }
    }

    public class CatalogosPersonasDto
    {
        public List<TipoVinculoCatalogoItemDto> TiposVinculo { get; set; } = new();
        public List<CatalogoItemDto> Cargos { get; set; } = new();
        public List<CatalogoItemDto> EmpresasContratistas { get; set; } = new();
        public List<RolCatalogoItemDto> Roles { get; set; } = new();
        public List<CatalogoItemDto> Proyectos { get; set; } = new();
        public List<AlmacenCatalogoItemDto> Almacenes { get; set; } = new();
    }

    public class NuevaAsignacionDto
    {
        public short RolId { get; set; }
        public int? ProyectoId { get; set; }
        public int? AlmacenId { get; set; }
    }
}
