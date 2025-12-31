using ResolveAi.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ResolveAi.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Problema> Problemas => Set<Problema>();
        public DbSet<Proposta> Propostas => Set<Proposta>();
    }
}
