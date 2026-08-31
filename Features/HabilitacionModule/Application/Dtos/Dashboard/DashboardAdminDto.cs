namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Dashboard
{
    public class DashboardAdminDto
    {
        public int ProyectoId { get; set; }
        public string ProyectoNombre { get; set; } = "";
        public DashboardKpisDto Kpis { get; set; } = new();
        public List<EmpresaResumenDto> Empresas { get; set; } = new();
        public List<WorkerNombradoDto> TrabajadoresNoAutorizados { get; set; } = new();
        public List<EntregableNombradoDto> EntregablesEmpresaVencidos { get; set; } = new();
        public List<EntregableNombradoDto> EntregablesEmpresaFalta { get; set; } = new();
        public List<EntregableNombradoDto> EntregablesTrabajadorVencidos { get; set; } = new();
        public List<EntregableNombradoDto> EntregablesTrabajadorFalta { get; set; } = new();
        public List<EntregableNombradoDto> EntregablesCasaVencidos { get; set; } = new();
        public List<EntregableNombradoDto> EntregablesCasaFalta { get; set; } = new();
        public List<WorkerNombradoDto> EmosVencidos { get; set; } = new();
        public List<InterconsultaNombradaDto> Interconsultas { get; set; } = new();
        public List<WorkerNombradoDto> PersonalCasaNoHabilitado { get; set; } = new();
    }

    public class DashboardKpisDto
    {
        public int EmpresasActivas { get; set; }
        public int EmpresasHabilitadas { get; set; }
        public int EmpresasNoHabilitadas { get; set; }
        public int WorkersTotal { get; set; }
        public int WorkersHabilitados { get; set; }
        public int WorkersNoAutorizados { get; set; }
        public int WorkersAutorizadoTemporal { get; set; }
        public int EntregablesEmpresaVencidos { get; set; }
        public int EntregablesEmpresaFalta { get; set; }
        public int EntregablesTrabajadorVencidos { get; set; }
        public int EntregablesTrabajadorFalta { get; set; }
        public int EntregablesCasaVencidos { get; set; }
        public int EntregablesCasaFalta { get; set; }
        public int EmosVencidos { get; set; }
        public int InterconsultasPendientes { get; set; }
        public int PersonalCasaTotal { get; set; }
        public int PersonalCasaHabilitado { get; set; }
        public int PersonalCasaNoHabilitado { get; set; }
    }

    public class EmpresaResumenDto
    {
        public int EmpresaId { get; set; }
        public string Nombre { get; set; } = "";
        public bool Habilitada { get; set; }
        public int WorkersTotal { get; set; }
        public int WorkersHabilitados { get; set; }
        public int WorkersNoAutorizados { get; set; }
    }

    public class WorkerNombradoDto
    {
        public int WorkerId { get; set; }
        public string Nombre { get; set; } = "";
        public string Dni { get; set; } = "";
        public string Empresa { get; set; } = "";
        public string Motivo { get; set; } = "";
    }

    public class EntregableNombradoDto
    {
        public string Entidad { get; set; } = "";
        public string Item { get; set; } = "";
        public DateTime? Vigencia { get; set; }
    }

    public class InterconsultaNombradaDto
    {
        public int WorkerId { get; set; }
        public string Nombre { get; set; } = "";
        public string Empresa { get; set; } = "";
        public string Especialidad { get; set; } = "";
        public int DiasDesdeDerivacion { get; set; }
    }
}
