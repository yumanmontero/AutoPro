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
    
    public partial class vehiculo_tiene_costo
    {
        public int fk_vehiculo { get; set; }
        public int fk_costo { get; set; }
        public decimal gasto { get; set; }
    
        public virtual estructura_costo estructura_costo { get; set; }
        public virtual vehiculo vehiculo { get; set; }
    }
}