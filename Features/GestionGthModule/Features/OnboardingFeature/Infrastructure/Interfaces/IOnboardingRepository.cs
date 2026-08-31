using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Dtos;

namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Infrastructure.Interfaces
{
    public interface IOnboardingRepository
    {
        /// <summary>
        /// Todo lo de la pantalla de Onboarding en una sola petición: resumen, embudo de fases, tabla
        /// de colaboradores y los candidatos aptos para el modal «Nuevo ingreso».
        /// </summary>
        Task<BandejaOnboardingDto> GetBandeja();

        /// <summary>
        /// Valida que el candidato pueda entrar a onboarding y devuelve el contexto (código, puesto,
        /// correo destino, etc.) que el servicio necesita para subir la carta y armar el correo. No
        /// escribe nada: se llama ANTES de tocar SharePoint o mandar el correo.
        /// </summary>
        Task<OnboardingContextoDto> PrepararInicio(int candidatoId, DateOnly? fechaIngreso, string? correo);

        /// <summary>
        /// Persiste el onboarding ya con su carta oferta cargada y su enlace de firma enviado: fase
        /// inicial CARTA_OFERTA_FIRMADA y estado CARTA_ENVIADA. <paramref name="carpeta"/> es el file
        /// digital del colaborador, que queda guardado para los documentos siguientes. El token del
        /// enlace público viaja en <c>contexto.Token</c>. Devuelve la fila lista para la tabla.
        /// </summary>
        Task<OnboardingListItemDto> Crear(
            OnboardingContextoDto contexto,
            CartaOfertaPersistDto carta,
            FileDigitalCarpetaDto carpeta,
            string? observacion,
            int? userId);

        /// <summary>
        /// Valida que se le pueda reenviar el enlace de firma a un onboarding ya abierto y devuelve el
        /// contexto para armar el correo. No escribe nada: se llama ANTES de mandar el correo.
        ///
        /// Es además la vía por la que un onboarding abierto antes de la firma en línea entra al flujo
        /// nuevo: su fila no tiene token ni ficha resuelta, así que se resuelven acá.
        /// <paramref name="tokenSiFalta"/> solo se usa en ese caso; si la fila ya tiene token, se
        /// conserva para no invalidar el enlace que el colaborador ya recibió.
        /// </summary>
        Task<OnboardingContextoDto> PrepararReenvio(int onboardingId, string? correo, string tokenSiFalta);

        /// <summary>
        /// Deja registrado que el enlace de firma salió: guarda el token, la ficha y el correo usados
        /// y actualiza la fecha de envío. Se llama DESPUÉS del correo, para no dar por enviado un
        /// enlace que nunca llegó.
        /// </summary>
        Task<OnboardingListItemDto> MarcarEnlaceEnviado(
            int onboardingId, OnboardingContextoDto contexto, int? userId);

        /// <summary>
        /// Biblioteca de SharePoint donde vive el file de los colaboradores
        /// (<c>gth_carta_oferta_folder</c>, primera fila vigente): dentro se crea la carpeta
        /// «{DNI} - {NOMBRE}» de cada uno. Se define por BD y no por appsettings: cambiar ese link
        /// redirige los documentos NUEVOS sin tocar los ya subidos, que se abren con la url/itemId
        /// que cada onboarding guardó en su propia fila. Null si no hay fila vigente configurada.
        /// </summary>
        Task<CartaOfertaFolderDto?> GetCartaOfertaFolder();

        /// <summary>
        /// Datos de un onboarding ya abierto necesarios para subirle un documento: su file digital
        /// (si ya está persistido), su código y su nombre. No escribe nada.
        /// </summary>
        Task<OnboardingDocumentoContextoDto> PrepararDocumento(int onboardingId);

        /// <summary>
        /// Guarda la carta oferta firmada ya subida a SharePoint. Si el onboarding todavía no tenía
        /// su file digital persistido, lo completa con <paramref name="carpeta"/>. Reemplazar la
        /// carta borra la aprobación anterior: lo aprobado ya no es lo que está adjunto.
        /// </summary>
        Task<OnboardingListItemDto> GuardarCartaFirmada(
            int onboardingId, CartaOfertaPersistDto carta, FileDigitalCarpetaDto? carpeta, int? userId);

        /// <summary>Marca la carta firmada como aprobada por GTH (RF-ONB-02).</summary>
        Task<OnboardingListItemDto> AprobarCartaFirmada(int onboardingId, int? userId);

        /// <summary>
        /// Avanza el onboarding a la fase siguiente del catálogo, validando lo que esa fase exige.
        /// </summary>
        Task<OnboardingListItemDto> Avanzar(int onboardingId, int? userId);
    }
}
