using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using AutoPro.Models;
using System.Data;
using System.Data.Entity;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;


namespace AutoPro.Controllers
{
    public class AdministracionController : Controller
    {

        private sgcaEntities autodb = new sgcaEntities(); //conexcion con bd
        
        // GET: Administracion
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Reporte()
        {


            return View();
        }

        public ActionResult Inventario()
        {

            return View();
        }

        [Authorize]
        public ActionResult Prueba_Reporte_Inventario(string fecha_inicio, string fecha_fin)
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            var consecionario = (from l_concesio in autodb.concesionario where l_concesio.id_concesionario == usuario.id_Concesionario select new { l_concesio.dia_max_inventario, l_concesio.id_concesionario }).First();

            ReporteInventarioViewModels modelo = new ReporteInventarioViewModels();

            List<ReporteItemString_RezagadoViewModels> lista_modelo = new List<ReporteItemString_RezagadoViewModels>();
            List<ReporteItemString_RezagadoViewModels> lista_categoria = new List<ReporteItemString_RezagadoViewModels>();
            List<ReporteItemString_RezagadoViewModels> lista_color = new List<ReporteItemString_RezagadoViewModels>();
            List<ReporteItemString_RezagadoViewModels> lista_año = new List<ReporteItemString_RezagadoViewModels>();
            List<ReporteItemStringViewModels> lista_cant_tiempo = new List<ReporteItemStringViewModels>();

            List<ReporteItemString_RezagadoViewModels> lista_costo = new List<ReporteItemString_RezagadoViewModels>();
            List<ReporteItemString_RezagadoViewModels> lista_marca_inv_rez = new List<ReporteItemString_RezagadoViewModels>();


            DateTime fecha_ingresada_incio = new DateTime();
            DateTime fecha_ingresada_fin = new DateTime();
            if (fecha_inicio != null)
            {
                fecha_ingresada_incio = Convert.ToDateTime(fecha_inicio);
                fecha_ingresada_fin = Convert.ToDateTime(fecha_fin);
            }
            else
            {
                fecha_ingresada_incio = DateTime.Now;
                fecha_ingresada_fin = DateTime.Now;
            }


            var vehiculos = from l_vehiculo in autodb.vehiculo where l_vehiculo.fk_concesionario == usuario.id_Concesionario && l_vehiculo.fecha_ingreso <= fecha_ingresada_fin && ((l_vehiculo.fecha_salida.HasValue && l_vehiculo.fecha_salida.Value >= fecha_ingresada_incio) || (l_vehiculo.fecha_salida == null)) select l_vehiculo;

            if (vehiculos.Any())
            {

                //Consulta Marca Inv Rez
                var group_marcas_inv_rez = vehiculos.GroupBy(x => x.modelo.fk_marca);
                foreach (var item_marcas in group_marcas_inv_rez)
                {
                    ReporteItemString_RezagadoViewModels item_marc = new ReporteItemString_RezagadoViewModels
                    {
                        Cantidad_Inv = item_marcas.Count(),
                        Nombre = item_marcas.First().modelo.marca.nombre,
                        Cantidad_Rez = item_marcas.Count(x => CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) >= consecionario.dia_max_inventario)
                    };
                    lista_marca_inv_rez.Add(item_marc);
                }

                //Group Modelo Inv Rez
                var group_modelo = vehiculos.GroupBy(x => x.fk_modelo);
                foreach (var item_grouped in group_modelo)
                {
                    ReporteItemString_RezagadoViewModels item = new ReporteItemString_RezagadoViewModels
                    {
                        Cantidad_Inv = item_grouped.Count(),
                        Nombre = item_grouped.First().modelo.nombre,
                        Cantidad_Rez = item_grouped.Count(x => DateTime.Now.Subtract(x.fecha_ingreso).TotalDays >= consecionario.dia_max_inventario)
                    };
                    lista_modelo.Add(item);
                }

                //Group Categoria
                var group_categoria = vehiculos.GroupBy(x => x.modelo.fk_clasificacion);
                foreach (var item_grouped in group_categoria)
                {
                    ReporteItemString_RezagadoViewModels item = new ReporteItemString_RezagadoViewModels
                    {
                        Cantidad_Inv = item_grouped.Count(),
                        Nombre = item_grouped.First().modelo.modelo_clasificacion.descripcion,
                        Cantidad_Rez = item_grouped.Count(x => DateTime.Now.Subtract(x.fecha_ingreso).TotalDays >= consecionario.dia_max_inventario)
                    };
                    lista_categoria.Add(item);
                }

                //Group Color
                var group_color = vehiculos.GroupBy(x => x.color);
                foreach (var item_grouped in group_color)
                {
                    ReporteItemString_RezagadoViewModels item = new ReporteItemString_RezagadoViewModels
                    {
                        Cantidad_Inv = item_grouped.Count(),
                        Nombre = item_grouped.First().color,
                        Cantidad_Rez = item_grouped.Count(x => DateTime.Now.Subtract(x.fecha_ingreso).TotalDays >= consecionario.dia_max_inventario)

                    };
                    lista_color.Add(item);
                }

                //Group Año
                var group_año = vehiculos.GroupBy(x => x.modelo.año);
                foreach (var item_grouped in group_año)
                {
                    ReporteItemString_RezagadoViewModels item = new ReporteItemString_RezagadoViewModels
                    {
                        Cantidad_Inv = item_grouped.Count(),
                        Nombre = item_grouped.First().modelo.año.ToString(),
                        Cantidad_Rez = item_grouped.Count(x => DateTime.Now.Subtract(x.fecha_ingreso).TotalDays >= consecionario.dia_max_inventario)
                    };
                    lista_año.Add(item);
                }

                //Group Tiempo Inventario

                ReporteItemStringViewModels item_cant_30 = new ReporteItemStringViewModels
                {
                    Cantidad = vehiculos.Count(x => DbFunctions.DiffDays(x.fecha_ingreso, DateTime.Now) <= 30),
                    Nombre = "0 - 30"
                };
                lista_cant_tiempo.Add(item_cant_30);
                ReporteItemStringViewModels item_cant_60 = new ReporteItemStringViewModels
                {
                    Cantidad = vehiculos.Count(x => DbFunctions.DiffDays(x.fecha_ingreso, DateTime.Now) > 30 && DbFunctions.DiffDays(x.fecha_ingreso, DateTime.Now) <= 60),
                    Nombre = "30 - 60"
                };
                lista_cant_tiempo.Add(item_cant_60);
                ReporteItemStringViewModels item_cant_90 = new ReporteItemStringViewModels
                {
                    Cantidad = vehiculos.Count(x => DbFunctions.DiffDays(x.fecha_ingreso, DateTime.Now) > 60 && DbFunctions.DiffDays(x.fecha_ingreso, DateTime.Now) <= 90),
                    Nombre = "60 - 90"
                };
                lista_cant_tiempo.Add(item_cant_90);
                ReporteItemStringViewModels item_cant_120 = new ReporteItemStringViewModels
                {
                    Cantidad = vehiculos.Count(x => DbFunctions.DiffDays(x.fecha_ingreso, DateTime.Now) > 90 && DbFunctions.DiffDays(x.fecha_ingreso, DateTime.Now) <= 120),
                    Nombre = "90 - 120"
                };
                lista_cant_tiempo.Add(item_cant_120);

                ReporteItemStringViewModels item_cant_121 = new ReporteItemStringViewModels
                {
                    Cantidad = vehiculos.Count(x => DbFunctions.DiffDays(x.fecha_ingreso, DateTime.Now) > 120),
                    Nombre = "120+"
                };
                lista_cant_tiempo.Add(item_cant_121);

                ReporteItemString_RezagadoViewModels item_cost_10 = new ReporteItemString_RezagadoViewModels
                {

                    Cantidad_Inv = vehiculos.Count(x => x.valor_compra <= 10000),
                    Nombre = "0 $ a 10.000 $",
                    Cantidad_Rez = vehiculos.Count(x => x.valor_compra <= 10000 && DbFunctions.DiffDays(x.fecha_ingreso, DateTime.Now) >= consecionario.dia_max_inventario)
                };
                lista_costo.Add(item_cost_10);

                ReporteItemString_RezagadoViewModels item_cost_20 = new ReporteItemString_RezagadoViewModels
                {

                    Cantidad_Inv = vehiculos.Count(x => x.valor_compra > 10000 && x.valor_compra <= 20000),
                    Nombre = "10.000 $ a 20.000 $",
                    Cantidad_Rez = vehiculos.Count(x => (x.valor_compra > 10000 && x.valor_compra <= 20000) && DbFunctions.DiffDays(x.fecha_ingreso, DateTime.Now) >= consecionario.dia_max_inventario)
                };
                lista_costo.Add(item_cost_20);

                ReporteItemString_RezagadoViewModels item_cost_30 = new ReporteItemString_RezagadoViewModels
                {

                    Cantidad_Inv = vehiculos.Count(x => x.valor_compra > 20000 && x.valor_compra <= 30000),
                    Nombre = "20.000 $ a 30.000 $",
                    Cantidad_Rez = vehiculos.Count(x => (x.valor_compra > 20000 && x.valor_compra <= 30000) && DbFunctions.DiffDays(x.fecha_ingreso, DateTime.Now) >= consecionario.dia_max_inventario)
                };
                lista_costo.Add(item_cost_30);
                ReporteItemString_RezagadoViewModels item_cost_31 = new ReporteItemString_RezagadoViewModels
                {

                    Cantidad_Inv = vehiculos.Count(x => x.valor_compra > 30000),
                    Nombre = "30.000 $ +",
                    Cantidad_Rez = vehiculos.Count(x => x.valor_compra > 30000 && DbFunctions.DiffDays(x.fecha_ingreso, DateTime.Now) >= consecionario.dia_max_inventario)
                };
                lista_costo.Add(item_cost_31);


            }

            //Armar modelo
            modelo.Cant_Total_Vehiculos = vehiculos.Count();
            modelo.Cant_Vehiculos_Rezagados = vehiculos.Count(x => DbFunctions.DiffDays(x.fecha_ingreso, DateTime.Now) > consecionario.dia_max_inventario);
            modelo.Lista_Año = lista_año;
            modelo.Lista_Cant_Tiempo = lista_cant_tiempo;
            modelo.Lista_Categoria = lista_categoria;
            modelo.Lista_Costo = lista_costo;
            modelo.Lista_Color = lista_color;

            modelo.Lista_Modelo = lista_modelo;

            var vehiculos_total = from l_vehiculos in autodb.vehiculo where l_vehiculos.fk_vehiculo_estado == usuario.id_Concesionario select new { l_vehiculos.id_vehiculo, l_vehiculos.fecha_ingreso, l_vehiculos.fecha_salida };
            modelo.Cant_Vehiculos_Entraron = vehiculos_total.Count(x => DbFunctions.DiffDays(x.fecha_ingreso, DateTime.Now) <= 30);
            modelo.Cant_Vehiculos_Salienron = vehiculos_total.Where(x => x.fecha_salida.HasValue && DbFunctions.DiffDays(x.fecha_salida.Value, DateTime.Now) <= 30).Count();
            modelo.Lista_Marca_Inv_Rez = lista_marca_inv_rez;
            return View(modelo);
        }

        [Authorize]
        public ActionResult Reporte_Inventario(string fecha_inicio, string fecha_fin)
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            var consecionario = (from l_concesio in autodb.concesionario where l_concesio.id_concesionario == usuario.id_Concesionario select new { l_concesio.dia_max_inventario, l_concesio.id_concesionario }).First();

            ReporteInventarioViewModels modelo = new ReporteInventarioViewModels();

            List<ReporteItemString_RezagadoViewModels> lista_modelo = new List<ReporteItemString_RezagadoViewModels>();
            List<ReporteItemString_RezagadoViewModels> lista_categoria = new List<ReporteItemString_RezagadoViewModels>();
            List<ReporteItemString_RezagadoViewModels> lista_color = new List<ReporteItemString_RezagadoViewModels>();
            List<ReporteItemString_RezagadoViewModels> lista_año = new List<ReporteItemString_RezagadoViewModels>();
            List<ReporteItemStringViewModels> lista_cant_tiempo = new List<ReporteItemStringViewModels>();

            List<ReporteItemString_RezagadoViewModels> lista_costo = new List<ReporteItemString_RezagadoViewModels>();
            List<ReporteItemString_RezagadoViewModels> lista_marca_inv_rez = new List<ReporteItemString_RezagadoViewModels>();


            DateTime fecha_ingresada_incio = new DateTime();
            DateTime fecha_ingresada_fin = new DateTime();
            if (fecha_inicio != null)
            {
                fecha_ingresada_incio = Convert.ToDateTime(fecha_inicio);
                fecha_ingresada_fin = Convert.ToDateTime(fecha_fin);
            }
            else
            {
                fecha_ingresada_incio = DateTime.Now;
                fecha_ingresada_fin = DateTime.Now;
            }


            var vehiculos = from l_vehiculo in autodb.vehiculo where l_vehiculo.fk_concesionario == usuario.id_Concesionario && l_vehiculo.fecha_ingreso <= fecha_ingresada_fin && ((l_vehiculo.fecha_salida.HasValue && l_vehiculo.fecha_salida.Value >= fecha_ingresada_incio) || (l_vehiculo.fecha_salida == null)) select l_vehiculo;
            var enumerable_vehiculo = vehiculos.AsEnumerable();
            if (vehiculos.Any())
            {

                //Consulta Marca Inv Rez
                var group_marcas_inv_rez = vehiculos.GroupBy(x => x.modelo.fk_marca);
                foreach (var item_marcas in group_marcas_inv_rez)
                {
                    ReporteItemString_RezagadoViewModels item_marc = new ReporteItemString_RezagadoViewModels
                    {
                        Cantidad_Inv = item_marcas.Count(),
                        Nombre = item_marcas.First().modelo.marca.nombre,
                        Cantidad_Rez = item_marcas.Count(x => CantidadDiasEnInventario(fecha_ingresada_fin,x.fecha_salida,x.fecha_ingreso) >= consecionario.dia_max_inventario)
                    };
                    lista_marca_inv_rez.Add(item_marc);
                }

                //Group Modelo Inv Rez
                var group_modelo = vehiculos.GroupBy(x => x.fk_modelo);
                foreach (var item_grouped in group_modelo)
                {
                    ReporteItemString_RezagadoViewModels item = new ReporteItemString_RezagadoViewModels
                    {
                        Cantidad_Inv = item_grouped.Count(),
                        Nombre = item_grouped.First().modelo.nombre,
                        Cantidad_Rez = item_grouped.Count(x => CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) >= consecionario.dia_max_inventario)
                    };
                    lista_modelo.Add(item);
                }

                //Group Categoria
                var group_categoria = vehiculos.GroupBy(x => x.modelo.fk_clasificacion);
                foreach (var item_grouped in group_categoria)
                {
                    ReporteItemString_RezagadoViewModels item = new ReporteItemString_RezagadoViewModels
                    {
                        Cantidad_Inv = item_grouped.Count(),
                        Nombre = item_grouped.First().modelo.modelo_clasificacion.descripcion,
                        Cantidad_Rez = item_grouped.Count(x => CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) >= consecionario.dia_max_inventario)
                    };
                    lista_categoria.Add(item);
                }

                //Group Color
                var group_color = vehiculos.GroupBy(x => x.color);
                foreach (var item_grouped in group_color)
                {
                    ReporteItemString_RezagadoViewModels item = new ReporteItemString_RezagadoViewModels
                    {
                        Cantidad_Inv = item_grouped.Count(),
                        Nombre = item_grouped.First().color,
                        Cantidad_Rez = item_grouped.Count(x => CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) >= consecionario.dia_max_inventario)

                    };
                    lista_color.Add(item);
                }

                //Group Año
                var group_año = vehiculos.GroupBy(x => x.modelo.año);
                foreach (var item_grouped in group_año)
                {
                    ReporteItemString_RezagadoViewModels item = new ReporteItemString_RezagadoViewModels
                    {
                        Cantidad_Inv = item_grouped.Count(),
                        Nombre = item_grouped.First().modelo.año.ToString(),
                        Cantidad_Rez = item_grouped.Count(x => CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) >= consecionario.dia_max_inventario)
                    };
                    lista_año.Add(item);
                }

                //Group Tiempo Inventario
                
                ReporteItemStringViewModels item_cant_30 = new ReporteItemStringViewModels
                {
                    Cantidad = enumerable_vehiculo.Count(x => CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) <= 30),
                    Nombre = "0 - 30"
                };
                lista_cant_tiempo.Add(item_cant_30);
                ReporteItemStringViewModels item_cant_60 = new ReporteItemStringViewModels
                {
                    Cantidad = enumerable_vehiculo.Count(x => CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) > 30 && CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) <= 60),
                    Nombre = "30 - 60"
                };
                lista_cant_tiempo.Add(item_cant_60);
                ReporteItemStringViewModels item_cant_90 = new ReporteItemStringViewModels
                {
                    Cantidad = enumerable_vehiculo.Count(x => CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) > 60 && CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) <= 90),
                    Nombre = "60 - 90"
                };
                lista_cant_tiempo.Add(item_cant_90);
                ReporteItemStringViewModels item_cant_120 = new ReporteItemStringViewModels
                {
                    Cantidad = enumerable_vehiculo.Count(x => CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) > 90 && CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) <= 120),
                    Nombre = "90 - 120"
                };
                lista_cant_tiempo.Add(item_cant_120);

                ReporteItemStringViewModels item_cant_121 = new ReporteItemStringViewModels
                {
                    Cantidad = enumerable_vehiculo.Count(x => CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) > 120),
                    Nombre = "120+"
                };
                lista_cant_tiempo.Add(item_cant_121);
                //Costos
                ReporteItemString_RezagadoViewModels item_cost_10 = new ReporteItemString_RezagadoViewModels
                {

                    Cantidad_Inv = enumerable_vehiculo.Count(x => x.valor_compra <= 10000),
                    Nombre = "0 $ a 10.000 $",
                    Cantidad_Rez = enumerable_vehiculo.Count(x => x.valor_compra <= 10000 && CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) >= consecionario.dia_max_inventario)
                };
                lista_costo.Add(item_cost_10);

                ReporteItemString_RezagadoViewModels item_cost_20 = new ReporteItemString_RezagadoViewModels
                {

                    Cantidad_Inv = enumerable_vehiculo.Count(x => x.valor_compra > 10000 && x.valor_compra <= 20000),
                    Nombre = "10.000 $ a 20.000 $",
                    Cantidad_Rez = enumerable_vehiculo.Count(x => (x.valor_compra > 10000 && x.valor_compra <= 20000) && CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) >= consecionario.dia_max_inventario)
                };
                lista_costo.Add(item_cost_20);

                ReporteItemString_RezagadoViewModels item_cost_30 = new ReporteItemString_RezagadoViewModels
                {

                    Cantidad_Inv = enumerable_vehiculo.Count(x => x.valor_compra > 20000 && x.valor_compra <= 30000),
                    Nombre = "20.000 $ a 30.000 $",
                    Cantidad_Rez = enumerable_vehiculo.Count(x => (x.valor_compra > 20000 && x.valor_compra <= 30000) && CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) >= consecionario.dia_max_inventario)
                };
                lista_costo.Add(item_cost_30);
                ReporteItemString_RezagadoViewModels item_cost_31 = new ReporteItemString_RezagadoViewModels
                {

                    Cantidad_Inv = enumerable_vehiculo.Count(x => x.valor_compra > 30000),
                    Nombre = "30.000 $ +",
                    Cantidad_Rez = enumerable_vehiculo.Count(x => x.valor_compra > 30000 && CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) >= consecionario.dia_max_inventario)
                };
                lista_costo.Add(item_cost_31);


            }

            //Armar modelo
            modelo.Cant_Total_Vehiculos = vehiculos.Count();
            modelo.Cant_Vehiculos_Rezagados = enumerable_vehiculo.Count(x => CantidadDiasEnInventario(fecha_ingresada_fin, x.fecha_salida, x.fecha_ingreso) > consecionario.dia_max_inventario);
            modelo.Lista_Año = lista_año;
            modelo.Lista_Cant_Tiempo = lista_cant_tiempo;
            modelo.Lista_Categoria = lista_categoria;
            modelo.Lista_Costo = lista_costo;
            modelo.Lista_Color = lista_color;

            modelo.Lista_Modelo = lista_modelo;

            var vehiculos_total = from l_vehiculos in autodb.vehiculo where l_vehiculos.fk_vehiculo_estado == usuario.id_Concesionario select new { l_vehiculos.id_vehiculo, l_vehiculos.fecha_ingreso, l_vehiculos.fecha_salida };
            modelo.Cant_Vehiculos_Entraron = vehiculos_total.Count(x => x.fecha_ingreso >= fecha_ingresada_incio && x.fecha_ingreso <= fecha_ingresada_fin);
            modelo.Cant_Vehiculos_Salienron = vehiculos_total.Where(x => x.fecha_salida.HasValue && x.fecha_salida.Value >= fecha_ingresada_incio && x.fecha_salida.Value <= fecha_ingresada_fin).Count();
            modelo.Lista_Marca_Inv_Rez = lista_marca_inv_rez;
            if((fecha_ingresada_fin.DayOfYear == DateTime.Now.DayOfYear) && (fecha_ingresada_incio.DayOfYear == DateTime.Now.DayOfYear))
            {
                modelo.Rango_de_Consulta = "Hoy";
            }
            else
            {
                modelo.Rango_de_Consulta = fecha_ingresada_incio.ToShortDateString() + " a " + fecha_ingresada_fin.ToShortDateString();
            }
            


            return View(modelo);
        }

        public int CantidadDiasEnInventario(DateTime fecha_consulta, DateTime? fecha_registrada, DateTime fecha_ingreso)
        {
            int cant_dias = 0;
            if(fecha_registrada != null)
            {
                if (fecha_consulta >= fecha_registrada)
                {
                    cant_dias = Convert.ToInt32(fecha_registrada.Value.Subtract(fecha_ingreso).TotalDays);
                }
                else
                {
                    cant_dias = Convert.ToInt32(fecha_consulta.Subtract(fecha_ingreso).TotalDays);
                }

            }else 
            {
                cant_dias = Convert.ToInt32(DateTime.Now.Subtract(fecha_ingreso).TotalDays);
            }
            return cant_dias;
        }

        [Authorize]
        public ActionResult Reporte_Ganancia(string fecha_inicio, string fecha_fin)
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            ReporteVentasViewModels modelo = new ReporteVentasViewModels();
            List<ReporteItemProfitViewModels> lista_venta_por_dia = new List<ReporteItemProfitViewModels>();
            List<ReporteItemProfitViewModels> lista_ganancia_neta_venta = new List<ReporteItemProfitViewModels>();
            List<ReporteItemProfitViewModels> lista_ganancia_neta_dia = new List<ReporteItemProfitViewModels>();
            DateTime fecha_ingresada = new DateTime();
            if (fecha_inicio != null)
            {
                fecha_ingresada = Convert.ToDateTime(fecha_inicio);
            }
            else
            {
                fecha_ingresada = DateTime.Now;
            }
           

            var vehiculos = from l_vehiculos in autodb.vehiculo where l_vehiculos.fk_concesionario == usuario.id_Concesionario && l_vehiculos.fecha_salida.HasValue && (l_vehiculos.fecha_salida.Value.Month == fecha_ingresada.Month && l_vehiculos.fecha_salida.Value.Year == fecha_ingresada.Year) select new {l_vehiculos.fecha_salida, l_vehiculos.valor_venta, l_vehiculos.valor_compra, l_vehiculos.id_vehiculo};
            var aux_t = vehiculos.Count();
            if(vehiculos.Any())
            {
                var group_vehiculos = vehiculos.GroupBy(x => x.fecha_salida.Value.Day);

                var aux_5_5 = group_vehiculos.Where(x => x.Count() <= 5);
                var aux_5 = 0;
                if (aux_5_5.Any())
                {
                    aux_5 = aux_5_5.Sum(y => y.Count());
                }
                var aux_7_7 = group_vehiculos.Where(x => x.Count() > 5 && x.Count() <= 7);
                var aux_7 = 0;
                if(aux_7_7.Any())
                {
                    aux_7 = aux_7_7.Sum(y => y.Count());
                }
                    
                var aux_8_8 = group_vehiculos.Where(x => x.Count() > 7);
                var aux_8 = 0;
                if (aux_8_8.Any())
                {
                    aux_8 = aux_8_8.Sum(y => y.Count());
                }
                ReporteItemProfitViewModels vent_dia_5 = new ReporteItemProfitViewModels
                {
                    Nombre = "Menos de 5 unidades",
                    Cantidad_Ventas= aux_5,
                    Porcentaje = (Convert.ToDecimal(aux_5) / Convert.ToDecimal(aux_t)) * 100 
                };
                lista_venta_por_dia.Add(vent_dia_5);
                ReporteItemProfitViewModels vent_dia_7 = new ReporteItemProfitViewModels
                {
                    Nombre = "Entre 5 y 7 unidades",
                    Cantidad_Ventas = aux_7,
                    Porcentaje = (Convert.ToDecimal(aux_7) / Convert.ToDecimal(aux_t)) * 100
                };
                lista_venta_por_dia.Add(vent_dia_7);
                ReporteItemProfitViewModels vent_dia_8 = new ReporteItemProfitViewModels
                {
                    Nombre = "Más de 7 unidades",
                    Cantidad_Ventas = aux_8,
                    Porcentaje = (Convert.ToDecimal(aux_8) / Convert.ToDecimal(aux_t)) * 100
                };
                lista_venta_por_dia.Add(vent_dia_8);

                //Ganancia neta por Venta
                var venta_a_25 = vehiculos.Count(x => (x.valor_venta.Value - x.valor_compra) <= 2500);
                var venta_a_35 = vehiculos.Count(x => (x.valor_venta.Value - x.valor_compra) > 2500 && (x.valor_venta.Value - x.valor_compra) <= 3500);
                var venta_a_36 = vehiculos.Count(x => (x.valor_venta.Value - x.valor_compra) > 3500);
                ReporteItemProfitViewModels ganancia_neta_v_25 = new ReporteItemProfitViewModels
                {
                    Nombre = "Menos de 2.500$",
                    Cantidad_Ventas = venta_a_25,
                    Porcentaje = (Convert.ToDecimal(venta_a_25) / Convert.ToDecimal(aux_t)) * 100
                };
                lista_ganancia_neta_venta.Add(ganancia_neta_v_25);
                ReporteItemProfitViewModels ganancia_neta_v_35 = new ReporteItemProfitViewModels
                {
                    Nombre = "Entre 2.500$ y 3.500$",
                    Cantidad_Ventas = venta_a_35,
                    Porcentaje = (Convert.ToDecimal(venta_a_35) / Convert.ToDecimal(aux_t)) * 100
                };
                lista_ganancia_neta_venta.Add(ganancia_neta_v_35);
                ReporteItemProfitViewModels ganancia_neta_v_36 = new ReporteItemProfitViewModels
                {
                    Nombre = "Más de 3.500$",
                    Cantidad_Ventas = venta_a_36,
                    Porcentaje = (Convert.ToDecimal(venta_a_36) / Convert.ToDecimal(aux_t)) * 100
                };
                lista_ganancia_neta_venta.Add(ganancia_neta_v_36);

                //Ganancia neta por dia
                var venta_dia_14_14 = group_vehiculos.Where(x => x.Sum(y => (y.valor_venta.Value - y.valor_compra)) <= 14000);
                var venta_dia_14 = 0;
                if (venta_dia_14_14.Any())
                {
                    venta_dia_14 = venta_dia_14_14.Sum(y => y.Count());
                }
                var venta_dia_16_16 = group_vehiculos.Where(x => (x.Sum(y => (y.valor_venta.Value - y.valor_compra)) > 14000 && x.Sum(y => (y.valor_venta.Value - y.valor_compra)) <= 16000));
                var venta_dia_16 = 0;
                if (venta_dia_16_16.Any())
                {
                    venta_dia_16 = venta_dia_16_16.Sum(y => y.Count());
                }
                var venta_dia_17_17 = group_vehiculos.Where(x => x.Sum(y => (y.valor_venta.Value - y.valor_compra)) > 16000);
                var venta_dia_17 = 0;
                if (venta_dia_17_17.Any())
                {
                    venta_dia_17 = venta_dia_17_17.Sum(y => y.Count());
                }               
                ReporteItemProfitViewModels ganancia_n_dia_14 = new ReporteItemProfitViewModels
                {
                    Nombre = "Menos de 14.000$",
                    Cantidad_Ventas = venta_dia_14,
                    Porcentaje = (Convert.ToDecimal(venta_dia_14) / Convert.ToDecimal(aux_t)) * 100
                };
                lista_ganancia_neta_dia.Add(ganancia_n_dia_14);
                ReporteItemProfitViewModels ganancia_n_dia_16 = new ReporteItemProfitViewModels
                {
                    Nombre = "Entre 14.000$ y 16.000$",
                    Cantidad_Ventas = venta_dia_16,
                    Porcentaje = (Convert.ToDecimal(venta_dia_16) / Convert.ToDecimal(aux_t)) * 100
                };
                lista_ganancia_neta_dia.Add(ganancia_n_dia_16);

                ReporteItemProfitViewModels ganancia_n_dia_17 = new ReporteItemProfitViewModels
                {
                    Nombre = "Más de 16.000$",
                    Cantidad_Ventas = venta_dia_17,
                    Porcentaje = (Convert.ToDecimal(venta_dia_17) / Convert.ToDecimal(aux_t)) * 100
                };
                lista_ganancia_neta_dia.Add(ganancia_n_dia_17);


                
            }


            //armar modelo
            modelo.Lista_Ganancia_Neta_Dia = lista_ganancia_neta_dia;
            modelo.Lista_Ganancia_Neta_Venta = lista_ganancia_neta_venta;
            modelo.Lista_Venta_Por_Dia = lista_venta_por_dia;
            
            modelo.Cantida_Venta_Mes = aux_t;
            if (aux_t > 0)
            {
                modelo.Ganancia_promedio_vehiculo = modelo.Ganancia_Neta_Total / Convert.ToDecimal(aux_t);
                modelo.Ganancia_Neta_Total = vehiculos.Sum(y => ((y.valor_venta.Value - y.valor_compra)));
            }
            else
            {
                modelo.Ganancia_promedio_vehiculo = 0;
                modelo.Ganancia_Neta_Total = 0;
            }



            modelo.Mes_Año = ObtenerMes(fecha_ingresada.Month) + " " + fecha_ingresada.Year;
            
            return View(modelo);
        }

        [Authorize]
        public ActionResult Reporte_Empleados(string fecha_inicio, string fecha_fin)
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            ReporteEmpleadosViewModels modelo = new ReporteEmpleadosViewModels();

            List<ReporteEmpleado_ItemViewModels> lista_top_compradores = new List<ReporteEmpleado_ItemViewModels>();
            List<ReporteEmpleado_ItemViewModels> lista_top_vendedores = new List<ReporteEmpleado_ItemViewModels>();
            List<ReporteEmpleado_ItemViewModels> lista_top_financista = new List<ReporteEmpleado_ItemViewModels>();
            
            
            DateTime fecha_ingresada_incio = new DateTime();
            DateTime fecha_ingresada_fin = new DateTime();
            if (fecha_inicio != null)
            {
                fecha_ingresada_incio = Convert.ToDateTime(fecha_inicio);
                fecha_ingresada_fin = Convert.ToDateTime(fecha_fin);
            }
            else
            {
                fecha_ingresada_incio = DateTime.Now.AddYears(-100);
                fecha_ingresada_fin = DateTime.Now;
            }


            var usuarios_comision = from l_usuario in autodb.usuario_gana_comision where l_usuario.usuario.fk_concesionario == usuario.id_Concesionario && (l_usuario.transaccion_venta.fecha >= fecha_ingresada_incio && l_usuario.transaccion_venta.fecha <= fecha_ingresada_fin) select l_usuario;
            var usuarios_compra = from l_venta in autodb.transaccion_compra where l_venta.fk_concesionario == usuario.id_Concesionario && (l_venta.fecha >= fecha_ingresada_incio && l_venta.fecha <= fecha_ingresada_fin) select l_venta;

            var usuarios_financista = usuarios_comision.Where(x => x.usuario.fk_tipo_usuario == 5);
            var usuarios_venta = usuarios_comision.Where(x => x.usuario.fk_tipo_usuario != 5);
            if(usuarios_comision.Any())
            {
                var group_venta = usuarios_venta.GroupBy(x => x.fk_usuario).OrderByDescending(y => y.Count()).Take(5);
                var group_financista = usuarios_financista.GroupBy(x => x.fk_usuario).OrderByDescending(y => y.Count()).Take(5);

                foreach(var item in group_venta)
                {
                    var dato_usu = item.First().usuario;
                    ReporteEmpleado_ItemViewModels item_add = new ReporteEmpleado_ItemViewModels()
                    {
                        Nombre = dato_usu.nombre + " " +dato_usu.apellido,
                        Cant_Ventas = item.Count(),
                        Monto_Total = item.Sum(x => x.monto)
                    };
                    lista_top_vendedores.Add(item_add);
                }

                foreach (var item in group_financista)
                {
                    var dato_usu = item.First().usuario;
                    ReporteEmpleado_ItemViewModels item_add = new ReporteEmpleado_ItemViewModels()
                    {
                        Nombre = dato_usu.nombre + " " + dato_usu.apellido,
                        Cant_Ventas = item.Count(),
                        Monto_Total = item.Sum(x => x.monto)
                    };
                    lista_top_financista.Add(item_add);
                }


            }
            if(usuarios_compra.Any())
            {
                var group_compra = usuarios_compra.GroupBy(x => x.fk_usuario).OrderByDescending(y => y.Sum(c => c.vehiculo.Where(z => z.valor_venta.HasValue).Count())).Take(5);
                foreach (var item in group_compra)
                {
                    var dato_usu = item.First().usuario;
                    ReporteEmpleado_ItemViewModels item_add = new ReporteEmpleado_ItemViewModels()
                    {
                        Nombre = dato_usu.nombre + " " + dato_usu.apellido,
                        Cant_Ventas = item.Count(),
                        Monto_Total = item.Sum(x => x.vehiculo.Where(z => z.valor_venta.HasValue).Sum(y => (y.valor_venta.Value - y.valor_compra)))
                    };
                    lista_top_compradores.Add(item_add);
                }

            }
            



            //Armando Modelo
            modelo.Vendedor_Venta_Total = usuarios_venta.Count();
            if(modelo.Vendedor_Venta_Total > 0)
            {
                modelo.Vendedor_Comision_Total = usuarios_venta.Sum(x => x.monto);
            }else
            {
                modelo.Vendedor_Comision_Total = 0;
            }
            modelo.Financista_Venta_Total = usuarios_financista.Count();
            if (modelo.Financista_Venta_Total > 0)
            {
                modelo.Financista_Comision_Total = usuarios_financista.Sum(x => x.monto);
            }
            else
            {
                modelo.Financista_Comision_Total = 0;
            }

            modelo.Comprador_Compra_Total = usuarios_compra.Count();
            if (modelo.Comprador_Compra_Total != 0)
            {
                modelo.Monto_Total_Compra = usuarios_compra.Sum(x => x.vehiculo.Sum(y => y.valor_compra));
            }
            else
            {
                modelo.Monto_Total_Compra = 0;
            }
            modelo.Lista_Top_Compradores = lista_top_compradores;
            modelo.Lista_Top_Financista = lista_top_financista;
            modelo.Lista_Top_Vendedores = lista_top_vendedores;
            if ((fecha_ingresada_fin.DayOfYear == DateTime.Now.DayOfYear) && (fecha_ingresada_incio.DayOfYear == DateTime.Now.AddYears(-100).DayOfYear))
            {
                modelo.Rango_de_Consulta = "Historico Total";
            }
            else
            {
                modelo.Rango_de_Consulta = fecha_ingresada_incio.ToShortDateString() + " a " + fecha_ingresada_fin.ToShortDateString();
            }


            return View(modelo);
        }
        

        public string ObtenerMes(int i)
        {
            string Mes = "";
            if (i == 1)
            {
                Mes = "Enero";
            }
            else if (i == 2)
            {
                Mes = "Febrero";
            }
            else if (i == 3)
            {
                Mes = "Marzo";
            }
            else if (i == 4)
            {
                Mes = "Abril";
            }
            else if (i == 5)
            {
                Mes = "Mayo";
            }
            else if (i == 6)
            {
                Mes = "Junio";
            }
            else if (i == 7)
            {
                Mes = "Julio";
            }
            else if (i == 8)
            {
                Mes = "Agosto";
            }
            else if (i == 9)
            {
                Mes = "Septiembre";
            }
            else if (i == 10)
            {
                Mes = "Octubre";
            }
            else if (i == 11)
            {
                Mes = "Noviembre";
            }
            else if (i == 12)
            {
                Mes = "Diciembre";
            }

            return Mes;
        }
    }
}