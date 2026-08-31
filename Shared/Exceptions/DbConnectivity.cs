using System.Net.Sockets;
using Npgsql;

namespace Abril_Backend.Shared.Exceptions
{
    /// <summary>
    /// Detecta fallos de conectividad/recursos con la base de datos (PostgreSQL/Npgsql),
    /// recorriendo toda la cadena de InnerException (incluida RetryLimitExceededException,
    /// que envuelve la PostgresException real).
    /// </summary>
    public static class DbConnectivity
    {
        public static bool IsUnavailable(Exception? ex, out string detalle)
        {
            detalle = string.Empty;

            for (var e = ex; e is not null; e = e.InnerException)
            {
                switch (e)
                {
                    // PostgreSQL SqlStates:
                    //  53300 too_many_connections  (← "remaining connection slots are reserved...")
                    //  53400 configuration_limit_exceeded
                    //  57P03 cannot_connect_now
                    //  clase 08*  connection_exception (conexión caída/rechazada)
                    case PostgresException pg when pg.SqlState is "53300" or "53400" or "57P03"
                                                   || (pg.SqlState?.StartsWith("08") ?? false):
                        detalle = $"{pg.SqlState}: {pg.MessageText}";
                        return true;

                    // Fallos de red al abrir la conexión (servidor caído, timeout, DNS, etc.)
                    case NpgsqlException npg when npg.InnerException is SocketException or TimeoutException:
                        detalle = npg.Message;
                        return true;

                    case SocketException sock:
                        detalle = sock.Message;
                        return true;

                    case TimeoutException to:
                        detalle = to.Message;
                        return true;
                }
            }

            return false;
        }
    }
}
