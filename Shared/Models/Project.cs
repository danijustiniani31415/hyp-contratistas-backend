using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Shared.Models {
    public class Project {
        // Identidad
        public int ProjectId {get; set;}
        public string ProjectDescription {get; set;}
        public string? Codigo {get; set;}
        public string? Abbreviation {get; set;}
        public string? LevelDescription {get; set;}

        // Estado del proyecto (vigente, finalizado, etc.)
        public string? Estado {get; set;}

        // Contribuyente / razón social
        public int? ContributorId {get; set;}
        public Contributor? Contributor {get; set;}

        // Ubicación
        public string? ProjectDistrict {get; set;}
        public string? ProjectProvince {get; set;}
        public string? ProjectDepartment {get; set;}
        public string? ProjectLocation {get; set;}

        // Responsables
        public string? ResponsableArqCom {get; set;}
        public int? ResponsableArqComId {get; set;}
        public string? ResponsableUdp {get; set;}
        public int? ResponsableUdpId {get; set;}
        public string? ResponsablePlaneamientoBim {get; set;}
        public int? ResponsablePlaneamientoBimId {get; set;}

        // Planeamiento BIM: meta de PPC pactada, fija por proyecto
        public decimal? MetaPpc {get; set;}

        // Residente: referencia al trabajador. Su correo se lee de
        // workers.email_corporativo al enviar, así sigue siempre al dato maestro.
        public int? ResidenteWorkersId {get; set;}

        // Emails del proyecto
        /// <summary>
        /// DEPRECADO — reemplazado por <see cref="ResidenteWorkersId"/>. Se conserva
        /// solo como histórico (convención del proyecto: no se borran campos); ningún
        /// código lo lee. Para el residente usar la FK.
        /// </summary>
        public string? EmailResidente {get; set;}
        public string? EmailResponsable {get; set;}
        public string? EmailRrhh {get; set;}
        public string? EmailCoordSsoma {get; set;}
        public string? EmailCoordAdmin {get; set;}
        public string? StaffEmail {get; set;}

        // Fechas del proyecto
        public DateOnly? FechaInicio {get; set;}
        public DateOnly? FechaFin {get; set;}
        public DateOnly? InicioObra {get; set;}
        public DateOnly? FinObra {get; set;}

        // Métricas físicas
        public string? NumNiveles {get; set;}
        public string? NumSotanos {get; set;}
        public string? Pisos {get; set;}
        public int? TiempoConstruccion {get; set;}
        public decimal? AreaM2 {get; set;}
        public decimal? AreaTechadaM2 {get; set;}
        public decimal? HhTotalCasa {get; set;}
        public string? CantTrabajadoresCasa {get; set;}
        /// <summary>HH_REAL | HH_PROYECTADO | HH_CALCULADO_MEDIANA</summary>
        public string? HhFuente {get; set;}
        /// <summary>Estado del ciclo de vida: Finalizado | Activo | Inactivo</summary>
        public string? Activo {get; set;}

        // Contadores
        public int ContadorIncidentes {get; set;}
        public int ContadorAccidentes {get; set;}
        public int ContadorRac {get; set;}
        public int ContadorPenalidad {get; set;}

        // Flags
        public bool TieneArquitecturaComercial {get; set;}
        public bool TieneUnidadDeProyectos {get; set;}
        public bool Operativo { get; set; } = true;

        // Foto
        public string? FotoUrl {get; set;}

        // Geolocalización (geofencing de Tareos)
        public decimal? Lat {get; set;}
        public decimal? Lng {get; set;}
        public decimal RadioGeofenceMetros {get; set;} = 300;

        // Auditoría
        public DateTime CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTime? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}

        // Navegaciones
        public List<ResidentReportIncidence> Incidences { get; set; }
    }
}