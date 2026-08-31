using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Shared.Services.Contractors
{
    /// <summary>
    /// Regla de negocio: un correo de usuario pertenece a UNA sola empresa contratista
    /// (vínculo en contractor_user); contractor_email queda solo como correos de contacto
    /// y sí puede repetirse entre contratistas.
    /// </summary>
    public static class ContractorAccountEmailPolicy
    {
        /// <summary>
        /// Valida que el correo pueda usarse como usuario de la contratista indicada.
        /// Devuelve el usuario existente (para re-registro de contraseña) o null si hay
        /// que crearlo. Lanza AbrilException 409 si el correo ya pertenece a un usuario
        /// de otra contratista o a un usuario interno del sistema.
        /// </summary>
        public static async Task<User?> ValidateAndGetUserAsync(
            AppDbContext ctx, string emailNormalizado, int contractorId, int contributorId)
        {
            if (emailNormalizado.EndsWith("@abril.pe"))
                throw new AbrilException("No se puede usar un correo interno de Abril como usuario de contratista.", 409);

            var user = await ctx.User.FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalizado);
            if (user == null) return null;

            // 1. ¿Ya es usuario titular de una contratista? (contractor_user)
            var contratistaVinculada = await ctx.ContractorUser
                .Where(cu => cu.UserId == user.UserId && cu.Active && cu.State)
                .Select(cu => (int?)cu.ContractorId)
                .FirstOrDefaultAsync();
            if (contratistaVinculada != null)
            {
                if (contratistaVinculada != contractorId)
                    throw new AbrilException("Este correo ya está registrado como usuario de otra empresa contratista. Utilice un correo diferente.", 409);
                return user; // misma contratista: se permite volver a registrar la contraseña
            }

            // 2. ¿Es sub-usuario invitado de alguna empresa? (ss_contratista_usuario guarda contributor_id)
            var subUsuarioDeEmpresa = await ctx.SsContratistaUsuarios
                .Where(s => s.UserId == user.UserId && s.Activo)
                .Select(s => (int?)s.ContractorId)
                .FirstOrDefaultAsync();
            if (subUsuarioDeEmpresa != null)
            {
                if (subUsuarioDeEmpresa != contributorId)
                    throw new AbrilException("Este correo ya está registrado como usuario de otra empresa contratista. Utilice un correo diferente.", 409);
                return user; // sub-usuario de esta misma empresa
            }

            // 3. ¿Usuario de otros módulos del sistema? (roles distintos a CONTRATISTA = 11)
            var tieneOtrosRoles = await ctx.UserRole
                .AnyAsync(ur => ur.UserId == user.UserId && ur.Active && ur.State && ur.RoleId != 11);
            if (tieneOtrosRoles)
                throw new AbrilException("Este correo ya está en uso por otro usuario del sistema. Utilice un correo diferente.", 409);

            // Usuario huérfano (p. ej. migrado sin vínculo): se reclama para esta contratista.
            return user;
        }
    }
}
