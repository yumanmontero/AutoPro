using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace AutoPro.Models
{
    public class AdministracionViewModels
    {
    }


    public class ReporteInventarioViewModels
    {

        public List<ReporteItemString_RezagadoViewModels> Lista_Modelo { get; set; }

        public List<ReporteItemString_RezagadoViewModels> Lista_Categoria { get; set; }

        public List<ReporteItemString_RezagadoViewModels> Lista_Color { get; set; }

        public List<ReporteItemString_RezagadoViewModels> Lista_Año { get; set; }

        public List<ReporteItemStringViewModels> Lista_Cant_Tiempo { get; set; }

        public List<ReporteItemString_RezagadoViewModels> Lista_Costo { get; set; }

        public int Cant_Total_Vehiculos { get; set; }

        public int Cant_Vehiculos_Rezagados { get; set; }

        public int Cant_Vehiculos_Entraron { get; set; }

        public int Cant_Vehiculos_Salienron { get; set; }

        public List<ReporteItemString_RezagadoViewModels> Lista_Marca_Inv_Rez { get; set; }

        public string Rango_de_Consulta { get; set; }

    }

    public class ReporteVentasViewModels
    {
        public List<ReporteItemProfitViewModels> Lista_Venta_Por_Dia { get; set; }

        public List<ReporteItemProfitViewModels> Lista_Ganancia_Neta_Venta { get; set; }

        public List<ReporteItemProfitViewModels> Lista_Ganancia_Neta_Dia { get; set; }

        public int Cantida_Venta_Mes { get; set; }

        public decimal Ganancia_Neta_Total { get; set; }

        public decimal Ganancia_promedio_vehiculo { get; set; }

        public string Mes_Año { get; set; }




    }

    public class ReporteEmpleadosViewModels
    {
        public int Vendedor_Venta_Total { get; set; }

        public decimal Vendedor_Comision_Total { get; set; }

        public decimal Financista_Comision_Total { get; set; }

        public int Financista_Venta_Total { get; set; }

        public int Comprador_Compra_Total { get; set; }

        public decimal Monto_Total_Compra { get; set; }

        public List<ReporteEmpleado_ItemViewModels> Lista_Top_Compradores { get; set; }

        public List<ReporteEmpleado_ItemViewModels> Lista_Top_Vendedores { get; set; }

        public List<ReporteEmpleado_ItemViewModels> Lista_Top_Financista { get; set; }

        public string Rango_de_Consulta { get; set; }

    }

    public class ReporteItemStringViewModels
    {
        public string Nombre { get; set; }

        public int Cantidad { get; set; }

    }

    public class ReporteEmpleado_ItemViewModels
    {
        public string Nombre { get; set; }

        public int Cant_Ventas {get;set;}

        public decimal Monto_Total { get; set; }



    }

    public class ReporteItemString_RezagadoViewModels
    {
        public string Nombre { get; set; }

        public int Cantidad_Inv { get; set; }

        public int Cantidad_Rez { get; set; }

    }

    public class ReporteItemProfitViewModels
    {
        public string Nombre { get; set; }

        public int Cantidad_Ventas { get; set; }

        public decimal Porcentaje { get; set; }

    }

}