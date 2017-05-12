using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using AutoPro.Models;

namespace AutoPro
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Session_Start(Object sender, EventArgs e)
        {
            HttpContext.Current.Session.Add("idUser", null);
            HttpContext.Current.Session.Add("Mensaje", null);
            HttpContext.Current.Session.Add("NombreUser", null);
            HttpContext.Current.Session.Add("Cargo", null);
            HttpContext.Current.Session.Add("User", null);


        }
    }
}
