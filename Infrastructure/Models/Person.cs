namespace Abril_Backend.Infrastructure.Models {
    public class Person {
        public int PersonId {get; set;}
        public int? UserId {get;set;}
        public int? DocumentIdentityTypeId {get; set;}
        public string? DocumentIdentityCode {get; set;}
        public string? FirstNames {get; set;}
        public string? FirstName {get; set;}
        public string? SecondName {get; set;}
        public string? FirstLastName {get; set;}
        public string? SecondLastName {get; set;}
        public string? FullName {get; set;}
        /// <summary>
        /// Correo personal / de contacto. El corporativo vive en <c>workers.email_corporativo</c> y el
        /// de autenticación en <c>app_user.email</c> — este nunca se usa para iniciar sesión.
        /// Se puede repetir entre personas a propósito (varios trabajadores de una contratista
        /// comparten el correo de su RR.HH.): el UNIQUE que tenía la columna se quitó por eso.
        /// </summary>
        public string? Email {get; set;}
        /// <summary>Celular personal. El corporativo vive en <c>workers.celular_corporativo</c>.</summary>
        public int? PhoneNumber {get;set;}
        /// <summary>
        /// Fecha de nacimiento (columna <c>fecha_nacimiento</c>). Fuente única del cumpleaños
        /// (antes duplicado en <c>person.cumpleanos</c> y <c>workers.fecha_nacimiento</c>).
        /// Para el calendario del boletín solo interesa día y mes.
        /// </summary>
        public DateOnly? FechaNacimiento {get; set;}
        /// <summary>
        /// true = su cumpleaños aparece en el calendario del boletín (columna
        /// <c>mostrar_en_boletin</c>). Lo controla el checkbox "Mostrar en el boletín" del
        /// formulario de trabajadores, y viene marcado por defecto: quien no quiera figurar se
        /// desmarca a mano. Es independiente de <see cref="FechaNacimiento"/>: la fecha se sigue
        /// guardando (la usan EMO y GTH), solo deja de publicarse.
        /// </summary>
        public bool MostrarEnBoletin {get; set;} = true;
        /// <summary>FK al catálogo normalizado <c>sexo</c> (reemplaza la antigua columna de texto).</summary>
        public int? SexoId {get; set;}
        public Sexo? Sexo {get; set;}
        /// <summary>Distrito de residencia (texto no normalizado, a propósito).</summary>
        public string? Distrito {get; set;}
        public string? Direccion {get; set;}
        public int? NumeroHijos {get; set;}
        public int? GradoAcademicoId {get; set;}
        public int? UniversidadId {get; set;}
        public int? ProfesionId {get; set;}
        public int? TallaId {get; set;}
        public DateTime CreatedDateTime {get; set;}
        public int? CreatedUserId {get; set;}
        public DateTime? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}
        /// <summary>Bytes de la firma (PNG) dibujada por esta persona en Configuración.</summary>
        public byte[]? SignatureImageBytes {get; set;}
        public string? SignatureMime {get; set;}
        public DateTimeOffset? SignatureUpdatedDateTime {get; set;}
        public User User { get; set; }
    }
}