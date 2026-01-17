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
        base.OnModelCreating(modelBuilder);

        // Forzamos TODAS las tablas con su nombre exacto (mayúscula y comillas)
        modelBuilder.Entity<Boletum>().ToTable("Boleta");
        modelBuilder.Entity<Cliente>().ToTable("Clientes");
        modelBuilder.Entity<Compra>().ToTable("Compras");  // ← Clave para tu error actual
        modelBuilder.Entity<Inasistencium>().ToTable("Inasistencia");
        modelBuilder.Entity<LineaDeCompra>().ToTable("LineaDeCompra");
        modelBuilder.Entity<Producto>().ToTable("Producto");
        modelBuilder.Entity<Proveedor>().ToTable("Proveedor");
        modelBuilder.Entity<Usuario>().ToTable("Usuarios");
        modelBuilder.Entity<Vacacion>().ToTable("Vacaciones");

        modelBuilder.Entity<Boletum>(entity =>
        {
            entity.ToTable("Boleta");

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
            entity.ToTable("Clientes");

            entity.HasKey(e => e.Idcliente);
            entity.Property(e => e.Idcliente).HasColumnName("IDCliente");

            entity.Property(e => e.NombreCliente).HasColumnName("NombreCliente");
            entity.Property(e => e.TelefonoCliente).HasColumnName("TelefonoCliente");  // Sin acento ni espacio si en BD es así
            entity.Property(e => e.DireccionCliente).HasColumnName("DirecciónCliente");  // ← ¡Aquí! Sin espacio
            entity.Property(e => e.EstadoDePago).HasColumnName("EstadoPago");
            entity.Property(e => e.DNI).HasColumnName("DNI");
        });

        modelBuilder.Entity<Compra>(entity =>
        {
            entity.ToTable("Compras");

            entity.HasKey(e => e.Idcompras);
            entity.Property(e => e.Idcompras).HasColumnName("IDCompras");

            entity.Property(e => e.Descripcion).IsUnicode(false);
            entity.Property(e => e.MontoCompra).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.FechaCompra).HasColumnType("date");
            entity.Property(e => e.EstadoDePago).HasMaxLength(20).IsUnicode(false);

            // Columna física de la FK (esto es lo que EF no está viendo)
            entity.Property(e => e.Idcliente).HasColumnName("IDCliente");
            entity.Property(e => e.Idusuario).HasColumnName("IDUsuario");

            // Relación con Cliente - FUERZA la columna real "IDCliente"
            entity.HasOne(d => d.IdclienteNavigation)
                  .WithMany(p => p.Compras)
                  .HasForeignKey(d => d.Idcliente)  // ← Usa la propiedad C# Idcliente
                  .HasConstraintName("FK_Compras_Clientes")
                  .OnDelete(DeleteBehavior.ClientSetNull);

            // Relación con Usuario
            entity.HasOne(d => d.IdusuarioNavigation)
                  .WithMany(p => p.Compras)
                  .HasForeignKey(d => d.Idusuario)
                  .HasConstraintName("FK_Compras_Usuarios")
                  .OnDelete(DeleteBehavior.ClientSetNull);

            // Relación con líneas de compra (si la tenés)
            entity.HasMany(c => c.LineaDeCompras)
                  .WithOne(l => l.IdcomprasNavigation)
                  .HasForeignKey(l => l.Idcompras)
                  .OnDelete(DeleteBehavior.Cascade);
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
            entity.ToTable("LineaDeCompra");

            entity.HasKey(e => e.Idlineadecompra);
            entity.Property(e => e.Idlineadecompra).HasColumnName("IDLineaDeCompra");

            // FK a Compra (columna real en BD)
            entity.Property(e => e.Idcompras).HasColumnName("IDCompras");

            // FK a Producto (columna real en BD)
            entity.Property(e => e.Idproducto).HasColumnName("IDProducto");

            entity.Property(e => e.Cantidad);

            // Relación con Compra (inversa)
            entity.HasOne(l => l.IdcomprasNavigation)
                  .WithMany(c => c.LineaDeCompras)
                  .HasForeignKey(l => l.Idcompras)  // ← Usa propiedad C# Idcompras
                  .OnDelete(DeleteBehavior.Cascade);

            // Relación con Producto - FUERZA la columna real "IDProducto"
            entity.HasOne(l => l.IdproductoNavigation)
                  .WithMany(p => p.LineaDeCompras)
                  .HasForeignKey(l => l.Idproducto)  // ← Esto evita que EF invente "IdproductoNavigationIdproducto"
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_LineaDeCompra_Producto");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("Producto");
            entity.HasKey(e => e.Idproducto);
            entity.Property(e => e.Idproducto).HasColumnName("IDProducto");
            entity.Property(e => e.NombreProducto).HasColumnName("NombreProducto");
            entity.Property(e => e.PrecioUnitario).HasColumnName("PrecioUnitario");
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
                  .WithMany() 
                  .HasForeignKey(v => v.Idusuario) 
                  .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
