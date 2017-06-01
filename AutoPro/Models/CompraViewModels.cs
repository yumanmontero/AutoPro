using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;


namespace AutoPro.Models
{
    public class CompraViewModels
    {
    }


    public class BusquedaPorModeloViewModels
    {

        public int Nro_Marca { set; get; }

        public int Nro_Categoria { set; get; }

        public int Nro_Inventario { get; set; }

        [Required]
        [StringLength(25, ErrorMessage = "El número de caracteres de {0} debe ser al menos {2}.", MinimumLength = 3)]
        [Display(Name = "Modelo")]
        public int Busq_Modelo { get; set; }
    }

    public class ModelosDetallesViewModels
    {

        public int Id { set; get; }

        public string Image { set; get; }

        public string Nombre { set; get; }

        public string Descripcion { get; set; }

        public int Nro_Inventario { get; set; }

        public int Nro_Vendidos { get; set; }

        public int Nro_Rezagados { get; set; }
    }

}