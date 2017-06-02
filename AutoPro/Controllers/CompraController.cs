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
            
            var lista_modelos = (from l_model in autodb.modelo where (l_model.modelo1 + " " + l_model.nombre + " " + l_model.año).Contains(q) select l_model).ToList();
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
                    Nro_Rezagados = inventario.Where(x => x.fk_modelo == item.id_modelo && x.fecha_salida == null && DateTime.Now.Subtract(x.fecha_ingreso).TotalDays > 30).Count(),
                    Nro_Vendidos = inventario.Where(x => x.fk_modelo == item.id_modelo && x.fecha_salida  != null).Count(),
                };

                l_modelos.Add(model);

            }

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
    }
}