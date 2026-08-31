namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Application.Interfaces;

public interface IRevisionSharePointService
{
    Task<string> SubirFotoObservacionAsync(Stream stream, string fileName, string contentType, int proyectoId, int revisionObservacionId);
    Task<string> SubirFotoLevantamientoAsync(Stream stream, string fileName, string contentType, int proyectoId, int revisionObservacionId, int orden);

    /// <summary>Descarga bytes de una foto ya subida usando permisos de aplicación — mismo
    /// motivo que en Observaciones (evita depender de sesión de SharePoint del navegador).</summary>
    Task<(byte[] Bytes, string ContentType)> DescargarFotoAsync(string url);
}
