using Microsoft.EntityFrameworkCore;
using Talle_DW.Entities;

namespace Talle_DW.Data;

/// <summary>
/// Mapeo de la base de datos TallerDW (creada con BaseDeDatos/TallerDW.sql).
/// </summary>
public class TallerDbContext(DbContextOptions<TallerDbContext> options) : DbContext(options)
{
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Modulo> Modulos => Set<Modulo>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Parametro> Parametros => Set<Parametro>();
    public DbSet<RegistroBitacora> Bitacora => Set<RegistroBitacora>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<OrdenTrabajo> Ordenes => Set<OrdenTrabajo>();
    public DbSet<HistorialOrden> HistorialOrdenes => Set<HistorialOrden>();
    public DbSet<Diagnostico> Diagnosticos => Set<Diagnostico>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Repuesto> Repuestos => Set<Repuesto>();
    public DbSet<MovimientoInventario> Movimientos => Set<MovimientoInventario>();
    public DbSet<SolicitudRepuesto> Solicitudes => Set<SolicitudRepuesto>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<DetalleFactura> DetallesFactura => Set<DetalleFactura>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Rol>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(x => x.RolId);
        });

        mb.Entity<Modulo>(e =>
        {
            e.ToTable("Modulos");
            e.HasKey(x => x.ModuloId);
        });

        mb.Entity<RolPermiso>(e =>
        {
            e.ToTable("RolPermisos");
            e.HasKey(x => new { x.RolId, x.ModuloId });
            e.HasOne(x => x.Rol).WithMany(r => r.Permisos).HasForeignKey(x => x.RolId);
            e.HasOne(x => x.Modulo).WithMany().HasForeignKey(x => x.ModuloId);
        });

        mb.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.HasKey(x => x.UsuarioId);
            e.HasOne(x => x.Rol).WithMany(r => r.Usuarios).HasForeignKey(x => x.RolId);
        });

        mb.Entity<Parametro>(e =>
        {
            e.ToTable("Parametros");
            e.HasKey(x => x.ParametroId);
        });

        mb.Entity<RegistroBitacora>(e =>
        {
            e.ToTable("Bitacora");
            e.HasKey(x => x.BitacoraId);
        });

        mb.Entity<Cliente>(e =>
        {
            e.ToTable("Clientes");
            e.HasKey(x => x.ClienteId);
        });

        mb.Entity<Vehiculo>(e =>
        {
            e.ToTable("Vehiculos");
            e.HasKey(x => x.VehiculoId);
            e.HasOne(x => x.Cliente).WithMany(c => c.Vehiculos).HasForeignKey(x => x.ClienteId);
        });

        mb.Entity<OrdenTrabajo>(e =>
        {
            e.ToTable("OrdenesTrabajo");
            e.HasKey(x => x.OrdenId);
            e.Property(x => x.NumeroOrden)
                .HasComputedColumnSql("('OT-' + RIGHT('000000' + CAST(OrdenId AS VARCHAR(10)), 6))", stored: true);
            e.HasOne(x => x.Vehiculo).WithMany(v => v.Ordenes).HasForeignKey(x => x.VehiculoId);
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId);
            e.HasOne(x => x.Recepcionista).WithMany().HasForeignKey(x => x.RecepcionistaId);
            e.HasOne(x => x.Mecanico).WithMany().HasForeignKey(x => x.MecanicoId);
            e.HasOne(x => x.UsuarioSalida).WithMany().HasForeignKey(x => x.UsuarioSalidaId);
        });

        mb.Entity<HistorialOrden>(e =>
        {
            e.ToTable("HistorialOrden");
            e.HasKey(x => x.HistorialId);
            e.HasOne(x => x.Orden).WithMany(o => o.Historial).HasForeignKey(x => x.OrdenId);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });

        mb.Entity<Diagnostico>(e =>
        {
            e.ToTable("Diagnosticos");
            e.HasKey(x => x.DiagnosticoId);
            e.Property(x => x.CostoManoObra).HasPrecision(12, 2);
            e.HasOne(x => x.Orden).WithMany(o => o.Diagnosticos).HasForeignKey(x => x.OrdenId);
            e.HasOne(x => x.Mecanico).WithMany().HasForeignKey(x => x.MecanicoId);
        });

        mb.Entity<Categoria>(e =>
        {
            e.ToTable("Categorias");
            e.HasKey(x => x.CategoriaId);
        });

        mb.Entity<Repuesto>(e =>
        {
            e.ToTable("Repuestos");
            e.HasKey(x => x.RepuestoId);
            e.Property(x => x.PrecioCosto).HasPrecision(12, 2);
            e.Property(x => x.PrecioVenta).HasPrecision(12, 2);
            e.HasOne(x => x.Categoria).WithMany(c => c.Repuestos).HasForeignKey(x => x.CategoriaId);
        });

        mb.Entity<MovimientoInventario>(e =>
        {
            e.ToTable("MovimientosInventario");
            e.HasKey(x => x.MovimientoId);
            e.HasOne(x => x.Repuesto).WithMany().HasForeignKey(x => x.RepuestoId);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });

        mb.Entity<SolicitudRepuesto>(e =>
        {
            e.ToTable("SolicitudesRepuesto");
            e.HasKey(x => x.SolicitudId);
            e.Property(x => x.PrecioUnitario).HasPrecision(12, 2);
            e.HasOne(x => x.Orden).WithMany(o => o.Solicitudes).HasForeignKey(x => x.OrdenId);
            e.HasOne(x => x.Repuesto).WithMany(r => r.Solicitudes).HasForeignKey(x => x.RepuestoId);
            e.HasOne(x => x.Solicitante).WithMany().HasForeignKey(x => x.SolicitanteId);
            e.HasOne(x => x.Bodeguero).WithMany().HasForeignKey(x => x.BodegueroId);
        });

        mb.Entity<Factura>(e =>
        {
            e.ToTable("Facturas");
            e.HasKey(x => x.FacturaId);
            e.Property(x => x.NumeroFactura)
                .HasComputedColumnSql("('FAC-' + RIGHT('000000' + CAST(FacturaId AS VARCHAR(10)), 6))", stored: true);
            e.Property(x => x.SubtotalManoObra).HasPrecision(12, 2);
            e.Property(x => x.SubtotalRepuestos).HasPrecision(12, 2);
            e.Property(x => x.Descuento).HasPrecision(12, 2);
            e.Property(x => x.Total).HasPrecision(12, 2);
            e.Property(x => x.PorcentajeImpuesto).HasPrecision(5, 2);
            e.Property(x => x.BaseImponible).HasPrecision(12, 2);
            e.Property(x => x.Impuesto).HasPrecision(12, 2);
            e.HasOne(x => x.Orden).WithMany(o => o.Facturas).HasForeignKey(x => x.OrdenId);
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });

        mb.Entity<DetalleFactura>(e =>
        {
            e.ToTable("DetalleFactura");
            e.HasKey(x => x.DetalleId);
            e.Property(x => x.PrecioUnitario).HasPrecision(12, 2);
            e.Property(x => x.Subtotal).HasPrecision(12, 2);
            e.HasOne(x => x.Factura).WithMany(f => f.Detalles).HasForeignKey(x => x.FacturaId);
        });
    }
}
