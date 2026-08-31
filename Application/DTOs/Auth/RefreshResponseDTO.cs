namespace Abril_Backend.Application.DTOs
{
    /// <summary>
    /// Respuesta del refresco de sesión: un JWT nuevo (vida corta) y la lista de
    /// features permitidas, ambos recalculados con los datos actuales de BD para
    /// que cambios de rol se reflejen sin re-loguear.
    /// </summary>
    public class RefreshResponseDTO
    {
        public string AccessToken { get; set; } = null!;
        public List<string> AllowedFeatures { get; set; } = new();
    }
}
