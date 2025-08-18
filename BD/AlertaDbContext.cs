using Microsoft.EntityFrameworkCore;
using AlertaBoletaService.Models;

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
        public DbSet<ParametroBoletaEmail> ParametroBoletaEmails { get; set; }

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
                    
                entity.Property(e => e.UltimaExecucaoBoleta)
                    .HasColumnName("DT_ULTIMA_EXECUCAO_BOLETA")
                    .HasColumnType("DATE");
            });
            
            modelBuilder.Entity<ParametroBoletaEmail>(entity =>
            {
                entity.HasKey(e => e.IdParamBoletasEmail);
                entity.ToTable("MV_PARAM_BOLETAS_EMAIL");
                
                entity.Property(e => e.IdParamBoletasEmail)
                    .HasColumnName("ID_PARAM_BOLETAS_EMAIL")
                    .HasColumnType("NUMBER")
                    .IsRequired();
                
                entity.Property(e => e.IdEmpresa)
                    .HasColumnName("ID_EMPRESA")
                    .HasColumnType("NUMBER");
                
                entity.Property(e => e.DsEmail)
                    .HasColumnName("DS_EMAIL")
                    .HasMaxLength(255);
            });
            
            base.OnModelCreating(modelBuilder);
        }
    }
} 