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
        public int id_costo { get; set; }
        public decimal monto { get; set; }
        public System.DateTime fecha { get; set; }
        public byte fk_tipo_costo { get; set; }
        public int fk_vehiculo { get; set; }
    
        public virtual tipo_costo tipo_costo { get; set; }
        public virtual vehiculo vehiculo { get; set; }
    }
}
