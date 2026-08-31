namespace Abril_Backend.Features.Contractors.ContractorManagement.Application.Dtos
{
    /// <summary>
    /// Representa un correo de contacto de un contratista junto con su estado.
    /// Se usa tanto para devolver el detalle (lectura) como para recibir la edición (escritura).
    /// </summary>
    public class ContractorEmailItemDto
    {
        /// <summary>Id del correo. Null o 0 indica un correo nuevo a insertar.</summary>
        public int? ContractorEmailId { get; set; }
        public string Email { get; set; } = null!;
        /// <summary>Flag 'active': si está en false el correo no aparece en filtros/desplegables ni recibe correos.</summary>
        public bool Active { get; set; }
        /// <summary>Clasificación del contacto (Gerente General, etc.). Null = sin clasificar.</summary>
        public int? ContractorPersonTypeId { get; set; }
        /// <summary>Descripción de la clasificación. Solo lectura (se ignora en la edición).</summary>
        public string? ContractorPersonTypeDescription { get; set; }
    }
}
