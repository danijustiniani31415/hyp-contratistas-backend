namespace Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Application.Dtos
{
    public class HolidayDto
    {
        public int HolidayId { get; set; }
        public int HolidayTypeId { get; set; }
        public string HolidayTypeName { get; set; } = string.Empty;
        public DateOnly HolidayDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool RecurringYearly { get; set; }
        public bool Active { get; set; }
    }

    public class HolidayCreateDto
    {
        public int HolidayTypeId { get; set; }
        public DateOnly HolidayDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool RecurringYearly { get; set; }
        public bool Active { get; set; } = true;
    }

    public class HolidayEditDto
    {
        public int HolidayId { get; set; }
        public int HolidayTypeId { get; set; }
        public DateOnly HolidayDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool RecurringYearly { get; set; }
        public bool Active { get; set; }
    }

    public class HolidayTypeSimpleDto
    {
        public int HolidayTypeId { get; set; }
        public string HolidayTypeName { get; set; } = string.Empty;
    }

    /// <summary>Carga inicial de la pantalla: tipos (para los desplegables) + primera página de la tabla.</summary>
    public class HolidayInitialDto
    {
        public List<HolidayTypeSimpleDto> Types { get; set; } = new();
        public Abril_Backend.Application.DTOs.PagedResult<HolidayDto> Holidays { get; set; } = new();
    }
}
