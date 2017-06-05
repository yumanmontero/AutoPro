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

        public decimal Valor { set; get; }

        public string Nombre { get; set; }

        public string Marca { set; get; }

        public string Modelo { set; get; }

        public int Año { set; get; }

        public bool Preferencia_Banco { set; get; }

        public int Estado_Vehiculo { set; get; }

        public string Imagen { set; get; }

        public double Valor_Calculado_Maximo { set; get; }

        public double Valor_Calculado_Minimo { get; set; }

        public double Nivel_Pref_Cliente { get; set; }

        public double Rentabilidad { set; get; }

        public int Tiempo_Inventario { get; set; }

        public int Nro_Inventario { get; set; }

        public int Nro_Vendidos { get; set; }

        public int Nro_Rezagados { get; set; }

        public List<BancoFinanciaModeloViewModels> Lista_Banco { get; set; }

        public List<RentabilidadViewModels> Lista_Rentabilidad { get; set; }

        
    }

    public class RentabilidadViewModels
    {
        public string Fecha { set; get; }

        public double Valor_Compra { get; set; }

        public double Valor_Venta { get; set; }

    }

    public class BancoFinanciaModeloViewModels
    {

        public int id_Banco { set; get; }

        public int id_Modelo { set; get; }

        public string Banco { set; get; }

        public decimal Valor_financia { get; set; }

        public int Preferencia { get; set; }
    }

}