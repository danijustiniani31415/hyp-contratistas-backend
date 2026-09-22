namespace Abril_Backend.Features.PersonasModule.Application.Dtos
{
    public class CatalogoValorDto
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = null!;
        public string Valor { get; set; } = null!;
        public bool Activo { get; set; }
    }

    public class CatalogoValorCreateDto
    {
        public string Tipo { get; set; } = null!;
        public string Valor { get; set; } = null!;
    }

    public class CatalogoValorUpdateDto
    {
        public string Valor { get; set; } = null!;
        public bool Activo { get; set; } = true;
    }
}
