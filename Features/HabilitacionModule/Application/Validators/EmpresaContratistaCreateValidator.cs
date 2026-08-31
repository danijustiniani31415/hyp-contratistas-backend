using Abril_Backend.Features.Habilitacion.Application.Dtos.Empresa;
using FluentValidation;

namespace Abril_Backend.Features.Habilitacion.Application.Validators
{
    public class EmpresaContratistaCreateValidator : AbstractValidator<EmpresaContratistaCreateDto>
    {
        public EmpresaContratistaCreateValidator()
        {
            RuleFor(x => x.RazonSocial).NotEmpty().WithMessage("Razón social requerida.")
                .MaximumLength(300);

            RuleFor(x => x.Password).NotEmpty().MinimumLength(6)
                .WithMessage("La contraseña debe tener al menos 6 caracteres.");

            RuleFor(x => x.Ruc).MaximumLength(20).When(x => x.Ruc != null);

            RuleFor(x => x.EmailAdmin).EmailAddress().When(x => !string.IsNullOrEmpty(x.EmailAdmin))
                .WithMessage("Email administrador inválido.");

            RuleFor(x => x.EmailSsoma).EmailAddress().When(x => !string.IsNullOrEmpty(x.EmailSsoma))
                .WithMessage("Email SSOMA inválido.");
        }
    }
}
