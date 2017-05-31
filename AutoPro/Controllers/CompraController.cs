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