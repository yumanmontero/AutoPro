﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Este código se generó a partir de una plantilla.
//
//     Los cambios manuales en este archivo pueden causar un comportamiento inesperado de la aplicación.
//     Los cambios manuales en este archivo se sobrescribirán si se regenera el código.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AutoPro.Models
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class sgcaEntities : DbContext
    {
        public sgcaEntities()
            : base("name=sgcaEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<AspNetRoles> AspNetRoles { get; set; }
        public virtual DbSet<AspNetUserClaims> AspNetUserClaims { get; set; }
        public virtual DbSet<AspNetUserLogins> AspNetUserLogins { get; set; }
        public virtual DbSet<AspNetUsers> AspNetUsers { get; set; }
        public virtual DbSet<banco> banco { get; set; }
        public virtual DbSet<banco_financia_modelo> banco_financia_modelo { get; set; }
        public virtual DbSet<ciudad> ciudad { get; set; }
        public virtual DbSet<cliente> cliente { get; set; }
        public virtual DbSet<concesionario> concesionario { get; set; }
        public virtual DbSet<direccion> direccion { get; set; }
        public virtual DbSet<estado> estado { get; set; }
        public virtual DbSet<estado_transaccion> estado_transaccion { get; set; }
        public virtual DbSet<estructura_comision> estructura_comision { get; set; }
        public virtual DbSet<estructura_costo> estructura_costo { get; set; }
        public virtual DbSet<marca> marca { get; set; }
        public virtual DbSet<mensaje> mensaje { get; set; }
        public virtual DbSet<modelo> modelo { get; set; }
        public virtual DbSet<modelo_clasificacion> modelo_clasificacion { get; set; }
        public virtual DbSet<modulos_sistema> modulos_sistema { get; set; }
        public virtual DbSet<pais> pais { get; set; }
        public virtual DbSet<tipo_costo> tipo_costo { get; set; }
        public virtual DbSet<tipo_transaccion_venta> tipo_transaccion_venta { get; set; }
        public virtual DbSet<tipo_usuario> tipo_usuario { get; set; }
        public virtual DbSet<transaccion_compra> transaccion_compra { get; set; }
        public virtual DbSet<transaccion_venta> transaccion_venta { get; set; }
        public virtual DbSet<usuario> usuario { get; set; }
        public virtual DbSet<usuario_estado> usuario_estado { get; set; }
        public virtual DbSet<usuario_gana_comision> usuario_gana_comision { get; set; }
        public virtual DbSet<usuario_tiene_mensaje> usuario_tiene_mensaje { get; set; }
        public virtual DbSet<vehiculo> vehiculo { get; set; }
        public virtual DbSet<vehiculo_estado> vehiculo_estado { get; set; }
    }
}
