using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;


namespace AutoPro.Models
{
    public class VentaViewModels
    {


    }

    public class PreventaViewModels
    {
        public List<TransaccionesVentaViewModels> Lista_Transacciones { get; set; }

        public ClienteViewModels Cliente { get; set; }

    }


    public class Preventa_MenuViewModels
    {
        public int Cant_Prioritarios { get; set; }

        public int Cant_Pref_Cliente { get; set; }

        public int Cant_Pref_Mercado { get; set; }

        public int Cant_Inventario { get; set; }

        public string ID_Cliente { get; set; }

    }

    public class VehiculosDetallesViewModels
    {
        public vehiculo Vehiculo { get; set; }

        [Required]
        [RegularExpression("^[0-9]*$", ErrorMessage = "UPRN must be numeric")]
        [Display(Name = "Código")]
        [StringLength(11, ErrorMessage = "El número de caracteres de {0} debe ser al menos {2}.", MinimumLength = 1)]
        public string Valor_Venta { set; get; }

        public SelectList Lista_Agentes { get; set; }

        public int id_Financista { get; set; }

        public string Comentario { get; set; }

        public int Nivel_Pref_Cliente { get; set; }

        public string Tiempo_Inventario { get; set; }

        public decimal Comision_Obtener { get; set; }

        public decimal Monto_Minimo { get; set; }

        public decimal Monto_Maximo { get; set; }

        public string Imagen { get; set; }

        public string ID_Cliente { get; set; }

        public int ID_Usuario { get; set; }


    }

    public class Marcas_CategoriaViewModels
    {
        public List<Venta_MarcasViewModels> Lista_Marcas { get; set; }

        public List<Venta_CategoriaViewModels> Lista_Categoria { get; set; }

        public string id_cliente { get; set; }

    }

    public class Venta_VehiculoViewModels
    {
        public List<Venta_ModeloViewModels> Lista_Vehiculos { get; set; }

        public string Titulo { get; set; }

        public string ID_Cliente { get; set; }

    }

    public class Venta_ModeloViewModels
    {
        public vehiculo Vehiculo { get; set; }

        public decimal Monto_Venta { get; set; }

        public string Cant_Dias_Inventario { get; set; }

        public string Imagen { get; set; }

    }

    public class Venta_MarcasViewModels
    {
        public marca Marca { get; set; }

        public int Nro_Inventario { get; set; }

        public int Nivel_Preferencia { get; set; }

    }

    public class Venta_CategoriaViewModels
    {
        public modelo_clasificacion Categoria { get; set; }

        public int Nro_Inventario { get; set; }

        public int Nivel_Preferencia { get; set; }

    }

    public class ClienteViewModels
    {
        public string Nombre_Completo { get; set; }

        public string Nro_Telefono { get; set; }

        public string Email { get; set; }

        public string Direccion { get; set; }

        public string Image { get; set; }

    }

    public class TransaccionesVentaViewModels
    {

        public int Tipo_Transaccion { get; set; }

        public List<vehiculo> Lista_Vehiculo { get; set; }

        public int Cant_Vehiculos { get; set; }

        public decimal Monto_Total { get; set; }

        public string Nombre_Operador { get; set; }

        public decimal Menor_Monto { get; set; }

        public decimal Mayor_Monto { get; set; }

        public string Cant_Tiempo_Transcurrido { get; set; }

        public int id { get; set; }

        public DateTime fecha { get; set; }

    }

}