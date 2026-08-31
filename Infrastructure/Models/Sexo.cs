namespace Abril_Backend.Infrastructure.Models {
    /// <summary>
    /// Catálogo normalizado de sexo (M/F). Reemplaza la antigua columna de texto
    /// <c>person.sexo</c>; <see cref="Person.SexoId"/> apunta aquí.
    /// </summary>
    public class Sexo {
        public int SexoId { get; set; }
        public string Codigo { get; set; } = default!;
        public string Nombre { get; set; } = default!;
        public int Orden { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
