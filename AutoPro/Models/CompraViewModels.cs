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

        public int id { set; get; }

        public string Image { set; get; }

        public string Nombre { set; get; }

        public string Descripcion { get; set; }

        public int Nro_Inventario { get; set; }

        public int Nro_Vendidos { get; set; }

        public int Nro_Rezagados { get; set; }

    }

    public class ListaModelosViewModels
    {
        public IEnumerable<ModelosDetallesViewModels> Lista_Modelo { set; get; }

        public int Total_Modelos { get; set; }
    }

    public class ModeloDestallesViewModels
    {

        public int id { set; get; }

        public float Valor { set; get; }

        public string Nombre { get; set; }

        public string Marca { set; get; }

        public string Modelo { set; get; }

        public int Año { set; get; }

        public bool Preferencia_Banco { set; get; }

        public float Valor_Banco { set; get; }

        public int Nivel_Pref_Banco { get; set; }

        public float Viabilidad { set; get; }

        public int Estado_Vehiculo { set; get; }

        public string Imagen { set; get; }

        public float Valor_Calculado_Maximo { set; get; }

        public float Valor_Calculado_Minimo { get; set; }

        public int Nivel_Pref_Cliente { get; set; }

        public float Rentabilidad { set; get; }

        public int Tiempo_Inventario { get; set; }

        public int Nro_Inventario { get; set; }

        public int Nro_Vendidos { get; set; }

        public int Nro_Rezagados { get; set; }

        
    }

}