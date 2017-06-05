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


namespace AutoPro.Controllers
{
    [Authorize]
    public class CompraController : Controller
    {

        private sgcaEntities autodb = new sgcaEntities(); //conexcion con bd
        
        // GET: Compra
        public ActionResult Index()
        {
            return View();
        }


        // GET: Evaluacion Vehiculo

        public ActionResult Evaluacion()
        {


            return View();
        }



        [AllowAnonymous]
        public JsonResult ObtenerModelos(string q)
        {
            int id_concesionario_session = Convert.ToInt32(this.Session["Concesionario"]);
            var concesionario = (from l_concesionario in autodb.concesionario where l_concesionario.id_concesionario == id_concesionario_session select l_concesionario).First();
            var lista_modelos = (from l_model in autodb.modelo where (l_model.modelo1 + " " + l_model.nombre + " " + l_model.año).Contains(q) select l_model).OrderBy(x => x.modelo1).ToList();
            List<ModelosDetallesViewModels> l_modelos = new List<ModelosDetallesViewModels>();
            var inventario = (from inv in autodb.vehiculo where inv.fk_concesionario == id_concesionario_session select inv).ToList();

            foreach(var item in lista_modelos)
            {
                ModelosDetallesViewModels model = new ModelosDetallesViewModels
                {
                    id = item.id_modelo,
                    Image = item.imagen,
                    Descripcion = "" + item.descripcion,
                    Nombre = item.modelo1 + " " + item.nombre + " " + item.año,
                    Nro_Inventario = inventario.Where( x => x.fk_modelo == item.id_modelo && x.fecha_salida == null).Count(),
                    Nro_Rezagados = inventario.Where(x => x.fk_modelo == item.id_modelo && x.fecha_salida == null && DateTime.Now.Subtract(x.fecha_ingreso).TotalDays > concesionario.dia_max_inventario).Count(),
                    Nro_Vendidos = inventario.Where(x => x.fk_modelo == item.id_modelo && x.fecha_salida  != null).Count(),
                };

                l_modelos.Add(model);

            }
            l_modelos.OrderBy(x => x.Nombre);
            ListaModelosViewModels t_modelos = new ListaModelosViewModels
            {
                Lista_Modelo = l_modelos,
                Total_Modelos = l_modelos.Count()

            };

            var json = t_modelos;
            return Json(json, JsonRequestBehavior.AllowGet);
        }


        // GET: Busqueda por Modelo
        public ActionResult BusquedaPorModelo ()
        {
            int id_concesionario_session = Convert.ToInt32(this.Session["Concesionario"]);
            var nro_marca = (from c_marca in autodb.marca select c_marca).Count();
            var nro_categoria = (from c_cat in autodb.modelo_clasificacion select c_cat).Count();
            var nro_inventario = (from c_inv in autodb.vehiculo where c_inv.fecha_salida != null && c_inv.fk_concesionario == id_concesionario_session select c_inv).Count();
            BusquedaPorModeloViewModels busqueda_modelo = new BusquedaPorModeloViewModels { 
            Nro_Categoria = nro_categoria,
            Nro_Inventario = nro_inventario,
            Nro_Marca = nro_marca
            };

            return View(busqueda_modelo);
        }

        [HttpPost]
        public ActionResult BusquedaPorModelo(BusquedaPorModeloViewModels model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            return RedirectToAction("ConsultarModelo", "Compra", new { id = model.Busq_Modelo });


        }


        // GET: Busqueda por Modelo
        [Authorize]
        public ActionResult ConsultarModelo(int id)
        {
            int id_concesionario_session = Convert.ToInt32(this.Session["Concesionario"]);
            if(id_concesionario_session == null)
            {
                return RedirectToAction("Home", "Index");
            }
            var concesionario = (from l_concesionario in autodb.concesionario where l_concesionario.id_concesionario == id_concesionario_session select l_concesionario).First();
            var auto = from l_auto in autodb.modelo where l_auto.id_modelo == id select l_auto;

            if(auto.Count() == 0)
            {
                return View("Error Modelo no existe");

            }
            else
            {
                var item_auto = auto.First();
                List<BancoFinanciaModeloViewModels> l_banco = new List<BancoFinanciaModeloViewModels>();
                if(ConsultarPreferenciaBanco(item_auto) == true)
                {
                    foreach(var item in item_auto.banco_financia_modelo)
                    {
                        BancoFinanciaModeloViewModels item_banco = new BancoFinanciaModeloViewModels
                        {
                            id_Banco = item.fk_banco,
                            id_Modelo = item.fk_modelo,
                            Banco = item.banco.nombre,
                            Preferencia = item.nivel_preferencia,
                            Valor_financia = item.valor_limite
                        };
                        l_banco.Add(item_banco);
                    }
                }
                
                ModeloDestallesViewModels Modelo_Auto = new ModeloDestallesViewModels
                {
                    id = id,
                    Imagen = item_auto.imagen,
                    Año = item_auto.año,
                    Marca = item_auto.marca.nombre,
                    Modelo = item_auto.modelo1,
                    Valor = item_auto.valor,
                    Nombre = item_auto.modelo1 + " " + item_auto.nombre,
                    Preferencia_Banco = ConsultarPreferenciaBanco(item_auto),
                    Lista_Banco = l_banco,
                    Nivel_Pref_Cliente =  ConsultarPreferenciaCliente(item_auto,id_concesionario_session),
                    Nro_Inventario = item_auto.vehiculo.Where(x => x.fk_concesionario == id_concesionario_session && x.fecha_salida == null).Count(),
                    Nro_Rezagados = item_auto.vehiculo.Where(x => x.fk_concesionario == id_concesionario_session && x.fecha_salida == null && DateTime.Now.Subtract(x.fecha_ingreso).TotalDays > concesionario.dia_max_inventario).Count(),
                    Nro_Vendidos = item_auto.vehiculo.Where(x => x.fk_concesionario == id_concesionario_session && x.fecha_salida != null).Count(),
                    Tiempo_Inventario = Convert.ToInt32(ConsultarPromedioDiasEnInventario(item_auto)),
                    Rentabilidad = ConsultarRentabilidad(item_auto,id_concesionario_session),
                    Valor_Calculado_Maximo = ConsultarValorMaximoModelo(item_auto.id_modelo,id_concesionario_session,100),
                    Valor_Calculado_Minimo = ConsultarValorMinimoModelo(item_auto.id_modelo, id_concesionario_session, 100),
                    Lista_Rentabilidad = ConsultarRentabilidadLista(item_auto,id_concesionario_session)

                };


                return View(Modelo_Auto);



            }    
            
        }

        // POST: Busqueda por Modelo
        [HttpPost]
        public ActionResult ConsultarModelo(ModeloDestallesViewModels modelo)
        {
            int id_concesionario_session = Convert.ToInt32(this.Session["Concesionario"]);
            if (id_concesionario_session == null)
            {
                return RedirectToAction("Home", "Index");
            }
            var concesionario = (from l_concesionario in autodb.concesionario where l_concesionario.id_concesionario == id_concesionario_session select l_concesionario).First();
            var auto = from l_auto in autodb.modelo where l_auto.id_modelo == modelo.id select l_auto;

            if (auto.Count() == 0)
            {
                return RedirectToAction("ConsultarModelo", "Compra", new { id = modelo.id });

            }
            else
            {
                TempData["Auto"] = modelo;
                return RedirectToAction("AdquirirVehiculo", "Compra");

            }

        }


        public ActionResult AdquirirVehiculo()
        {
            int id_concesionario_session = Convert.ToInt32(this.Session["Concesionario"]);
            if (id_concesionario_session == null)
            {
                return RedirectToAction("Home", "Index");
            }
            ModeloDestallesViewModels modelo = (ModeloDestallesViewModels)TempData["Auto"];
            if(modelo == null)
            {
                return RedirectToAction("BusquedaPorModelo", "Compra");

            }
            var concesionario = (from l_concesionario in autodb.concesionario where l_concesionario.id_concesionario == id_concesionario_session select l_concesionario).First();
            var auto = from l_auto in autodb.modelo where l_auto.id_modelo == modelo.id select l_auto;

            if (auto.Count() == 0)
            {
                return RedirectToAction("ConsultarModelo", "Compra", new { id = modelo.id });

            }
            else
            {
                var item_auto = auto.First();
                List<SelectListItem> ListaDeColores= new List<SelectListItem>();
                ListaDeColores.Add(new SelectListItem() { Value = "1", Text = "Negro" });
                ListaDeColores.Add(new SelectListItem() { Value = "2", Text = "Blanco" });
                ListaDeColores.Add(new SelectListItem() { Value = "3", Text = "Rojo" });
                ListaDeColores.Add(new SelectListItem() { Value = "4", Text = "Azul" });
                ListaDeColores.Add(new SelectListItem() { Value = "5", Text = "Verde" });
                ListaDeColores.Add(new SelectListItem() { Value = "6", Text = "Dorado" });
                ListaDeColores.Add(new SelectListItem() { Value = "7", Text = "Plateado" });

                SelectList lista = new SelectList(ListaDeColores, "Value", "Text", "1");
                
                ComprarVehiculoViewModels compra_modelo = new ComprarVehiculoViewModels
                {
                    Estado_Vehiculo = modelo.Estado_Vehiculo,
                    Valor_Maximo = modelo.Valor_Calculado_Maximo,
                    Valor_Minimo = modelo.Valor_Calculado_Minimo,
                    Valor_Mercado = Convert.ToDouble(modelo.Valor),
                    Lista_Colores = lista,
                    id_Modelo = item_auto.id_modelo,
                    Imagen = item_auto.imagen,
                    Año = item_auto.año,
                    Marca = item_auto.marca.nombre,
                    Modelo = item_auto.modelo1,
                    Nombre = item_auto.modelo1 + " " + item_auto.nombre
                };


                return View(compra_modelo);

            }

        }


        [AllowAnonymous]
        [HttpPost]
        public JsonResult ConsultarValorModelo(string id_modelo_c, string estado_vehiculo_c)
        {
            int id_modelo = Convert.ToInt32(id_modelo_c);
            double estado_vehiculo = Convert.ToDouble(estado_vehiculo_c);
            int id_concesionario_session = Convert.ToInt32(this.Session["Concesionario"]);
            var m1 = (from l_modelo in autodb.modelo where l_modelo.id_modelo == id_modelo select l_modelo).First();
            var l_vehiculo = (from l_inventario in autodb.vehiculo where l_inventario.fk_modelo == id_modelo && l_inventario.fk_concesionario == id_concesionario_session select l_inventario).OrderByDescending(x => x.fecha_ingreso).Take(20);
            var l_banco_f_modelo = from l_banco in autodb.banco_financia_modelo where l_banco.fk_modelo == m1.id_modelo select l_banco;
            var concesionario = (from l_concesionario in autodb.concesionario where l_concesionario.id_concesionario == id_concesionario_session select l_concesionario).First();
            double limite_inf = 0;
            double limite_sup = 0;
            MontoVehiculoViewModels modelo = new MontoVehiculoViewModels();
            var json = modelo;

            //Limite inf
            if (l_vehiculo.Count() > 0)
            {
                //Existe al menos una transaccion de compra para este modelo.
                double promedio_inf = 0;
                foreach (var item_vehiculo in l_vehiculo)
                {
                    promedio_inf = promedio_inf + Convert.ToDouble(item_vehiculo.valor_compra);
                }
                promedio_inf = promedio_inf / l_vehiculo.Count();
                limite_inf = (promedio_inf * (100 - concesionario.porcentaje_ganancia)) / 100;

                limite_inf = (limite_inf * estado_vehiculo) / 100;

                
            }
            else
            {
                //No existe ninguna transaccion de compra, por ende el valor sera de acuerdo a la kbb
                limite_inf = (Convert.ToDouble(m1.valor) * (100 - concesionario.porcentaje_ganancia)) / 100;
                limite_inf = (limite_inf * estado_vehiculo) / 100;


            }

            //Limite_Sup
            if (l_banco_f_modelo.Count() > 0)
            {
                //Algun banco financia al modelo de vehiculo
                double promedio_sup = 0;
                foreach (var item_banco in l_banco_f_modelo)
                {
                    promedio_sup = promedio_sup + Convert.ToDouble(item_banco.valor_limite);
                }
                promedio_sup = promedio_sup / l_banco_f_modelo.Count();
                limite_sup = (promedio_sup * (100 - concesionario.porcentaje_ganancia)) / 100;

            }
            else
            {
                //Ningun banco los financia, por ende el valor sera de acuerdo a la kbb
                limite_sup = (Convert.ToDouble(m1.valor) * (100 - concesionario.porcentaje_ganancia)) / 100;


            }

            if(limite_inf > limite_sup)
            {
                modelo.Valor_Maximo = limite_inf;
                modelo.Valor_Minimo = limite_sup;

            }
            else
            {
                modelo.Valor_Maximo = limite_sup;
                modelo.Valor_Minimo = limite_inf;
            }
            json = modelo;

            return Json(json, JsonRequestBehavior.AllowGet);

        }

        public double ConsultarValorMinimoModelo(int id_modelo, int id_concesionario, double estado_vehiculo)
        {
            var m1 = (from l_modelo in autodb.modelo where l_modelo.id_modelo == id_modelo select l_modelo).First();
            var l_vehiculo = (from l_inventario in autodb.vehiculo where l_inventario.fk_modelo == id_modelo && l_inventario.fk_concesionario == id_concesionario select l_inventario).OrderByDescending(x => x.fecha_ingreso).Take(20);
            var concesionario = (from l_concesionario in autodb.concesionario where l_concesionario.id_concesionario == id_concesionario select l_concesionario).First();
            double limite_inf = 0;

            if (l_vehiculo.Count() > 0)
            {
                //Existe al menos una transaccion de compra para este modelo.
                double promedio = 0;
                foreach (var item_vehiculo in l_vehiculo)
                {
                    promedio = promedio + Convert.ToDouble(item_vehiculo.valor_compra);
                }
                promedio = promedio / l_vehiculo.Count();
                limite_inf = (promedio * (100 - concesionario.porcentaje_ganancia)) / 100;

                return (limite_inf * estado_vehiculo) / 100;
            }
            else
            {
                //No existe ninguna transaccion de compra, por ende el valor sera de acuerdo a la kbb
                limite_inf = (Convert.ToDouble(m1.valor) * (100 - concesionario.porcentaje_ganancia)) / 100;
                return (limite_inf * estado_vehiculo) / 100;

            }

        }


        public double ConsultarValorMaximoModelo(int id_modelo, int id_concesionario, double estado_vehiculo)
        {
            var m1 = (from l_modelo in autodb.modelo where l_modelo.id_modelo == id_modelo select l_modelo).First();
            var l_banco_f_modelo = from l_banco in autodb.banco_financia_modelo where l_banco.fk_modelo == m1.id_modelo select l_banco;
            var concesionario = (from l_concesionario in autodb.concesionario where l_concesionario.id_concesionario == id_concesionario select l_concesionario).First();
            double limite_sup = 0;
            if(l_banco_f_modelo.Count() > 0)
            {
                //Algun banco financia al modelo de vehiculo
                double promedio = 0;
                foreach(var item_banco in l_banco_f_modelo)
                {
                    promedio = promedio + Convert.ToDouble(item_banco.valor_limite);
                }
                promedio = promedio / l_banco_f_modelo.Count();
                limite_sup = (promedio * (100 - concesionario.porcentaje_ganancia)) / 100;

                return (limite_sup * estado_vehiculo) / 100;
            }
            else
            {
                //Ningun banco los financia, por ende el valor sera de acuerdo a la kbb
                limite_sup = (Convert.ToDouble(m1.valor) * (100 - concesionario.porcentaje_ganancia)) / 100;
                return (limite_sup * estado_vehiculo) / 100;

            }

        }

        public ActionResult Prueba()
        {

            return View();
        }

        public double ConsultarRentabilidad(modelo m1, int id)
        {
            var inventario = (from l_inv in autodb.vehiculo where l_inv.fk_concesionario == id && l_inv.fk_modelo == m1.id_modelo && l_inv.fecha_salida != null select l_inv).OrderByDescending(x => x.fecha_salida).Take(20);
            if(inventario.Count() > 0)
            {
                double rentabilidad = 0;
                foreach(var item in inventario)
                {
                    rentabilidad = rentabilidad + (Convert.ToDouble((item.valor_venta - item.valor_compra) / item.valor_venta) * 100);

                }

                rentabilidad = rentabilidad / inventario.Count();

                if(rentabilidad > inventario.First().concesionario.porcentaje_ganancia)
                {
                    rentabilidad = 100;
                }
                else
                {
                    if (rentabilidad < 0)
                    {
                        rentabilidad = 0;
                    }
                    else
                    {
                        rentabilidad = (rentabilidad * 100) / Convert.ToDouble(inventario.First().concesionario.porcentaje_ganancia);
                    }
                }


                

                

                return rentabilidad;

            }
            else
            {
                return -1;
            }

        }

        public List<RentabilidadViewModels> ConsultarRentabilidadLista(modelo m1, int id)
        {
            var inventario = (from l_inv in autodb.vehiculo where l_inv.fk_concesionario == id && l_inv.fk_modelo == m1.id_modelo && l_inv.fecha_salida != null select l_inv).OrderByDescending(x => x.fecha_salida).Take(20);
            inventario = inventario.OrderBy(x => x.fecha_salida);
            List<RentabilidadViewModels> list_rentabilidad = new List<RentabilidadViewModels>();

            if (inventario.Count() > 1)
            {
                
                foreach (var item in inventario)
                {
                    RentabilidadViewModels rentabilidad = new RentabilidadViewModels { 
                    Fecha = Convert.ToDateTime(item.fecha_salida).ToShortDateString(),
                    Valor_Compra = Convert.ToDouble(item.valor_compra),
                    Valor_Venta = Convert.ToDouble(item.valor_venta)         
                    };
                    list_rentabilidad.Add(rentabilidad);
                }


                return list_rentabilidad;

            }
            else
            {
                return list_rentabilidad;
            }


        }

        public bool ConsultarPreferenciaBanco(modelo m1)
        {
            if(m1.banco_financia_modelo.Count() != 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public double ConsultarPromedioDiasEnInventario(modelo m1)
        {
            var inventario = from l_inventario in autodb.vehiculo where l_inventario.fk_modelo == m1.id_modelo select l_inventario;
            if(inventario.Count() == 0)
            {
                return 0;
            }
            
            var cant_carros = inventario.Count();
            double total_dias = 0;
            var dia_final = DateTime.Now;
            foreach(var item in inventario)
            {
                if(item.fecha_salida == null)
                {
                    dia_final = DateTime.Now;
                }else{

                    dia_final = Convert.ToDateTime(item.fecha_salida);
                }

                total_dias = total_dias + dia_final.Subtract(item.fecha_ingreso).TotalDays;

            }

            return total_dias / cant_carros;




        }

        public double ConsultarPreferenciaCliente(modelo m1, int id)
        {
            if(m1.vehiculo.Where(x => x.fk_concesionario == id).Count() == 0 )
            {
                return 0;
            }
            else
            {
                    //Obtener Lista de vehiculos en inventario perteneciente al Modelo.
                    var l_modelos_inventario = m1.vehiculo.Where(x => x.fk_concesionario == id);
                    //Calulcar preferencia en ventas: Nro Unidades Vendidas / Nro. Unidades Compradas
                    var unidades_inventario_total = l_modelos_inventario.Count();
                    var unidad_inventario_modelo = l_modelos_inventario.Where(x => x.fecha_salida != null).Count();
                    double p_ventas =  Convert.ToDouble(unidad_inventario_modelo) / Convert.ToDouble(unidades_inventario_total);
                    //Escalar el valor a [0 - 5] 
                    double rate_ventas = (p_ventas * 5) / 1;
                    //Calcular preferencia en preventa: Transaccion de Preventa del Modelo / Transaciones de Preventa Total
                    var t_preventa = from l_t_preventa in autodb.transaccion_venta where l_t_preventa.fk_tipo_venta == 1 && l_t_preventa.fk_concesionario == id select l_t_preventa;
                    double l_modelos_t_preventa = t_preventa.Count(x => x.vehiculo.Where(y => y.fk_modelo == m1.id_modelo).Count() > 0);
                    double rate_preventa = 0;
                    if (t_preventa.Count() != 0)
                    {
                        double p_preventa = l_modelos_t_preventa / Convert.ToDouble(t_preventa.Count());
                        //Escalar el valor a [0 - 5]
                        rate_preventa = (p_preventa * 5) / 1;
                    }
                    
                    //Calcular valor total donde al rate_ventas se le asigna un peso del 90% y al rate_preventa un 10%
                    var t_rate = (rate_ventas * 0.9) + (rate_preventa * 0.1);


                    return t_rate;
                
                
            }

        }


    }
}