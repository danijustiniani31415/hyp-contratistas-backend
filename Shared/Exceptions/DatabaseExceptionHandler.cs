using Microsoft.AspNetCore.Diagnostics;

namespace Abril_Backend.Shared.Exceptions
{
    /// <summary>
    /// Convierte fallos de conectividad con la BD (sin conexiones libres, servidor caído, etc.)
    /// en una respuesta 503 clara con el detalle real, en lugar de un 500 genérico.
    /// Solo actúa para endpoints cuya excepción NO fue atrapada por el try/catch del controller.
    /// </summary>
    public class DatabaseExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<DatabaseExceptionHandler> _logger;

        public DatabaseExceptionHandler(ILogger<DatabaseExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (!DbConnectivity.IsUnavailable(exception, out var detalle))
                return false; // no es un fallo de BD → que lo maneje el siguiente handler

            _logger.LogError(exception, "Base de datos no disponible: {Detalle}", detalle);

            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                message = "La base de datos no está disponible en este momento (sin conexiones libres). Intenta nuevamente en unos segundos.",
                detalle
            }, cancellationToken);

            return true;
        }
    }
}
