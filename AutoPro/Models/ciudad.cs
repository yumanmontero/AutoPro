//------------------------------------------------------------------------------
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
    using System.Collections.Generic;
    
    public partial class ciudad
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ciudad()
        {
            this.direccion = new HashSet<direccion>();
        }
    
        public int id_ciudad { get; set; }
        public string nombre { get; set; }
        public System.Data.Entity.Spatial.DbGeography coordenadas { get; set; }
        public int fk_estado { get; set; }
    
        public virtual estado estado { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<direccion> direccion { get; set; }
    }
}
