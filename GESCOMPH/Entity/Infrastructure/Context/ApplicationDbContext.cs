using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.Domain.Models.Implements.Business;
using Entity.Domain.Models.Implements.Persons;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.Domain.Models.Implements.Utilities;
using Entity.DTOs.Implements.SecurityAuthentication.Auth.RestPasword;
using Entity.Infrastructure.Configurations.SecurityAuthentication;
using Microsoft.EntityFrameworkCore;

namespace Entity.Infrastructure.Context
{
    /// <summary>
    /// Contexto principal de la aplicación.
    /// Gestiona las entidades del dominio y aplica las configuraciones de EF Core.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Aplica un filtro global para excluir imágenes marcadas como eliminadas.
            modelBuilder.Entity<Images>().HasQueryFilter(img => !img.IsDeleted);

            // Aplica automáticamente todas las configuraciones de entidades (IEntityTypeConfiguration<T>).
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
        }

        // ==========================
        //       Seguridad
        // ==========================
        public DbSet<User> Users { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<RolUser> RolUsers { get; set; }
        public DbSet<RolFormPermission> RolFormPermissions { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<PasswordResetCode> PasswordResetCodes { get; set; }

        // ==========================
        //   Administración del sistema
        // ==========================
        public DbSet<Form> Forms { get; set; }
        public DbSet<FormModule> FormModules { get; set; }
        public DbSet<Domain.Models.Implements.AdministrationSystem.Module> Modules { get; set; }

        // ==========================
        //       Personas
        // ==========================
        public DbSet<Person> Persons { get; set; }

        // ==========================
        //       Negocio
        // ==========================
        public DbSet<Plaza> Plazas { get; set; }
        public DbSet<Establishment> Establishments { get; set; }
        public DbSet<Images> Images { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        // ==========================
        //       Parámetros del sistema
        // ==========================
        public DbSet<SystemParameter> SystemParameters { get; set; }

        // ==========================
        //       Contratos
        // ==========================
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<PremisesLeased> PremisesLeaseds { get; set; }
        public DbSet<Clause> Clauses { get; set; }
        public DbSet<ContractClause> ContractClauses { get; set; }
        public DbSet<ObligationMonth> ObligationMonths { get; set; }
    }
}
