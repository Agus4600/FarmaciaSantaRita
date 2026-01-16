using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FarmaciaSantaRita.Models;

public partial class FarmaciabdContext : DbContext
{
    public FarmaciabdContext()
    {
    }

    public FarmaciabdContext(DbContextOptions<FarmaciabdContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Boletum> Boleta { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<Compra> Compras { get; set; }

    public virtual DbSet<Inasistencium> Inasistencia { get; set; }

    public virtual DbSet<LineaDeCompra> LineaDeCompras { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<Proveedor> Proveedors { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public DbSet<Vacacion> Vacaciones { get; set; } // Agrega esto en la clase

    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // {
    //     #warning To protect potentially sensitive information...
    //     => optionsBuilder.UseSqlServer("Server=DESKTOP-HRBS31L\\SQLEXPRESS;Database=FARMACIABD;Trusted_Connection=True;TrustServerCertificate=True;");
    // }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Boletum>(entity =>
        {
            entity.ToTable("Boletas");

            entity.HasKey(e => e.Idboleta);

            entity.Property(e => e.Idboleta).HasColumnName("IDBoleta");
            entity.HasKey(e => e.Idboleta);

            entity.Property(e => e.Idboleta).HasColumnName("IDBoleta");
            entity.Property(e => e.Categoria)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Detalle)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Idproveedor).HasColumnName("IDProveedor");
            entity.Property(e => e.Idusuario).HasColumnName("IDUsuario");
            entity.Property(e => e.Transfer)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdproveedorNavigation).WithMany(p => p.Boleta)
                .HasForeignKey(d => d.Idproveedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Boleta_Proveedor");

            entity.HasOne(d => d.IdusuarioNavigation).WithMany(p => p.Boleta)
                .HasForeignKey(d => d.Idusuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Boleta_Usuarios");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Idcliente);

            entity.Property(e => e.Idcliente).HasColumnName("IDCliente");
            entity.Property(e => e.DireccionCliente).IsUnicode(false);
            entity.Property(e => e.EstadoDePago)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NombreCliente).IsUnicode(false);
            entity.Property(e => e.TelefonoCliente)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Compra>(entity =>
        {
            entity.HasKey(e => e.Idcompras);

            entity.Property(e => e.Idcompras)
                .ValueGeneratedNever()
                .HasColumnName("IDCompras");
            entity.Property(e => e.Descripcion).IsUnicode(false);
            entity.Property(e => e.Idcliente).HasColumnName("IDCliente");
            entity.Property(e => e.IdlineaDeCompra).HasColumnName("IDLineaDeCompra");
            entity.Property(e => e.Idusuario).HasColumnName("IDUsuario");
            entity.Property(e => e.MontoCompra).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdclienteNavigation).WithMany(p => p.Compras)
                .HasForeignKey(d => d.Idcliente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Compras_Clientes");

            entity.HasOne(d => d.IdlineaDeCompraNavigation).WithMany(p => p.Compras)
                .HasForeignKey(d => d.IdlineaDeCompra)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Compras_LineaDeCompra");

            entity.HasOne(d => d.IdusuarioNavigation).WithMany(p => p.Compras)
                .HasForeignKey(d => d.Idusuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Compras_Usuarios");
        });

        modelBuilder.Entity<Inasistencium>(entity =>
        {
            entity.HasKey(e => e.Idinasistencias);

            entity.Property(e => e.Idinasistencias).HasColumnName("IDInasistencias");
            entity.Property(e => e.Idusuario).HasColumnName("IDUsuario");
            entity.Property(e => e.Motivo).IsUnicode(false);
            entity.Property(e => e.NombreEmpleado).IsUnicode(false);

            entity.HasOne(d => d.IdusuarioNavigation).WithMany(p => p.Inasistencia)
                .HasForeignKey(d => d.Idusuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inasistencia_Usuarios");
        });

        modelBuilder.Entity<LineaDeCompra>(entity =>
        {
            entity.HasKey(e => e.IdlineaDeCompra);

            entity.ToTable("LineaDeCompra");

            entity.Property(e => e.IdlineaDeCompra).HasColumnName("IDLineaDeCompra");
            entity.Property(e => e.Idproducto).HasColumnName("IDProducto");

            entity.HasOne(d => d.IdproductoNavigation).WithMany(p => p.LineaDeCompras)
                .HasForeignKey(d => d.Idproducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LineaDeCompra_Producto");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.Idproducto);

            entity.ToTable("Producto");

            entity.Property(e => e.Idproducto).HasColumnName("IDProducto");
            entity.Property(e => e.NombreProducto).IsUnicode(false);
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(18, 0)");
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(e => e.Idproveedor);

            entity.ToTable("Proveedor");

            entity.Property(e => e.Idproveedor).HasColumnName("IDProveedor");
            entity.Property(e => e.CorreoProveedor)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EstadoProveedor)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NombreProveedor)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TelefonoProveedor)
                .HasMaxLength(20)
                .IsUnicode(false);
        });


        modelBuilder.Entity<Inasistencium>(entity =>
        {
            entity.HasKey(e => e.Idinasistencias);

            entity.Property(e => e.Idinasistencias).HasColumnName("IDInasistencias");
            entity.Property(e => e.Idusuario).HasColumnName("IDUsuario");

            // ⭐ AÑADIDO CLAVE 1: Mapeo de la Fecha
            entity.Property(e => e.FechaInasistencia)
                .HasColumnType("date") // O el tipo de dato que uses en SQL (date, datetime2)
                .IsRequired(); // Si es NOT NULL en la BD

            // ⭐ AÑADIDO CLAVE 2: Mapeo del Turno
            entity.Property(e => e.Turno)
                .HasMaxLength(50) // Ajusta esta longitud según tu BD
                .IsUnicode(false)
                .IsRequired(); // Si es NOT NULL en la BD

            entity.Property(e => e.Motivo).IsUnicode(false);
            entity.Property(e => e.NombreEmpleado).IsUnicode(false);

            entity.HasOne(d => d.IdusuarioNavigation).WithMany(p => p.Inasistencia)
                .HasForeignKey(d => d.Idusuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inasistencia_Usuarios");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Idusuario);

            entity.Property(e => e.Idusuario).HasColumnName("IDUsuario");
            entity.Property(e => e.Apellido)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Contraseña)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CorreoUsuario)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Direccion)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Dni)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("DNI");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NombreUsuario).HasMaxLength(50);
            entity.Property(e => e.Rol)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Vacacion>(entity =>
        {
            entity.HasKey(v => v.IdVacaciones);

            entity.HasOne(v => v.Usuario)
                  .WithMany() // ← Sin navegación inversa → no requiere propiedad en Usuario
                  .HasForeignKey(v => v.Idusuario) // ← Nombre exacto
                  .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
