using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Shared.Extensions
{
    /// <summary>
    /// Extension methods para extraer en runtime los nombres reales de tablas y columnas
    /// desde el modelo de EF Core. Útil cuando se escribe SQL crudo (Dapper / ADO.NET) y
    /// se quiere mantener refactor-safe el SQL ante renames de modelos o propiedades.
    ///
    /// Uso típico:
    /// <code>
    ///     var tabla   = ctx.Table&lt;Project&gt;();
    ///     var colId   = ctx.Col&lt;Project&gt;(nameof(Project.ProjectId));
    ///     var sql     = $"SELECT {colId} FROM {tabla} WHERE ...";
    /// </code>
    /// </summary>
    public static class EfMetadataExtensions
    {
        /// <summary>
        /// Devuelve el nombre real (en BD) de la tabla mapeada al tipo <typeparamref name="T"/>.
        /// </summary>
        public static string Table<T>(this DbContext ctx) where T : class
        {
            var entity = ctx.Model.FindEntityType(typeof(T))
                ?? throw new InvalidOperationException(
                    $"La entidad '{typeof(T).Name}' no forma parte del modelo EF.");

            return entity.GetTableName()
                ?? throw new InvalidOperationException(
                    $"La entidad '{typeof(T).Name}' no tiene un nombre de tabla mapeado.");
        }

        /// <summary>
        /// Devuelve el nombre real (en BD) de la columna mapeada a la propiedad indicada.
        /// El parámetro debe pasarse con <c>nameof(...)</c> para que el rename automático
        /// del IDE actualice la referencia.
        /// </summary>
        public static string Col<T>(this DbContext ctx, string propertyName) where T : class
        {
            var entity = ctx.Model.FindEntityType(typeof(T))
                ?? throw new InvalidOperationException(
                    $"La entidad '{typeof(T).Name}' no forma parte del modelo EF.");

            var prop = entity.FindProperty(propertyName)
                ?? throw new InvalidOperationException(
                    $"La propiedad '{propertyName}' no existe en '{typeof(T).Name}'.");

            return prop.GetColumnName()
                ?? throw new InvalidOperationException(
                    $"La propiedad '{propertyName}' de '{typeof(T).Name}' no tiene columna mapeada.");
        }
    }
}
