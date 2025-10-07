using MyWebApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MyWebApi.Services
{
    public interface IIdGenerationService
    {
        Task<string> GenerateUserIdAsync();
        Task<string> GenerateCarrierIdAsync();
        Task<string> GenerateOrganizationIdAsync();
        Task<string> GenerateProductIdAsync();
        Task<string> GenerateRuleIdAsync();
        string GenerateId(string prefix);
    }

    public class IdGenerationService : IIdGenerationService
    {
        private readonly AppDbContext _context;

        public IdGenerationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateUserIdAsync()
        {
            var count = await _context.Users.CountAsync();
            return $"usr-{(count + 1):D3}";
        }

        public async Task<string> GenerateCarrierIdAsync()
        {
            var count = await _context.Carriers.CountAsync();
            return $"car-{(count + 1):D3}";
        }

        public async Task<string> GenerateOrganizationIdAsync()
        {
            var count = await _context.Users.CountAsync();
            return $"org-{(count + 1):D3}";
        }

        public async Task<string> GenerateProductIdAsync()
        {
            var count = await _context.Products.CountAsync();
            return $"prod-{(count + 1):D3}";
        }

        public async Task<string> GenerateRuleIdAsync()
        {
            var count = await _context.Rules.CountAsync();
            return $"rul-{(count + 1):D3}";
        }

        public string GenerateId(string prefix)
        {
            return $"{prefix}-{Guid.NewGuid().ToString("N")[..8]}";
        }
    }
}
