namespace Abril_Backend.Features.AuthModule.ContractorCredentials.Application.Dtos
{
    public class ContractorCredentialsCreateDto
    {
        public string Token { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }
}
