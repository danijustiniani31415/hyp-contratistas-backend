using Abril_Backend.Features.Habilitacion.Application.Dtos.SctrVidaley;
using FluentValidation;

namespace Abril_Backend.Features.Habilitacion.Application.Validators
{
    public class SctrVidaLeyCreateValidator : AbstractValidator<SctrVidaLeyCreateDto>
    {
        public SctrVidaLeyCreateValidator()
        {

            RuleFor(x => x.Tipo).Must(t => t == "SCTR" || t == "VIDA_LEY")
                .WithMessage("Tipo debe ser SCTR o VIDA_LEY.");

            RuleFor(x => x.Mes)
                .Must(m => m == 0 || (m >= 1 && m <= 12))
                .WithMessage("Mes debe estar entre 1 y 12, o ser 0 para calcularse desde FechaInicio.");

            RuleFor(x => x.Anio)
                .Must(a => a == 0 || a > 2020)
                .WithMessage("Anio debe ser mayor a 2020, o ser 0 para calcularse desde FechaInicio.");

            RuleFor(x => x)
                .Must(x => (x.Mes != 0 && x.Anio != 0) || x.FechaInicio.HasValue)
                .WithMessage("Si Mes o Anio son 0, FechaInicio es requerido para calcularlos.")
                .OverridePropertyName("FechaInicio");

            RuleFor(x => x.TipoPoliza)
                .Must(t => t == "Renovacion" || t == "Inclusion")
                .WithMessage("TipoPoliza debe ser Renovacion o Inclusion.");

            RuleFor(x => x.Workers).NotEmpty()
                .WithMessage("Debe seleccionar al menos un trabajador.");

            RuleForEach(x => x.Workers)
                .ChildRules(w => w.RuleFor(x => x.WorkerId).GreaterThan(0));
        }
    }
}
