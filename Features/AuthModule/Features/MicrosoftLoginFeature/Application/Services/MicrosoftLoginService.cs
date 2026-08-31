using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AuthModule.MicrosoftLogin.Application.Dtos;
using Abril_Backend.Features.AuthModule.MicrosoftLogin.Application.Interfaces;
using Abril_Backend.Features.AuthModule.MicrosoftLogin.Infrastructure.Interfaces;
using Abril_Backend.Features.AuthModule.MicrosoftProfile.Application.Interfaces;
using Abril_Backend.Infrastructure.Interfaces;

namespace Abril_Backend.Features.AuthModule.MicrosoftLogin.Application.Services
{
    public class MicrosoftLoginService : IMicrosoftLoginService
    {
        /// <summary>
        /// USUARIO REVISOR DE SALIDAS — espejo entero de
        /// <see cref="Shared.Constants.Roles.UsuarioRevisorSalidas"/>, que es string porque
        /// así viaja en el claim del JWT; acá se necesita el ID para asignar el user_role.
        /// </summary>
        private const int RoleIdUsuarioRevisorSalidas = 78;

        private readonly IMicrosoftProfileService _profileService;
        private readonly IMicrosoftLoginRepository _repository;
        private readonly IJWTService _jwtService;
        private readonly IAuthRepository _authRepository;

        public MicrosoftLoginService(
            IMicrosoftProfileService profileService,
            IMicrosoftLoginRepository repository,
            IJWTService jwtService,
            IAuthRepository authRepository)
        {
            _profileService = profileService;
            _repository = repository;
            _jwtService = jwtService;
            _authRepository = authRepository;
        }

        public async Task<MicrosoftLoginResponseDto> Login(string graphAccessToken)
        {
            var profileTask = _profileService.GetProfile(graphAccessToken);
            var photoTask = _profileService.GetPhotoBase64(graphAccessToken);

            await Task.WhenAll(profileTask, photoTask);

            var profile = await profileTask;
            if (profile is null)
                throw new AbrilException("No se pudo obtener el perfil de Microsoft.", 401);

            var email = profile.Mail ?? profile.UserPrincipalName;

            // El acceso vía Microsoft SSO está restringido al tenant @abril.pe.
            if (string.IsNullOrWhiteSpace(email)
                || !email.Trim().EndsWith("@abril.pe", StringComparison.OrdinalIgnoreCase))
            {
                throw new AbrilException(
                    "Solo se permite el acceso con cuentas corporativas @abril.pe.", 403);
            }

            var user = await _repository.GetUserByEmailAsync(email);

            if (user is null)
            {
                var existingPerson = await _repository.GetPersonByWorkerEmailAsync(email);
                user = existingPerson is not null
                    ? await _repository.CreateUserAndLinkPersonAsync(profile, existingPerson.PersonId)
                    : await _repository.CreateUserFromGraphAsync(profile);

                // Primer login: si el correo es del tenant @abril.pe, asignar rol "USUARIO ABRIL" (RoleId = 12).
                if (!string.IsNullOrWhiteSpace(email)
                    && email.Trim().EndsWith("@abril.pe", StringComparison.OrdinalIgnoreCase))
                {
                    var rolAbril = await _repository.AssignRoleAsync(user.UserId, 12);
                    if (rolAbril is not null)
                    {
                        user.Roles ??= new();
                        if (!user.Roles.Any(r => r.RoleId == rolAbril.RoleId))
                            user.Roles.Add(rolAbril);
                    }

                    // Roles automáticos por área del worker asociado.
                    var personId = user.Person?.PersonId ?? 0;
                    if (personId > 0)
                    {
                        var area = await _repository.GetWorkerAreaByPersonIdAsync(personId);

                        if (area?.Equals("Proyectos", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            // 9=ADMINISTRADOR SSOMA, 49=SERVICIO DE VIGILANCIA, 57=EVALUADOR
                            foreach (var roleId in new[] { 9, 49, 57 })
                            {
                                var rol = await _repository.AssignRoleAsync(user.UserId, roleId);
                                if (rol is not null && !user.Roles!.Any(r => r.RoleId == rol.RoleId))
                                    user.Roles!.Add(rol);
                            }
                        }

                        // Revisor de área (Revisores de Áreas): necesita entrar a Gestión de
                        // Salidas para aprobar las solicitudes de su área. Buena parte de los
                        // revisores designados no tenía usuario del sistema todavía, así que el
                        // rol no se les pudo asignar por SQL; se les asigna acá, al crearse la
                        // cuenta en su primer login.
                        if (await _repository.IsAreaRevisorByPersonIdAsync(personId))
                        {
                            var rolRevisor = await _repository.AssignRoleAsync(user.UserId, RoleIdUsuarioRevisorSalidas);
                            if (rolRevisor is not null && !user.Roles!.Any(r => r.RoleId == rolRevisor.RoleId))
                                user.Roles!.Add(rolRevisor);
                        }
                    }
                }
            }
            else if (user.Person is null || user.Person.PersonId == 0)
            {
                var existingPerson = await _repository.GetPersonByWorkerEmailAsync(email);
                user.Person = existingPerson is not null
                    ? await _repository.LinkPersonToUserAsync(user.UserId, existingPerson.PersonId, email)
                    : await _repository.CreatePersonForUserAsync(user.UserId, profile);
            }

            var accessToken     = _jwtService.GenerateToken(user);
            var session         = await _authRepository.CreateSessionAsync(user.UserId);
            var allowedFeatures = await _authRepository.GetAllowedFeaturesAsync(user.UserId);

            return new MicrosoftLoginResponseDto
            {
                AccessToken     = accessToken,
                SessionToken    = session.Token,
                ExpiresAt       = session.ExpiresAt,
                AllowedFeatures = allowedFeatures,
                DisplayName       = profile.DisplayName,
                GivenName         = profile.GivenName,
                Surname           = profile.Surname,
                UserPrincipalName = profile.UserPrincipalName,
                Mail              = profile.Mail,
                JobTitle          = profile.JobTitle,
                OfficeLocation    = profile.OfficeLocation,
                MobilePhone       = profile.MobilePhone,
                BusinessPhones    = profile.BusinessPhones,
                Department        = profile.Department,
                PhotoBase64       = await photoTask
            };
        }
    }
}
