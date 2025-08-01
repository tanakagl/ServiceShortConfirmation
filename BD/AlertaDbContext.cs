using Microsoft.EntityFrameworkCore;
using AlertaBoletaService.Models;
using AlertaBoletaService.Provider;

namespace AlertaBoletaService.BD
{
    public class AlertaDbContext : DbContext
    {
        public AlertaDbContext()
        {
        }

        public AlertaDbContext(DbContextOptions<AlertaDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<ParametroEmpresa> ParametroEmpresas { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = Configuration.GetValue<string>("ConnectionStrings:OracleConnection");
                optionsBuilder.UseOracle(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasDefaultSchema("OPUS")
                .UseCollation("USING_NLS_COMP");
            
            modelBuilder.Entity<ParametroEmpresa>(entity =>
            {
                entity.HasKey(e => e.IdParamEmpresa);
                entity.ToTable("MV_PARAM_EMPRESA");
                
                entity.Property(e => e.IdParamEmpresa)
                    .HasColumnName("ID_PARAM_EMPRESA")
                    .HasColumnType("NUMBER")
                    .IsRequired();
                
                entity.Property(e => e.IdEmpresa)
                    .HasColumnName("ID_EMPRESA")
                    .HasColumnType("NUMBER")
                    .IsRequired();
                
                entity.Property(e => e.FlagNotificaBoleta)
                    .HasColumnName("IN_NOTIFICA_BOLETA_REAPROVACAO")
                    .HasMaxLength(1)
                    .HasDefaultValue("N");
                    
                entity.Property(e => e.PeriodoHorasBoleta)
                    .HasColumnName("NR_PERIODO_HORAS_BOLETA")
                    .HasColumnType("NUMBER")
                    .HasDefaultValue(24);
                    
                entity.Property(e => e.UltimaExecucaoBoleta)
                    .HasColumnName("DT_ULTIMA_EXECUCAO_BOLETA")
                    .HasColumnType("DATE");
            });
            
            base.OnModelCreating(modelBuilder);
        }
    }
} 