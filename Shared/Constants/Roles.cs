namespace Abril_Backend.Shared.Constants
{
    /// <summary>
    /// IDs de los roles de la tabla <c>role</c> (producción), expresados como string
    /// porque así viajan en el claim del JWT y así los consumen <c>[Authorize(Roles = ...)]</c>
    /// y <c>User.IsInRole(...)</c>.
    ///
    /// Se usan IDs y NO nombres a propósito: el nombre de un rol puede editarse desde el
    /// CRUD de roles; si la autorización dependiera del nombre, ese cambio la rompería en
    /// silencio. El ID es estable.
    ///
    /// El JWT emite el ID en <c>ClaimTypes.Role</c> (ver JWTService) — por eso estas
    /// constantes son los IDs. Para mostrar el nombre del rol existe el claim aparte
    /// <c>role_name</c>.
    ///
    /// Espejo del archivo del frontend <c>src/app/core/constants/roles.ts</c>: mantener
    /// ambos alineados.
    ///
    /// Nota: CONTRATISTA (11) y CLINICA (14) también se usan como discriminador de tipo de
    /// sesión vía el claim <c>tipo</c>; para "¿es contratista/clínica?" preferir ese claim.
    /// </summary>
    public static class Roles
    {
        public const string AdministradorSistema              = "1";  // ADMINISTRADOR DEL SISTEMA
        public const string AdministradorUdp                  = "2";  // ADMINISTRADOR DE UDP
        public const string UsuarioUdp                        = "3";  // USUARIO DE UDP
        public const string AdministradorResidentes           = "4";  // ADMINISTRADOR DE RESIDENTES
        public const string Residente                         = "5";  // RESIDENTE
        public const string CostosOficinaCentral              = "6";  // USUARIO DE COSTOS Y PRESUPUESTOS DE OFICINA CENTRAL
        public const string CostosAdministrador               = "7";  // ADMINISTRADOR DE COSTOS Y PRESUPUESTOS
        public const string UsuarioArquitecturaComercial      = "8";  // USUARIO DE ARQUITECTURA COMERCIAL
        public const string AdministradorSsoma                = "9";  // JEFE SSOMA (antes ADMINISTRADOR SSOMA)
        public const string AdministradorAdministracion       = "10"; // ADMINISTRADOR ADMINISTRACION
        public const string Contratista                       = "11"; // CONTRATISTA
        public const string UsuarioDeAbril                    = "12"; // USUARIO DE ABRIL
        public const string Clinica                           = "14"; // CLINICA
        // 15 (ADMINISTRADOR DE GESTIÓN ADMINISTRATIVA) eliminado el 2026-07-14; lo reemplaza el 76.
        public const string ContabilidadFirmante              = "16"; // USUARIO FIRMANTE DE FACTURAS DE CONTABILIDAD
        public const string AdministradorMejoraContinua       = "48"; // ADMINISTRADOR DE MEJORA CONTINUA
        public const string ServicioVigilancia                = "49"; // SERVICIO DE VIGILANCIA
        public const string GestorArquitecturaComercial       = "51"; // GESTOR DE ARQUITECTURA COMERCIAL
        public const string UsuarioRecepcion                  = "52"; // USUARIO DE RECEPCIÓN
        public const string SaludOcupacional                  = "53"; // MÉDICO OCUPACIONAL (antes SALUD OCUPACIONAL)
        public const string VisualizadorEvaluaciones          = "56"; // VISUALIZADOR DE EVALUACIONES
        public const string Evaluador                         = "57"; // EVALUADOR
        public const string AdministradorEvaluaciones         = "58"; // ADMINISTRADOR DE EVALUACIONES
        public const string AdministradorDeObra               = "60"; // ADMINISTRADOR DE OBRA
        public const string CostosOficinaTecnica              = "61"; // USUARIO DE COSTOS Y PRESUPUESTOS DE OFICINA TÉCNICA
        public const string UsuarioVecinos                    = "62"; // USUARIO DE VECINOS
        public const string AdministradorObraVecinos          = "63"; // ADMINISTRADOR DE OBRA DE VECINOS
        public const string ContabilidadUsuario               = "64"; // USUARIO DE CONTABILIDAD
        public const string VisualizadorDashboardResidentes   = "65"; // VISUALIZADOR DE DASHBOARD RESIDENTES
        public const string UsuarioTrabajadores               = "66"; // USUARIO DE TRABAJADORES
        public const string CoordinadorSsoma                  = "70"; // COORDINADOR SSOMA
        public const string AsistentaSocial                   = "71"; // ASISTENTA SOCIAL
        public const string Prevencionista                    = "72"; // PREVENCIONISTA
        public const string ContratistaSupervisorCampo        = "74"; // CONTRATISTA - SUPERVISOR DE CAMPO
        public const string AdministradorSolicitudSalidas     = "76"; // ADMINISTRADOR DE SOLICITUD DE SALIDAS
        public const string UsuarioGth                        = "77"; // USUARIO DE GTH
        public const string UsuarioRevisorSalidas             = "78"; // USUARIO REVISOR DE SALIDAS

        /// <summary>
        /// TESORERO. No basta con tenerlo: las features que concede solo se otorgan si además el
        /// puesto del trabajador es de categoría <c>CategoriaIds.Tesorero</c> (46) — lo aplica
        /// <c>AuthRepository.GetAllowedFeaturesAsync</c>.
        /// </summary>
        public const string Tesorero                          = "83"; // TESORERO
    }
}
