using System.Data;
using System.Globalization;
using Dapper;

namespace Abril_Backend.Infrastructure.Data
{
    /// <summary>
    /// Dapper no conoce <see cref="DateOnly"/>: al armar los parámetros de una consulta llama a
    /// <c>LookupDbType</c>, no lo encuentra en su tabla de tipos y revienta con
    /// <c>NotSupportedException: The member X of type System.DateOnly cannot be used as a parameter
    /// value</c> — antes siquiera de tocar la base de datos. Npgsql SÍ lo soporta de forma nativa
    /// (<c>DateOnly</c> ↔ columna <c>date</c>); el que falta es el puente entre ambos.
    ///
    /// Este handler es ese puente y se registra una sola vez al arrancar
    /// (<see cref="Register"/> desde <c>Program.cs</c>). Como hoy cualquier parámetro
    /// <c>DateOnly</c> en Dapper lanza excepción, registrarlo no puede romper nada existente:
    /// solo habilita lo que antes fallaba.
    ///
    /// Dapper resuelve los <c>Nullable&lt;T&gt;</c> desenvolviendo el tipo subyacente, así que
    /// registrar <c>DateOnly</c> cubre también <c>DateOnly?</c> (los nulos llegan como
    /// <see cref="DBNull"/> y los maneja la clase base).
    /// </summary>
    public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.DbType = DbType.Date;
            parameter.Value  = value;
        }

        /// <summary>
        /// Lectura: según el driver y la consulta, una columna <c>date</c> puede volver como
        /// <see cref="DateOnly"/>, como <see cref="DateTime"/> (comportamiento histórico de Npgsql)
        /// o como texto, así que se contemplan los tres.
        /// </summary>
        public override DateOnly Parse(object value) => value switch
        {
            DateOnly d  => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            string s    => DateOnly.Parse(s, CultureInfo.InvariantCulture),
            _ => throw new InvalidCastException(
                     $"No se puede convertir {value?.GetType().FullName ?? "null"} a DateOnly."),
        };
    }

    /// <summary>Registro de los type handlers de Dapper que usa la aplicación.</summary>
    public static class DapperTypeHandlers
    {
        public static void Register()
        {
            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        }
    }
}
