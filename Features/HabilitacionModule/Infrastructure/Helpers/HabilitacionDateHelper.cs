namespace Abril_Backend.Features.Habilitacion.Infrastructure.Helpers
{
    public static class HabilitacionDateHelper
    {
        private static readonly HashSet<int> ItemsSctrVidaLey = new() { 15, 16 };
        private static readonly HashSet<int> ItemsCentinela = new() { 12, 13, 14, 17, 18, 19, 21, 23, 24, 25 };

        public static DateTime? AsUtc(DateTime? value)
        {
            if (!value.HasValue) return null;
            var v = value.Value;
            return v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc);
        }

        public static DateTime? ResolverVigencia(bool requiereVigencia, string estado, DateTime? dtoVigencia)
        {
            var esSintetico = !requiereVigencia
                && (string.Equals(estado, "Aprobado", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(estado, "Enviado", StringComparison.OrdinalIgnoreCase));
            if (esSintetico)
                return DateTime.SpecifyKind(new DateOnly(2040, 12, 31).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            return AsUtc(dtoVigencia);
        }

        public static DateTime? ResolverVigenciaAlEnviar(int itemId, bool esMensual, int? mes, int? anio, DateTime? dtoVigencia)
        {
            if (ItemsSctrVidaLey.Contains(itemId))
                return AsUtc(dtoVigencia);

            if (ItemsCentinela.Contains(itemId))
                return DateTime.SpecifyKind(new DateOnly(2040, 12, 31).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

            if (esMensual && mes.HasValue && anio.HasValue)
            {
                var mesSig = mes.Value == 12 ? 1 : mes.Value + 1;
                var anioSig = mes.Value == 12 ? anio.Value + 1 : anio.Value;
                return DateTime.SpecifyKind(new DateOnly(anioSig, mesSig, 27).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            }

            return AsUtc(dtoVigencia);
        }

        public static DateTime? ResolverVigenciaAlAprobar(int itemId, string estado, DateTime? dtoVigencia, DateTime? vigenciaActual)
        {
            if (string.Equals(estado, "Rechazado", StringComparison.OrdinalIgnoreCase))
                return DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-1), DateTimeKind.Utc);

            if (string.Equals(estado, "Aprobado", StringComparison.OrdinalIgnoreCase))
            {
                if (ItemsCentinela.Contains(itemId))
                    return DateTime.SpecifyKind(new DateOnly(2040, 12, 31).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
                if (dtoVigencia.HasValue) return AsUtc(dtoVigencia);
                return vigenciaActual;
            }

            return vigenciaActual;
        }

        public static DateTime? ResolverVigenciaEmpresa(int itemId, string estado, DateTime? dtoVigencia)
        {
            if (string.Equals(estado, "Rechazado", StringComparison.OrdinalIgnoreCase))
                return DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-1), DateTimeKind.Utc);

            if (ItemsCentinela.Contains(itemId))
                return DateTime.SpecifyKind(new DateOnly(2040, 12, 31).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

            if (dtoVigencia.HasValue) return AsUtc(dtoVigencia);

            var hoy = DateTime.UtcNow;
            return new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddDays(26);
        }
    }
}
