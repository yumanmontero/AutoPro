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
    
    public partial class estructura_costo
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public estructura_costo()
        {
            this.vehiculo_tiene_costo = new HashSet<vehiculo_tiene_costo>();
        }
    
        public int id_costo { get; set; }
        public decimal monto { get; set; }
        public System.DateTime fecha { get; set; }
        public byte fk_tipo_costo { get; set; }
    
        public virtual tipo_costo tipo_costo { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<vehiculo_tiene_costo> vehiculo_tiene_costo { get; set; }
    }
}