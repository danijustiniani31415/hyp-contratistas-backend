using Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores;
using FluentValidation;

namespace Abril_Backend.Features.Habilitacion.Application.Validators
{
    public class WorkerEntregableUpdateValidator : AbstractValidator<WorkerEntregableUpdateDto>
    {
        private static readonly string[] EstadosValidos = { "Falta", "Enviado", "Aprobado", "Rechazado", "No Aplica", "En plazo", "Vencido", "Renovando" };

        public WorkerEntregableUpdateValidator()
        {
            RuleFor(x => x.Estado)
                .Must(e => EstadosValidos.Contains(e))
                .When(x => !string.IsNullOrEmpty(x.Estado))
                .WithMessage("Estado inválido.");

            RuleFor(x => x.Vigencia)
                .Must((dto, vigencia) => vigencia == null || dto.Estado == "Falta" || vigencia.Value > DateTime.Today)
                .WithMessage("La vigencia debe ser una fecha futura.");
        }
    }
}
