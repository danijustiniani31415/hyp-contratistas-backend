using Abril_Backend.Features.Habilitacion.Application.Dtos.Auth;
using FluentValidation;

namespace Abril_Backend.Features.Habilitacion.Application.Validators
{
    public class ActivarCuentaValidator : AbstractValidator<ActivarCuentaDto>
    {
        public ActivarCuentaValidator()
        {
            RuleFor(x => x.Token).NotEmpty();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
                .WithMessage("La contraseña debe tener al menos 8 caracteres.")
                .Matches("[A-Z]").WithMessage("Debe contener al menos una mayúscula.")
                .Matches("[0-9]").WithMessage("Debe contener al menos un número.");
        }
    }
}
