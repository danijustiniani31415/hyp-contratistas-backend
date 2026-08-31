namespace Abril_Backend.Shared.Services.SharePoint.Options
{
    /// <summary>
    /// Identifica un sitio de SharePoint por hostname + path. Lo pasan los callers
    /// a <see cref="Interfaces.IGraphSharePointService"/> para mantener el servicio
    /// agnóstico al sitio sobre el que opera.
    /// </summary>
    public sealed record SharePointSiteRef(string Hostname, string SitePath)
    {
        /// <summary>
        /// Construye una referencia a partir de <c>SharePoint:Sites:{siteKey}:Hostname</c>
        /// y <c>SharePoint:Sites:{siteKey}:SitePath</c>. Lanza si falta alguno.
        /// </summary>
        public static SharePointSiteRef FromConfig(IConfiguration cfg, string siteKey)
        {
            var basePath = $"SharePoint:Sites:{siteKey}";
            var host = cfg[$"{basePath}:Hostname"]
                ?? throw new InvalidOperationException($"{basePath}:Hostname no está configurado.");
            var path = cfg[$"{basePath}:SitePath"]
                ?? throw new InvalidOperationException($"{basePath}:SitePath no está configurado.");
            return new SharePointSiteRef(host, path);
        }

        /// <summary>Clave estable para cachear por sitio.</summary>
        public string CacheKey => $"{Hostname.ToLowerInvariant()}|{SitePath.Trim('/').ToLowerInvariant()}";
    }
}
