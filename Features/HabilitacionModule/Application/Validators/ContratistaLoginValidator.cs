using Abril_Backend.Features.Habilitacion.Application.Dtos.Auth;
using FluentValidation;

namespace Abril_Backend.Features.Habilitacion.Application.Validators
{
    public class ContratistaLoginValidator : AbstractValidator<ContratistaLoginDto>
    {
        public ContratistaLoginValidator()
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email requerido.")
                .EmailAddress().WithMessage("Email inválido.");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Contraseña requerida.")
                .MinimumLength(4).WithMessage("Mínimo 4 caracteres.");
        }
    }
}
