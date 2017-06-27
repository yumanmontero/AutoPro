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

    public class MenuBotonViewModels
    {
        public int tipo { get; set; }

        public string cliente { get; set; }

    }
    public class PreventaViewModels
    {
        public List<TransaccionesVentaViewModels> Lista_Transacciones { get; set; }

        public ClienteViewModels Cliente { get; set; }

        public O_Transaccion_VentaViewModels Transaccion { get; set; }

    }

    public class HistorialComisionViewModels
    {
        public decimal Monto_Total { get; set; }

        public int Total_Ventas { get; set; }

        public DateTime Fecha_Inicial { get; set; }

        public DateTime Fecha_Final { get; set; }

        public List<TrasaccionComisionViewModels> Lista_Transaccion { get; set; }

        public List<ComisionGraficoViewModels> Lista_Comision_Grafico { get; set; }

        public int Tipo_Comision { get; set; }

        public int Valor_Comision_Fijo { get; set; }

        public decimal Comision_30_dias { get; set; }

        public decimal Comision_3_Mes { get; set; }

        public decimal Comision_1_Año { get; set; }

        public decimal Comision_Total { get; set; }






    }

    public class ComisionGraficoViewModels
    {
        public int Monto_Acumulado { get; set; }

        public string Fecha_F { get; set; }

    }

    public class TrasaccionComisionViewModels
    {
        public decimal Monto_Total { get; set; }

        public int id { get; set; }

        public DateTime Fecha { get; set; }

        public List<VehiculoComisionViewModels> Lista_Vehiculos { get; set; }



    }

    public class VehiculoComisionViewModels
    {
        public string Nombre { get; set; }

        public string Marca { get; set; }

        public int Año { get; set; }

        public string Imagen { get; set; }

        public decimal Comision { get; set; }

        public int id { get; set; }

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

    public class BoxClienteViewModels
    {
        public List<Cliente_ItemBoxViewModels> Lista_Cliente { get; set; }

        public int Total_Clientes { set; get; }

    }

    public class Cliente_ItemBoxViewModels
    {

        public string id { set; get; }

        public string Image { set; get; }

        public string Nombre { set; get; }

        public string Descripcion { get; set; }

        public int Nro_Preventas { get; set; }

        public int Nro_Ventas { get; set; }

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

        public string id { get; set; }

    }

    public class ConsultarClienteViewModels
    {

        public int Nro_Cliente { set; get; }

        [Display(Name = "Modelo")]
        public int Busq_Cliente { get; set; }

        public List<O_Transaccion_VentaViewModels> Transaccion { get; set; }

        public RegistroClienteViewModel Modelo_Registro { get; set; }

    }

    public class RegistroClienteViewModel
    {
        [Required]
        [RegularExpression(@"^\d+$", ErrorMessage = "El Formato del Numero no es correcto, No incluya () o [] o --")]
        [StringLength(100, ErrorMessage = "El número de caracteres de {0} debe ser al menos {2}.", MinimumLength = 8)]
        [Display(Name = "Numero de Telf.")]
        public string NumeroTlf { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "El número de caracteres de {0} debe ser al menos {2}.", MinimumLength = 3)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "El número de caracteres de {0} debe ser al menos {2}.", MinimumLength = 3)]
        [Display(Name = "Identificación")]
        public string ID_Cliente { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "El número de caracteres de {0} debe ser al menos {2}.", MinimumLength = 3)]
        [Display(Name = "Apellido")]
        public string Apellido { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "El número de caracteres de {0} debe ser al menos {2}.", MinimumLength = 8)]
        [Display(Name = "Direccion Detallada")]
        public string DireccionDetallada { get; set; }

        public int localidad { get; set; }
        //public List<System.Web.Mvc.SelectListItem> DropPais { get; set; }
        //public List<System.Web.Mvc.SelectListItem> DropEstado { get; set; }
        //public List<System.Web.Mvc.SelectListItem> DropLocalidad { get; set; }
    }

    public class O_Transaccion_VentaViewModels
    {
        public transaccion_venta Transaccion { get; set; }

        public decimal Monto_Total { get; set; }

        public int Cant_Vehiculo { get; set; }

        public List<vehiculo> Lista_Vehiculo { get; set; }

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
    public class RegistroPais
    {
        public int idPais { get; set; }
        public string Nombre { get; set; }

    }

    public class RegitroEstado
    {
        public int idEstado { get; set; }
        public string Nombre { get; set; }
    }

    public class RegistroLocalidad
    {
        public int idLocalidad { get; set; }
        public string Nombre { get; set; }

    }


}