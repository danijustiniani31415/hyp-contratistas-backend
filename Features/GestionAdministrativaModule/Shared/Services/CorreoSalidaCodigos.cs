namespace Abril_Backend.Features.GestionAdministrativa.Shared.Services
{
    /// <summary>Códigos estables del catálogo ga_correo_evento (los correos del flujo de salidas).</summary>
    public static class CorreoEventoCodigos
    {
        public const string Revisor = "REVISOR";
        public const string Confirmacion = "CONFIRMACION";
        public const string Aprobada = "APROBADA";
        public const string Rechazada = "RECHAZADA";

        /// <summary>
        /// Aviso al jefe/revisor de que el trabajador ya adjuntó el Consolidado del S10 y su
        /// reembolso está esperando revisión. Lo dispara el trabajador desde el autoservicio.
        /// </summary>
        public const string S10Revisor = "S10_REVISOR";

        /// <summary>El jefe aprobó el reembolso de una salida rendida — se avisa al solicitante.</summary>
        public const string ReembolsoAprobado = "REEMBOLSO_APROBADO";

        /// <summary>El jefe rechazó el reembolso — se avisa al solicitante con la observación.</summary>
        public const string ReembolsoRechazado = "REEMBOLSO_RECHAZADO";
    }

    /// <summary>Códigos estables del catálogo ga_correo_tipo_destinatario.</summary>
    public static class CorreoTipoCodigos
    {
        public const string Trabajador = "TRABAJADOR";
        public const string Area = "AREA";
        public const string Correo = "CORREO";
    }
}
