using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Infrastructure.Repositories {
    public class PersonRepository {
        private readonly AppDbContext _context;
        private readonly IDbContextFactory<AppDbContext> _factory;
        public PersonRepository(AppDbContext contexto, IDbContextFactory<AppDbContext> factory) {
            _context = contexto;
            _factory = factory;
        }
        /*
        public async Task<List<Person>> GetAll() {
            var registros = from item in _context.Person select item;
            return await registros.ToListAsync();
        }
        public async Task<List<PersonDTO>> GetAllFactory()
        {
            using var ctx = _factory.CreateDbContext();
            var registros = ctx.Person
                .OrderBy(item => item.PersonDescription)
                .Select(item => new PersonDTO
                {
                    PersonId = item.PersonId,
                    PersonDescription = item.PersonDescription,

                    CreatedDateTime = item.CreatedDateTime,
                    CreatedPersonId = item.CreatedPersonId,
                    UpdatedDateTime = item.UpdatedDateTime,
                    UpdatedPersonId = item.UpdatedPersonId,
                    Active = item.Active
                });
            return await registros.ToListAsync();
        }
        */
        /*
        public async Task<object> CreateFactory(PersonCreateDTO dto)
        {
            var exists = await _context.Person.AnyAsync(p => p.DocumentIdentityCode == dto.DocumentIdentityCode);

            if (exists)
                return null;

            var person = new Person
            {
                DocumentIdentityTypeId = dto.DocumentIdentityTypeId,
                DocumentIdentityCode = dto.DocumentIdentityCode,
                FirstName = dto.FirstName,
                SecondName = dto.SecondName,
                FirstLastName = dto.FirstLastName,
                SecondLastName = dto.SecondLastName,
                FullName = dto.FullName,
                Email = dto.Email,
                Active = dto.Active,
                State = true,
                CreatedDateTime = DateTime.UtcNow,
                CreatedPersonId = 1
            };

            _context.Person.Add(person);
            await _context.SaveChangesAsync();

            return person;
        }
         */
    }
}