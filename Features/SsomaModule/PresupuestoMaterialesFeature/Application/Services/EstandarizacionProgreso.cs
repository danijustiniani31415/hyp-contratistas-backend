using System.Collections.Concurrent;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Services;

/// <summary>
/// Progreso en memoria de un EstandarizarCargaAsync en curso, para que el frontend pueda mostrar
/// "línea X de Y" mientras dura (lotes grandes, como un Kardex histórico de miles de líneas, pueden
/// tardar varios minutos). No persiste nada — si el backend se reinicia a mitad de camino, el
/// progreso desaparece junto con la operación misma (que de todas formas se corta ahí).
/// </summary>
public static class EstandarizacionProgreso
{
    private static readonly ConcurrentDictionary<int, (int Procesadas, int Total)> _progreso = new();

    public static void Iniciar(int cargaId, int total) => _progreso[cargaId] = (0, total);

    public static void Avanzar(int cargaId) =>
        _progreso.AddOrUpdate(cargaId, (1, 0), (_, actual) => (actual.Procesadas + 1, actual.Total));

    public static (int Procesadas, int Total)? Obtener(int cargaId) =>
        _progreso.TryGetValue(cargaId, out var v) ? v : null;

    public static void Finalizar(int cargaId) => _progreso.TryRemove(cargaId, out _);
}
