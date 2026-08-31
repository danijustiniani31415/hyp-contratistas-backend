using Abril_Backend.Features.Habilitacion.Application.Dtos.Auth;
using FluentValidation;

namespace Abril_Backend.Features.Habilitacion.Application.Validators
{
    public class CambiarPasswordValidator : AbstractValidator<CambiarPasswordDto>
    {
        public CambiarPasswordValidator()
        {
            RuleFor(x => x.PasswordActual).NotEmpty();
            RuleFor(x => x.PasswordNuevo).NotEmpty().MinimumLength(8);
        }
    }
}
