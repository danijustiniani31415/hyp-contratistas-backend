using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Infrastructure.Repositories {
    public class UserPasswordTokenRepository : IUserPasswordTokenRepository
    {
        private readonly AppDbContext _context;

        public UserPasswordTokenRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(UserPasswordTokenDTO tokenDto)
        {
            var tokenModel = new UserPasswordToken
            {
                UserId = tokenDto.UserId,
                Token = tokenDto.Token,
                CreatedDateTime = tokenDto.CreatedDateTime,
                ExpiresAt = tokenDto.ExpiresAt,
                Used = tokenDto.Used
            };
            await _context.UserPasswordToken.AddAsync(tokenModel);
            await _context.SaveChangesAsync();
        }

        public async Task<UserPasswordTokenDTO?> GetValidTokenAsync(string token)
        {
            var tokenEntity = await _context.UserPasswordToken.Include(t => t.User)
                .FirstOrDefaultAsync(t =>
                t.Token == token &&
                !t.Used &&
                t.ExpiresAt > DateTime.UtcNow
            );
            if (tokenEntity == null)
                return null;

            var dto = new UserPasswordTokenDTO
            {
                UserId = tokenEntity.UserId,
                Token = tokenEntity.Token,
                CreatedDateTime = tokenEntity.CreatedDateTime,
                ExpiresAt = tokenEntity.ExpiresAt,
                Used = tokenEntity.Used
            };
            return dto;
        }

        public async Task InvalidateTokensByUserAsync(int userId)
        {
            var tokens = await _context.UserPasswordToken
                .Where(t => t.UserId == userId && !t.Used)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.Used = true;
            }
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}