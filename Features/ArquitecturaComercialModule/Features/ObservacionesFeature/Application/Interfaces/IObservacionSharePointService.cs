namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Interfaces;

public interface IObservacionSharePointService
{
    Task<string> SubirFotoObservacionAsync(Stream stream, string fileName, string contentType, int proyectoId, int observacionId);
    Task<string> SubirFotoLevantamientoAsync(Stream stream, string fileName, string contentType, int proyectoId, int observacionId, int orden);

    /// <summary>Descarga los bytes de una foto ya subida (por su webUrl guardado) usando permisos
    /// de aplicación — no depende de que el navegador tenga sesión de SharePoint, que es justo lo
    /// que rompía las miniaturas en celulares sin sesión de Microsoft 365 iniciada.</summary>
    Task<(byte[] Bytes, string ContentType)> DescargarFotoAsync(string url);
}
