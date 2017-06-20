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
    public class VentaController : Controller
    {


        private sgcaEntities autodb = new sgcaEntities(); //conexcion con bd
        
        
        // GET: Venta
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Preventa()
        {
            ComprarVehiculoViewModels modelo = new ComprarVehiculoViewModels();
            return View(modelo);
        }





        public ActionResult Prueba()
        {

            return View();
        }

        public ActionResult Prueba2()
        {

            return View();
        }

        public ActionResult Prueba3()
        {

            return View();
        }

        public ActionResult Prueba4()
        {
            return View();
        }

        public ActionResult HistorialComision()
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

            foreach (var item in lista_modelos)
            {
                if(inventario.Where(x => x.modelo.id_modelo == item.id_modelo && x.fecha_salida == null).Count() != 0)
                {
                    ModelosDetallesViewModels model = new ModelosDetallesViewModels
                    {
                        id = item.id_modelo,
                        Image = item.imagen,
                        Descripcion = "" + item.descripcion,
                        Nombre = item.modelo1 + " " + item.nombre + " " + item.año,
                        Nro_Inventario = inventario.Where(x => x.fk_modelo == item.id_modelo && x.fecha_salida == null).Count(),
                        Nro_Rezagados = inventario.Where(x => x.fk_modelo == item.id_modelo && x.fecha_salida == null && DateTime.Now.Subtract(x.fecha_ingreso).TotalDays > concesionario.dia_max_inventario).Count(),
                        Nro_Vendidos = inventario.Where(x => x.fk_modelo == item.id_modelo && x.fecha_salida != null).Count(),
                    };

                    l_modelos.Add(model);

                }
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

    }
}