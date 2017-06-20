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
    }
}