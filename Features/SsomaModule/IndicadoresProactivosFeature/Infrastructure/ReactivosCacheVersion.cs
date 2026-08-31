namespace Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Infrastructure;

/// <summary>
/// Contador simple para invalidar en bloque la caché de indicadores reactivos.
/// IMemoryCache no soporta borrar por prefijo, así que en vez de eso las claves
/// de caché incluyen esta versión: cualquier cambio que afecte accidentes o
/// días perdidos llama <see cref="Bump"/> y el próximo request recalcula en
/// lugar de servir la caché vieja (antes había que esperar hasta 10 minutos).
/// </summary>
public class ReactivosCacheVersion
{
    private int _version;
    public int Current => _version;
    public void Bump() => Interlocked.Increment(ref _version);
}
