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

        [Authorize]
        public ActionResult Preventa()
        {
            ComprarVehiculoViewModels modelo = new ComprarVehiculoViewModels();
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            usuario.id_T_Preventa = 0;
            this.Session["User"] = usuario;


            return View(modelo);
        }




        
        [Authorize]
        //CONSULTAR MARCAS Y CATEGORIA PREFERIDA POR EL CLIENTE Y POR EL MERCADO
        public ActionResult Prueba(int tipo, string id_cliente)
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            Marcas_CategoriaViewModels modelo = new Marcas_CategoriaViewModels();
            List<Venta_MarcasViewModels> l_marcas = new List<Venta_MarcasViewModels>();
            List<Venta_CategoriaViewModels> l_categoria = new List<Venta_CategoriaViewModels>();
            var inventario = from l_vehiculos in autodb.vehiculo where l_vehiculos.fk_concesionario == usuario.id_Concesionario && l_vehiculos.fecha_salida == null select l_vehiculos;
            IQueryable<transaccion_venta> consulta_transacciones = null;

            //Preferencia del Cliente
            if(tipo == 1)
            {
                consulta_transacciones = from l_tran in autodb.transaccion_venta where l_tran.fk_cliente == id_cliente select l_tran;
                
                
            }else{
            //Preferencia del Mercado
                var c_usuario = (from l_concesionario in autodb.concesionario where l_concesionario.id_concesionario == usuario.id_Concesionario select l_concesionario).First();
                consulta_transacciones = from l_tran in autodb.transaccion_venta where l_tran.concesionario.direccion.ciudad.estado.id_estado == c_usuario.direccion.ciudad.estado.id_estado select l_tran;
            }

            /////////MARCAS ////////
            //Obtener Lista de las Marcas en Inventario
            var marcas_inventario = inventario.GroupBy(x => x.modelo.marca.id_marca);
            //Obtener Lista de las Marcar preferidas por el cliente en general
            var marcas_preferidas_cliente = consulta_transacciones.SelectMany(x => x.vehiculo).GroupBy(x => x.modelo.marca.id_marca);

            if (marcas_preferidas_cliente.Count() > 0)
            {
                //Obtener las Marcas que entasn en inventario y que el cliente prefiere
                var marcas_inventario_preferidas_cliente = marcas_inventario.Where(x => marcas_preferidas_cliente.Select(y => y.Key).Contains(x.Key));
                IQueryable<IGrouping<int,vehiculo>> marca_pref_cliente_preventa = null;
                IQueryable<IGrouping<int, vehiculo>> marca_pref_cliente_venta = null;
                /////////////Estadisticas de Preventa////////////////// Se le asigna el 25% de la importancia en el calculo
                var transaccion_vehiculos_preventa = consulta_transacciones.Where(x => x.tipo_transaccion_venta.id_tipo_venta == 1);
                var cant_veh_preventa_total = 1;
                if (transaccion_vehiculos_preventa.Count() > 0)
                {
                    //Obtener todos los vehiculos en los cuales el cliente estuvo interesado en preventa
                    var vehiculos_consultados_cliente_preventa = transaccion_vehiculos_preventa.SelectMany(x => x.vehiculo);
                    //Agrupar los vehiculos consultados en preventa, por marca
                    marca_pref_cliente_preventa = vehiculos_consultados_cliente_preventa.GroupBy(x => x.modelo.marca.id_marca);
                    //Obtener Lista de marcas en inventario que el cliente prefiere de acuerdo a la preventa
                    var marca_inv_pref_preventa = marca_pref_cliente_preventa.Where(x => marcas_inventario.Select(y => y.Key).Contains(x.Key));
                    //Calcular el total de vehiculos restante de las consulta anteriores
                    cant_veh_preventa_total = marca_inv_pref_preventa.Sum(x => x.Count());
                    
                }

                /////////////Estadisticas de Preventa////////////////// Se le asigna el 75% de la importancia en el calculo
                var transaccion_vehiculos_venta = consulta_transacciones.Where(x => x.tipo_transaccion_venta.id_tipo_venta == 2);
                var cant_veh_venta_total = 1;
                if (transaccion_vehiculos_venta.Count() > 0)
                {
                    //Obtener todos los vehiculos en los cuales el cliente estuvo interesado en venta
                    var vehiculos_consultados_cliente_venta = transaccion_vehiculos_venta.SelectMany(x => x.vehiculo);
                    //Agrupar los vehiculos consultados en venta, por marca
                    marca_pref_cliente_venta = vehiculos_consultados_cliente_venta.GroupBy(x => x.modelo.marca.id_marca);
                    //Obtener Lista de marcas en inventario que el cliente prefiere de acuerdo a la preventa
                    var marca_inv_pref_venta = marca_pref_cliente_venta.Where(x => marcas_inventario.Select(y => y.Key).Contains(x.Key));
                    //Calcular el total de vehiculos restante de las consulta anteriores
                    cant_veh_venta_total = marca_inv_pref_venta.Sum(x => x.Count());
                }

                
                foreach (var item_marca in marcas_inventario_preferidas_cliente)
                {
                    var cant_veh_preventa_marca = 0;
                    if (marca_pref_cliente_preventa.Where(x => x.Key == item_marca.Key).Count() > 0)
                    {
                        cant_veh_preventa_marca = marca_pref_cliente_preventa.Where(x => x.Key == item_marca.Key).First().Count();
                    }
                    var cant_veh_venta_marca = 0;
                    if (marca_pref_cliente_venta.Where(x => x.Key == item_marca.Key).Count() > 0)
                    {
                        cant_veh_venta_marca = marca_pref_cliente_venta.Where(x => x.Key == item_marca.Key).First().Count();
                    }
                    
                    
                    Venta_MarcasViewModels i_marca = new Venta_MarcasViewModels
                    {
                        Marca = inventario.Where(x => x.modelo.marca.id_marca == item_marca.Key).First().modelo.marca,
                        Nro_Inventario = item_marca.Count(),
                        Nivel_Preferencia = CalcularPreferenciaCliente_Ambos(cant_veh_preventa_marca, cant_veh_preventa_total, cant_veh_venta_marca, cant_veh_venta_total)
                    };
                    l_marcas.Add(i_marca);
                }
            }

            /////////CATEGORIA ////////
            //Obtener Lista de las Cat en Inventario
            var categoria_inventario = inventario.GroupBy(x => x.modelo.modelo_clasificacion.id_clasificacion);
            //Obtener Lista de las Cat preferidas por el cliente en general
            var categoria_preferidas_cliente = consulta_transacciones.SelectMany(x => x.vehiculo).GroupBy(x => x.modelo.modelo_clasificacion.id_clasificacion);
            if (categoria_preferidas_cliente.Count() > 0)
            {
                //Obtener las Cat que entasn en inventario y que el cliente prefiere
                var categoria_inventario_preferidas_cliente = categoria_inventario.Where(x => categoria_preferidas_cliente.Select(y => y.Key).Contains(x.Key));
                IQueryable<IGrouping<byte, vehiculo>> categoria_pref_cliente_preventa = null;
                IQueryable<IGrouping<byte, vehiculo>> categoria_pref_cliente_venta = null;
                /////////////Estadisticas de Preventa////////////////// Se le asigna el 25% de la importancia en el calculo
                var transaccion_vehiculos_preventa = consulta_transacciones.Where(x => x.tipo_transaccion_venta.id_tipo_venta == 1);
                var cant_veh_preventa_total = 1;
                if (transaccion_vehiculos_preventa.Count() > 0)
                {
                    //Obtener todos los vehiculos en los cuales el cliente estuvo interesado en preventa
                    var vehiculos_consultados_cliente_preventa = transaccion_vehiculos_preventa.SelectMany(x => x.vehiculo);
                    //Agrupar los vehiculos consultados en preventa, por categoria
                    categoria_pref_cliente_preventa = vehiculos_consultados_cliente_preventa.GroupBy(x => x.modelo.modelo_clasificacion.id_clasificacion);
                    //Obtener Lista de categoria en inventario que el cliente prefiere de acuerdo a la preventa
                    var categoria_inv_pref_preventa = categoria_pref_cliente_preventa.Where(x => categoria_inventario.Select(y => y.Key).Contains(x.Key));
                    //Calcular el total de vehiculos restante de las consulta anteriores
                    cant_veh_preventa_total = categoria_inv_pref_preventa.Sum(x => x.Count());
                    
                }

                /////////////Estadisticas de Preventa////////////////// Se le asigna el 75% de la importancia en el calculo
                var transaccion_vehiculos_venta = consulta_transacciones.Where(x => x.tipo_transaccion_venta.id_tipo_venta == 2);
                var cant_veh_venta_total = 1;
                if (transaccion_vehiculos_venta.Count() > 0)
                {
                    //Obtener todos los vehiculos en los cuales el cliente estuvo interesado en venta
                    var vehiculos_consultados_cliente_venta = transaccion_vehiculos_venta.SelectMany(x => x.vehiculo);
                    //Agrupar los vehiculos consultados en venta, por categoria
                    categoria_pref_cliente_venta = vehiculos_consultados_cliente_venta.GroupBy(x => x.modelo.modelo_clasificacion.id_clasificacion);
                    //Obtener Lista de categoria en inventario que el cliente prefiere de acuerdo a la preventa
                    var categoria_inv_pref_venta = categoria_pref_cliente_venta.Where(x => categoria_inventario.Select(y => y.Key).Contains(x.Key));
                    //Calcular el total de vehiculos restante de las consulta anteriores
                    cant_veh_venta_total = categoria_inv_pref_venta.Sum(x => x.Count());
                    
                }



                foreach (var item_cat in categoria_inventario_preferidas_cliente)
                {
                    //Calcular cantidad de vehiculos en preventas perteneciente a la marca
                    var cant_veh_preventa_categoria = 0;
                    if (categoria_pref_cliente_preventa.Where(x => x.Key == item_cat.Key).Count() > 0)
                    {
                        cant_veh_preventa_categoria = categoria_pref_cliente_preventa.Where(x => x.Key == item_cat.Key).First().Count();
                    }
                    //Calcular cantidad de vehiculos en preventas perteneciente a la marca
                    var cant_veh_venta_categoria = 0;
                    if (categoria_pref_cliente_venta.Where(x => x.Key == item_cat.Key).Count() > 0)
                    {
                        cant_veh_venta_categoria = categoria_pref_cliente_venta.Where(x => x.Key == item_cat.Key).First().Count();
                    }
                    
                    
                    Venta_CategoriaViewModels i_cat = new Venta_CategoriaViewModels
                    {
                        Categoria = inventario.Where(x => x.modelo.modelo_clasificacion.id_clasificacion == item_cat.Key).First().modelo.modelo_clasificacion,
                        Nro_Inventario = item_cat.Count(),
                        Nivel_Preferencia = CalcularPreferenciaCliente_Ambos(cant_veh_preventa_categoria, cant_veh_preventa_total, cant_veh_venta_categoria, cant_veh_venta_total)
                    };
                    l_categoria.Add(i_cat);
                }

            }

            var lista_ordenada_marcas = l_marcas.OrderByDescending(x => x.Nivel_Preferencia).ToList();
            var lista_ordenada_categoria = l_categoria.OrderByDescending(x => x.Nivel_Preferencia).ToList();
            modelo.Lista_Categoria = lista_ordenada_categoria;
            modelo.Lista_Marcas = lista_ordenada_marcas;
            modelo.id_cliente = id_cliente;
            return View(modelo);
        }


        public int CalcularPreferenciaCliente_Ambos(int cant_veh_preventa_marca, int cant_veh_preventa_total, int cant_veh_venta_marca, int cant_veh_venta_total)
        {
            int Nivel_Preferencia = 0;
            double factor_preventa = 0;
            double factor_venta = 0;
            
            factor_preventa = (Convert.ToDouble(cant_veh_preventa_marca) * 100) / Convert.ToDouble(cant_veh_preventa_total);
            factor_venta = (Convert.ToDouble(cant_veh_venta_marca) * 100) / Convert.ToDouble(cant_veh_venta_total);

         

            Nivel_Preferencia = Convert.ToInt32(factor_preventa * 0.25) + Convert.ToInt32(factor_venta * 0.75);

            return Nivel_Preferencia;
        }
        public int CalcularPreferenciaCliente_Marca(string id_cliente, int id_marca)
        {
            int Nivel_Preferencia = 0;
            double factor_preventa = 0;
            double factor_venta = 0;
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            var inventario = from l_vehiculos in autodb.vehiculo where l_vehiculos.fk_concesionario == usuario.id_Concesionario && l_vehiculos.fecha_salida == null select l_vehiculos;
            var consulta_transacciones = from l_tran in autodb.transaccion_venta where l_tran.fk_cliente == id_cliente select l_tran;
            //Obtener Lista de las Marcas en Inventario
            var marcas_inventario = inventario.GroupBy(x => x.modelo.marca.id_marca);

            /////////////Estadisticas de Preventa////////////////// Se le asigna el 25% de la importancia en el calculo
            var transaccion_vehiculos_preventa = consulta_transacciones.Where(x => x.tipo_transaccion_venta.id_tipo_venta == 1);
            if (transaccion_vehiculos_preventa.Count() > 0)
            {
                //Obtener todos los vehiculos en los cuales el cliente estuvo interesado en preventa
                var vehiculos_consultados_cliente_preventa = transaccion_vehiculos_preventa.SelectMany(x => x.vehiculo);
                //Agrupar los vehiculos consultados en preventa, por marca
                var marca_pref_cliente_preventa = vehiculos_consultados_cliente_preventa.GroupBy(x => x.modelo.marca.id_marca);
                //Obtener Lista de marcas en inventario que el cliente prefiere de acuerdo a la preventa
                var marca_inv_pref_preventa = marca_pref_cliente_preventa.Where(x => marcas_inventario.Select(y => y.Key).Contains(x.Key));
                //Calcular el total de vehiculos restante de las consulta anteriores
                var cant_veh_preventa_total = marca_inv_pref_preventa.Sum(x => x.Count());
                //Calcular cantidad de vehiculos en preventas perteneciente a la marca
                var cant_veh_preventa_marca = 0;
                if (marca_pref_cliente_preventa.Where(x => x.Key == id_marca).Count() > 0)
                {
                    cant_veh_preventa_marca = marca_pref_cliente_preventa.Where(x => x.Key == id_marca).First().Count();
                }
                

                factor_preventa = (Convert.ToDouble(cant_veh_preventa_marca) * 100) / Convert.ToDouble(cant_veh_preventa_total);


            }

            /////////////Estadisticas de Preventa////////////////// Se le asigna el 75% de la importancia en el calculo
            var transaccion_vehiculos_venta = consulta_transacciones.Where(x => x.tipo_transaccion_venta.id_tipo_venta == 2);
            if (transaccion_vehiculos_venta.Count() > 0)
            {
                //Obtener todos los vehiculos en los cuales el cliente estuvo interesado en venta
                var vehiculos_consultados_cliente_venta = transaccion_vehiculos_venta.SelectMany(x => x.vehiculo);
                //Agrupar los vehiculos consultados en venta, por marca
                var marca_pref_cliente_venta = vehiculos_consultados_cliente_venta.GroupBy(x => x.modelo.marca.id_marca);
                //Obtener Lista de marcas en inventario que el cliente prefiere de acuerdo a la preventa
                var marca_inv_pref_venta = marca_pref_cliente_venta.Where(x => marcas_inventario.Select(y => y.Key).Contains(x.Key));
                //Calcular el total de vehiculos restante de las consulta anteriores
                var cant_veh_venta_total = marca_inv_pref_venta.Sum(x => x.Count());
                //Calcular cantidad de vehiculos en preventas perteneciente a la marca
                var cant_veh_venta_marca = 0;
                if(marca_pref_cliente_venta.Where(x => x.Key == id_marca).Count() > 0)
                {
                    cant_veh_venta_marca = marca_pref_cliente_venta.Where(x => x.Key == id_marca).First().Count();
                }

                factor_venta = (Convert.ToDouble(cant_veh_venta_marca) * 100) / Convert.ToDouble(cant_veh_venta_total);

            }

            Nivel_Preferencia = Convert.ToInt32(factor_preventa * 0.25) + Convert.ToInt32(factor_venta * 0.75); 

            return Nivel_Preferencia;
        }

        public int CalcularPreferenciaCliente_Categoria(string id_cliente, int id_categoria)
        {
            int Nivel_Preferencia = 0;
            double factor_preventa = 0;
            double factor_venta = 0;
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            var inventario = from l_vehiculos in autodb.vehiculo where l_vehiculos.fk_concesionario == usuario.id_Concesionario && l_vehiculos.fecha_salida == null select l_vehiculos;
            var consulta_transacciones = from l_tran in autodb.transaccion_venta where l_tran.fk_cliente == id_cliente select l_tran;
            //Obtener Lista de las Categoria en Inventario
            var categoria_inventario = inventario.GroupBy(x => x.modelo.modelo_clasificacion.id_clasificacion);

            /////////////Estadisticas de Preventa////////////////// Se le asigna el 25% de la importancia en el calculo
            var transaccion_vehiculos_preventa = consulta_transacciones.Where(x => x.tipo_transaccion_venta.id_tipo_venta == 1);
            if (transaccion_vehiculos_preventa.Count() > 0)
            {
                //Obtener todos los vehiculos en los cuales el cliente estuvo interesado en preventa
                var vehiculos_consultados_cliente_preventa = transaccion_vehiculos_preventa.SelectMany(x => x.vehiculo);
                //Agrupar los vehiculos consultados en preventa, por categoria
                var categoria_pref_cliente_preventa = vehiculos_consultados_cliente_preventa.GroupBy(x => x.modelo.modelo_clasificacion.id_clasificacion);
                //Obtener Lista de categoria en inventario que el cliente prefiere de acuerdo a la preventa
                var categoria_inv_pref_preventa = categoria_pref_cliente_preventa.Where(x => categoria_inventario.Select(y => y.Key).Contains(x.Key));
                //Calcular el total de vehiculos restante de las consulta anteriores
                var cant_veh_preventa_total = categoria_inv_pref_preventa.Sum(x => x.Count());
                //Calcular cantidad de vehiculos en preventas perteneciente a la marca
                var cant_veh_preventa_categoria = 0;
                if (categoria_pref_cliente_preventa.Where(x => x.Key == id_categoria).Count() > 0)
                {
                    cant_veh_preventa_categoria = categoria_pref_cliente_preventa.Where(x => x.Key == id_categoria).First().Count();
                }


                factor_preventa = (Convert.ToDouble(cant_veh_preventa_categoria) * 100) / Convert.ToDouble(cant_veh_preventa_total);


            }

            /////////////Estadisticas de Preventa////////////////// Se le asigna el 75% de la importancia en el calculo
            var transaccion_vehiculos_venta = consulta_transacciones.Where(x => x.tipo_transaccion_venta.id_tipo_venta == 2);
            if (transaccion_vehiculos_venta.Count() > 0)
            {
                //Obtener todos los vehiculos en los cuales el cliente estuvo interesado en venta
                var vehiculos_consultados_cliente_venta = transaccion_vehiculos_venta.SelectMany(x => x.vehiculo);
                //Agrupar los vehiculos consultados en venta, por categoria
                var categoria_pref_cliente_venta = vehiculos_consultados_cliente_venta.GroupBy(x => x.modelo.modelo_clasificacion.id_clasificacion);
                //Obtener Lista de categoria en inventario que el cliente prefiere de acuerdo a la preventa
                var categoria_inv_pref_venta = categoria_pref_cliente_venta.Where(x => categoria_inventario.Select(y => y.Key).Contains(x.Key));
                //Calcular el total de vehiculos restante de las consulta anteriores
                var cant_veh_venta_total = categoria_inv_pref_venta.Sum(x => x.Count());
                //Calcular cantidad de vehiculos en preventas perteneciente a la marca
                var cant_veh_venta_categoria = 0;
                if (categoria_pref_cliente_venta.Where(x => x.Key == id_categoria).Count() > 0)
                {
                    cant_veh_venta_categoria = categoria_pref_cliente_venta.Where(x => x.Key == id_categoria).First().Count();
                }
                factor_venta = (Convert.ToDouble(cant_veh_venta_categoria) * 100) / Convert.ToDouble(cant_veh_venta_total);

            }

            Nivel_Preferencia = Convert.ToInt32(factor_preventa * 0.25) + Convert.ToInt32(factor_venta * 0.75);

            return Nivel_Preferencia;
        }

        public int CalcularPreferenciaCliente_Modelo(string id_cliente, int id_modelo)
        {
            int Nivel_Preferencia = 0;
            double factor_preventa = 0;
            double factor_venta = 0;
           
            var consulta_transacciones = from l_tran in autodb.transaccion_venta where l_tran.fk_cliente == id_cliente select l_tran;
            if(consulta_transacciones.Count() > 0 )
            {
                var consulta_t_preventa = consulta_transacciones.Where(x => x.fk_tipo_venta == 1);
                if(consulta_t_preventa.Count() > 0 )
                {
                    var consulta_t_pre_modelo = consulta_t_preventa.Where(x => x.vehiculo.Where(y => y.modelo.id_modelo == id_modelo).Count() > 0);
                    if (consulta_t_pre_modelo.Count() > 0)
                    {
                        var cant_vehiculo_modelo = consulta_t_pre_modelo.Sum(x => x.vehiculo.Where(y => y.modelo.id_modelo == id_modelo).Count());
                        var cant_vehiculo_total = consulta_t_preventa.Sum(x => x.vehiculo.Count());
                        factor_preventa = Convert.ToDouble(cant_vehiculo_modelo) * 100 / cant_vehiculo_total;
                    }
                }
                var consulta_t_venta = consulta_transacciones.Where(x => x.fk_tipo_venta == 2);
                if (consulta_t_venta.Count() > 0)
                {
                    var consulta_t_ven_modelo = consulta_t_preventa.Where(x => x.vehiculo.Where(y => y.modelo.id_modelo == id_modelo).Count() > 0);
                    if (consulta_t_ven_modelo.Count() > 0)
                    {
                        var cant_vehiculo_modelo = consulta_t_ven_modelo.Sum(x => x.vehiculo.Where(y => y.modelo.id_modelo == id_modelo).Count());
                        var cant_vehiculo_total = consulta_t_venta.Sum(x => x.vehiculo.Count());
                        factor_venta = Convert.ToDouble(cant_vehiculo_modelo) * 100 / cant_vehiculo_total;
                    }
                }



            }

            Nivel_Preferencia = Convert.ToInt32(factor_preventa * 0.25) + Convert.ToInt32(factor_venta * 0.75);

            return Nivel_Preferencia;
        }

        //CONSULTAR 
        public ActionResult Prueba2(int id, int tipo, string id_cliente)
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            Venta_VehiculoViewModels modelo = new Venta_VehiculoViewModels();
            List<Venta_ModeloViewModels> lista_vehiculos = new List<Venta_ModeloViewModels>();
            IQueryable<vehiculo> consultar_vehiculos = null;
            var titulo = "";
            if(tipo == 1)
            {
                //Marca
                consultar_vehiculos = from l_vehiculos in autodb.vehiculo where l_vehiculos.fk_concesionario == usuario.id_Concesionario && l_vehiculos.fecha_salida == null && l_vehiculos.modelo.marca.id_marca ==  id select l_vehiculos;
                if(consultar_vehiculos.Count() > 0)
                {
                   titulo = "VEHICULOS EN INVENTARIO - MARCA " + consultar_vehiculos.First().modelo.marca.nombre.ToUpper();
   


                }else{
                    MensajeViewModels m1 = new MensajeViewModels
                    {
                         Titulo = "Error en Operación",
                         Cuerpo = "No existen vehiculos en la consulta",
                         Tipo_Modal = "modal-danger"
                    };

                    this.Session["Mensaje"] = m1;
                    return RedirectToAction("Index", "Home");
                }
                
            }else{
                //Categoria
                consultar_vehiculos = from l_vehiculos in autodb.vehiculo where l_vehiculos.fk_concesionario == usuario.id_Concesionario && l_vehiculos.fecha_salida == null && l_vehiculos.modelo.modelo_clasificacion.id_clasificacion == id select l_vehiculos;
                if(consultar_vehiculos.Count() > 0)
                {
                   titulo = "VEHICULOS EN INVENTARIO - CATEGORIA " + consultar_vehiculos.First().modelo.modelo_clasificacion.descripcion.ToUpper();
   
                }else{
                    MensajeViewModels m1 = new MensajeViewModels
                    {
                         Titulo = "Error en Operación",
                         Cuerpo = "No existen vehiculos en la consulta",
                         Tipo_Modal = "modal-danger"
                    };

                    this.Session["Mensaje"] = m1;
                    return RedirectToAction("Index", "Home");
                }
            }

            foreach (var item_vehiculo in consultar_vehiculos)
            {
                
                Venta_ModeloViewModels i_vehiculo = new Venta_ModeloViewModels
                {
                   Vehiculo = item_vehiculo,
                   Imagen = ObtenerImagenVehiculo(item_vehiculo),
                   Cant_Dias_Inventario = ObtenerDiasEnInventario(item_vehiculo).ToString(),
                   Monto_Venta = ObtenerPrecio_Venta_Total_Recomendado(item_vehiculo)
                };

                lista_vehiculos.Add(i_vehiculo);
            }

            modelo.Lista_Vehiculos = lista_vehiculos;
            modelo.Titulo = titulo;
            modelo.ID_Cliente = id_cliente;

            return View(modelo);
        }

        public ActionResult Menu_DetallesVehiculo(int id, string id_cliente)
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            VehiculosDetallesViewModels modelo = new VehiculosDetallesViewModels();
            var consulta_vehiculo = from l_vehiculos in autodb.vehiculo where l_vehiculos.id_vehiculo == id select l_vehiculos;

            if (consulta_vehiculo.Count() > 0)
            {
                var item_vehiculo = consulta_vehiculo.First();
                modelo.Imagen = ObtenerImagenVehiculo(item_vehiculo);
                modelo.Lista_Agentes = ObtenerAgentesFinanciamiento(usuario.id_Concesionario);
                modelo.Monto_Maximo = ObtenerPrecio_Venta_Maximo(item_vehiculo);
                modelo.Monto_Minimo = ObtenerPrecio_Venta_Minimo(item_vehiculo);
                modelo.Nivel_Pref_Cliente = ObtenerPreferencia_Cliente_Vehiculo(item_vehiculo, id_cliente);
                modelo.Tiempo_Inventario = ObtenerDiasEnInventario(item_vehiculo).ToString();
                modelo.Vehiculo = item_vehiculo;
                modelo.Comision_Obtener = CalcularComision(modelo.Monto_Maximo, usuario.id_Usuario);
                modelo.ID_Cliente = id_cliente;
                modelo.ID_Usuario = usuario.id_Usuario;
            }
            else
            {
                MensajeViewModels m1 = new MensajeViewModels
                {
                    Titulo = "Error en Operación",
                    Cuerpo = "No existen vehiculos ese vehiculo en la consulta.",
                    Tipo_Modal = "modal-danger"
                };

                this.Session["Mensaje"] = m1;
                return RedirectToAction("Index", "Home");

            }



            return PartialView("_Menu_DetallesVehiculo", modelo);
        }

        public ActionResult Menu_ListaVehiculos(int id, int tipo, string id_cliente)
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            Venta_VehiculoViewModels modelo = new Venta_VehiculoViewModels();
            List<Venta_ModeloViewModels> lista_vehiculos = new List<Venta_ModeloViewModels>();
            IQueryable<vehiculo> consultar_vehiculos = null;
            var titulo = "";
            if (tipo == 1)
            {
                //Marca
                consultar_vehiculos = from l_vehiculos in autodb.vehiculo where l_vehiculos.fk_concesionario == usuario.id_Concesionario && l_vehiculos.fecha_salida == null && l_vehiculos.modelo.marca.id_marca == id select l_vehiculos;
                if (consultar_vehiculos.Count() > 0)
                {
                    titulo = "VEHICULOS EN INVENTARIO - MARCA " + consultar_vehiculos.First().modelo.marca.nombre.ToUpper();



                }
                else
                {
                    MensajeViewModels m1 = new MensajeViewModels
                    {
                        Titulo = "Error en Operación",
                        Cuerpo = "No existen vehiculos en la consulta",
                        Tipo_Modal = "modal-danger"
                    };

                    this.Session["Mensaje"] = m1;
                    return RedirectToAction("Index", "Home");
                }

            }
            else
            {
                //Categoria
                consultar_vehiculos = from l_vehiculos in autodb.vehiculo where l_vehiculos.fk_concesionario == usuario.id_Concesionario && l_vehiculos.fecha_salida == null && l_vehiculos.modelo.modelo_clasificacion.id_clasificacion == id select l_vehiculos;
                if (consultar_vehiculos.Count() > 0)
                {
                    titulo = "VEHICULOS EN INVENTARIO - CATEGORIA " + consultar_vehiculos.First().modelo.modelo_clasificacion.descripcion.ToUpper();

                }
                else
                {
                    MensajeViewModels m1 = new MensajeViewModels
                    {
                        Titulo = "Error en Operación",
                        Cuerpo = "No existen vehiculos en la consulta",
                        Tipo_Modal = "modal-danger"
                    };

                    this.Session["Mensaje"] = m1;
                    return RedirectToAction("Index", "Home");
                }
            }

            foreach (var item_vehiculo in consultar_vehiculos)
            {

                Venta_ModeloViewModels i_vehiculo = new Venta_ModeloViewModels
                {
                    Vehiculo = item_vehiculo,
                    Imagen = ObtenerImagenVehiculo(item_vehiculo),
                    Cant_Dias_Inventario = ObtenerDiasEnInventario(item_vehiculo).ToString(),
                    Monto_Venta = ObtenerPrecio_Venta_Total_Recomendado(item_vehiculo)
                };

                lista_vehiculos.Add(i_vehiculo);
            }

            modelo.Lista_Vehiculos = lista_vehiculos;
            modelo.Titulo = titulo;
            modelo.ID_Cliente = id_cliente;

            return PartialView("_Menu_ListaVehiculos",modelo);
        }

        public ActionResult Menu_Preferidos(int tipo, string id_cliente)
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            Marcas_CategoriaViewModels modelo = new Marcas_CategoriaViewModels();
            List<Venta_MarcasViewModels> l_marcas = new List<Venta_MarcasViewModels>();
            List<Venta_CategoriaViewModels> l_categoria = new List<Venta_CategoriaViewModels>();
            var inventario = from l_vehiculos in autodb.vehiculo where l_vehiculos.fk_concesionario == usuario.id_Concesionario && l_vehiculos.fecha_salida == null select l_vehiculos;
            IQueryable<transaccion_venta> consulta_transacciones = null;

            //Preferencia del Cliente
            if (tipo == 1)
            {
                consulta_transacciones = from l_tran in autodb.transaccion_venta where l_tran.fk_cliente == id_cliente select l_tran;


            }
            else
            {
                //Preferencia del Mercado
                var c_usuario = (from l_concesionario in autodb.concesionario where l_concesionario.id_concesionario == usuario.id_Concesionario select l_concesionario).First();
                consulta_transacciones = from l_tran in autodb.transaccion_venta where l_tran.concesionario.direccion.ciudad.estado.id_estado == c_usuario.direccion.ciudad.estado.id_estado select l_tran;
            }

            /////////MARCAS ////////
            //Obtener Lista de las Marcas en Inventario
            var marcas_inventario = inventario.GroupBy(x => x.modelo.marca.id_marca);
            //Obtener Lista de las Marcar preferidas por el cliente en general
            var marcas_preferidas_cliente = consulta_transacciones.SelectMany(x => x.vehiculo).GroupBy(x => x.modelo.marca.id_marca);

            if (marcas_preferidas_cliente.Count() > 0)
            {
                //Obtener las Marcas que entasn en inventario y que el cliente prefiere
                var marcas_inventario_preferidas_cliente = marcas_inventario.Where(x => marcas_preferidas_cliente.Select(y => y.Key).Contains(x.Key));
                IQueryable<IGrouping<int, vehiculo>> marca_pref_cliente_preventa = null;
                IQueryable<IGrouping<int, vehiculo>> marca_pref_cliente_venta = null;
                /////////////Estadisticas de Preventa////////////////// Se le asigna el 25% de la importancia en el calculo
                var transaccion_vehiculos_preventa = consulta_transacciones.Where(x => x.tipo_transaccion_venta.id_tipo_venta == 1);
                var cant_veh_preventa_total = 1;
                if (transaccion_vehiculos_preventa.Count() > 0)
                {
                    //Obtener todos los vehiculos en los cuales el cliente estuvo interesado en preventa
                    var vehiculos_consultados_cliente_preventa = transaccion_vehiculos_preventa.SelectMany(x => x.vehiculo);
                    //Agrupar los vehiculos consultados en preventa, por marca
                    marca_pref_cliente_preventa = vehiculos_consultados_cliente_preventa.GroupBy(x => x.modelo.marca.id_marca);
                    //Obtener Lista de marcas en inventario que el cliente prefiere de acuerdo a la preventa
                    var marca_inv_pref_preventa = marca_pref_cliente_preventa.Where(x => marcas_inventario.Select(y => y.Key).Contains(x.Key));
                    //Calcular el total de vehiculos restante de las consulta anteriores
                    cant_veh_preventa_total = marca_inv_pref_preventa.Sum(x => x.Count());

                }

                /////////////Estadisticas de Preventa////////////////// Se le asigna el 75% de la importancia en el calculo
                var transaccion_vehiculos_venta = consulta_transacciones.Where(x => x.tipo_transaccion_venta.id_tipo_venta == 2);
                var cant_veh_venta_total = 1;
                if (transaccion_vehiculos_venta.Count() > 0)
                {
                    //Obtener todos los vehiculos en los cuales el cliente estuvo interesado en venta
                    var vehiculos_consultados_cliente_venta = transaccion_vehiculos_venta.SelectMany(x => x.vehiculo);
                    //Agrupar los vehiculos consultados en venta, por marca
                    marca_pref_cliente_venta = vehiculos_consultados_cliente_venta.GroupBy(x => x.modelo.marca.id_marca);
                    //Obtener Lista de marcas en inventario que el cliente prefiere de acuerdo a la preventa
                    var marca_inv_pref_venta = marca_pref_cliente_venta.Where(x => marcas_inventario.Select(y => y.Key).Contains(x.Key));
                    //Calcular el total de vehiculos restante de las consulta anteriores
                    cant_veh_venta_total = marca_inv_pref_venta.Sum(x => x.Count());
                }


                foreach (var item_marca in marcas_inventario_preferidas_cliente)
                {
                    var cant_veh_preventa_marca = 0;
                    if (marca_pref_cliente_preventa.Where(x => x.Key == item_marca.Key).Count() > 0)
                    {
                        cant_veh_preventa_marca = marca_pref_cliente_preventa.Where(x => x.Key == item_marca.Key).First().Count();
                    }
                    var cant_veh_venta_marca = 0;
                    if (marca_pref_cliente_venta.Where(x => x.Key == item_marca.Key).Count() > 0)
                    {
                        cant_veh_venta_marca = marca_pref_cliente_venta.Where(x => x.Key == item_marca.Key).First().Count();
                    }


                    Venta_MarcasViewModels i_marca = new Venta_MarcasViewModels
                    {
                        Marca = inventario.Where(x => x.modelo.marca.id_marca == item_marca.Key).First().modelo.marca,
                        Nro_Inventario = item_marca.Count(),
                        Nivel_Preferencia = CalcularPreferenciaCliente_Ambos(cant_veh_preventa_marca, cant_veh_preventa_total, cant_veh_venta_marca, cant_veh_venta_total)
                    };
                    l_marcas.Add(i_marca);
                }
            }

            /////////CATEGORIA ////////
            //Obtener Lista de las Cat en Inventario
            var categoria_inventario = inventario.GroupBy(x => x.modelo.modelo_clasificacion.id_clasificacion);
            //Obtener Lista de las Cat preferidas por el cliente en general
            var categoria_preferidas_cliente = consulta_transacciones.SelectMany(x => x.vehiculo).GroupBy(x => x.modelo.modelo_clasificacion.id_clasificacion);
            if (categoria_preferidas_cliente.Count() > 0)
            {
                //Obtener las Cat que entasn en inventario y que el cliente prefiere
                var categoria_inventario_preferidas_cliente = categoria_inventario.Where(x => categoria_preferidas_cliente.Select(y => y.Key).Contains(x.Key));
                IQueryable<IGrouping<byte, vehiculo>> categoria_pref_cliente_preventa = null;
                IQueryable<IGrouping<byte, vehiculo>> categoria_pref_cliente_venta = null;
                /////////////Estadisticas de Preventa////////////////// Se le asigna el 25% de la importancia en el calculo
                var transaccion_vehiculos_preventa = consulta_transacciones.Where(x => x.tipo_transaccion_venta.id_tipo_venta == 1);
                var cant_veh_preventa_total = 1;
                if (transaccion_vehiculos_preventa.Count() > 0)
                {
                    //Obtener todos los vehiculos en los cuales el cliente estuvo interesado en preventa
                    var vehiculos_consultados_cliente_preventa = transaccion_vehiculos_preventa.SelectMany(x => x.vehiculo);
                    //Agrupar los vehiculos consultados en preventa, por categoria
                    categoria_pref_cliente_preventa = vehiculos_consultados_cliente_preventa.GroupBy(x => x.modelo.modelo_clasificacion.id_clasificacion);
                    //Obtener Lista de categoria en inventario que el cliente prefiere de acuerdo a la preventa
                    var categoria_inv_pref_preventa = categoria_pref_cliente_preventa.Where(x => categoria_inventario.Select(y => y.Key).Contains(x.Key));
                    //Calcular el total de vehiculos restante de las consulta anteriores
                    cant_veh_preventa_total = categoria_inv_pref_preventa.Sum(x => x.Count());

                }

                /////////////Estadisticas de Preventa////////////////// Se le asigna el 75% de la importancia en el calculo
                var transaccion_vehiculos_venta = consulta_transacciones.Where(x => x.tipo_transaccion_venta.id_tipo_venta == 2);
                var cant_veh_venta_total = 1;
                if (transaccion_vehiculos_venta.Count() > 0)
                {
                    //Obtener todos los vehiculos en los cuales el cliente estuvo interesado en venta
                    var vehiculos_consultados_cliente_venta = transaccion_vehiculos_venta.SelectMany(x => x.vehiculo);
                    //Agrupar los vehiculos consultados en venta, por categoria
                    categoria_pref_cliente_venta = vehiculos_consultados_cliente_venta.GroupBy(x => x.modelo.modelo_clasificacion.id_clasificacion);
                    //Obtener Lista de categoria en inventario que el cliente prefiere de acuerdo a la preventa
                    var categoria_inv_pref_venta = categoria_pref_cliente_venta.Where(x => categoria_inventario.Select(y => y.Key).Contains(x.Key));
                    //Calcular el total de vehiculos restante de las consulta anteriores
                    cant_veh_venta_total = categoria_inv_pref_venta.Sum(x => x.Count());

                }



                foreach (var item_cat in categoria_inventario_preferidas_cliente)
                {
                    //Calcular cantidad de vehiculos en preventas perteneciente a la marca
                    var cant_veh_preventa_categoria = 0;
                    if (categoria_pref_cliente_preventa.Where(x => x.Key == item_cat.Key).Count() > 0)
                    {
                        cant_veh_preventa_categoria = categoria_pref_cliente_preventa.Where(x => x.Key == item_cat.Key).First().Count();
                    }
                    //Calcular cantidad de vehiculos en preventas perteneciente a la marca
                    var cant_veh_venta_categoria = 0;
                    if (categoria_pref_cliente_venta.Where(x => x.Key == item_cat.Key).Count() > 0)
                    {
                        cant_veh_venta_categoria = categoria_pref_cliente_venta.Where(x => x.Key == item_cat.Key).First().Count();
                    }


                    Venta_CategoriaViewModels i_cat = new Venta_CategoriaViewModels
                    {
                        Categoria = inventario.Where(x => x.modelo.modelo_clasificacion.id_clasificacion == item_cat.Key).First().modelo.modelo_clasificacion,
                        Nro_Inventario = item_cat.Count(),
                        Nivel_Preferencia = CalcularPreferenciaCliente_Ambos(cant_veh_preventa_categoria, cant_veh_preventa_total, cant_veh_venta_categoria, cant_veh_venta_total)
                    };
                    l_categoria.Add(i_cat);
                }

            }

            var lista_ordenada_marcas = l_marcas.OrderByDescending(x => x.Nivel_Preferencia).ToList();
            var lista_ordenada_categoria = l_categoria.OrderByDescending(x => x.Nivel_Preferencia).ToList();
            modelo.Lista_Categoria = lista_ordenada_categoria;
            modelo.Lista_Marcas = lista_ordenada_marcas;
            modelo.id_cliente = id_cliente;
            return PartialView("_Menu_Preferidos", modelo);


        }

        public decimal ObtenerPrecio_Venta_Total_Recomendado(vehiculo v1)
        {
            decimal Monto = 0;
            var porcentaje_ganancia_concesionario = v1.concesionario.porcentaje_ganancia;

            var valor_concesionario = ((v1.valor_compra * Convert.ToDecimal(porcentaje_ganancia_concesionario)) / 100) + v1.valor_compra;
            decimal valor_financia_banco = 0;
            if (ConsultarPreferenciaBanco (v1.modelo) == true)
            {
                valor_financia_banco = v1.modelo.banco_financia_modelo.Where(x => x.valor_limite == v1.modelo.banco_financia_modelo.Max(y => y.valor_limite)).First().valor_limite;
            }

            if(valor_financia_banco > valor_concesionario)
            {
                Monto = valor_financia_banco;
            }else{
                Monto = valor_concesionario;
            }


            return Monto;
        }

        public bool ConsultarPreferenciaBanco(modelo m1)
        {
            if (m1.banco_financia_modelo.Count() != 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public int ObtenerDiasEnInventario(vehiculo v1)
        {
            int dias = 0;

            var dia_final = DateTime.Now;
            var total_dias = dia_final.Subtract(v1.fecha_ingreso).TotalDays;
            dias = Convert.ToInt32(total_dias);
            return dias;

        }

        public string ObtenerImagenVehiculo(vehiculo v1)
        {
            string url_imagen = "";

            if((v1.imagen != null) && (v1.imagen != ""))
            {
                url_imagen = v1.imagen;
            }
            else
            {
                url_imagen = v1.modelo.imagen;
            }


            return url_imagen;
        }

        [Authorize]
        public ActionResult PreventaMenuInicial(string id_cliente)
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            Preventa_MenuViewModels modelo = new Preventa_MenuViewModels();
            var concesionarios = (from l_concesionario in autodb.concesionario where l_concesionario.id_concesionario == usuario.id_Concesionario select l_concesionario).First();
            var inventario = from l_vehiculos in autodb.vehiculo where l_vehiculos.fk_concesionario == usuario.id_Concesionario && l_vehiculos.fecha_salida == null select l_vehiculos;
            int Cant_prioritario = 0;
            int Cant_Pref_Cliente = 0;
            int Cant_Pref_Mercado = 0;
            int Cant_Inventario = 0;
            if(inventario.Count() > 0)
            {
                Cant_Inventario = inventario.Count();
                Cant_prioritario = inventario.Where(x => DbFunctions.DiffDays(x.fecha_ingreso, DateTime.Now) > concesionarios.dia_max_inventario).Count();

                //Lista de Marcas y Categoria que le gusta al cliente
                var consultar_transaccion = from l_transaccion in autodb.transaccion_venta where l_transaccion.fk_cliente == id_cliente select l_transaccion;
                var lista_vehiculos = consultar_transaccion.SelectMany(x => x.vehiculo);
                var marcas_pref = lista_vehiculos.GroupBy(x => x.modelo.marca.id_marca);
                var categoria_pref = lista_vehiculos.GroupBy(x => x.modelo.modelo_clasificacion.id_clasificacion);
                var lista_vehiculos_marca = inventario.Where(x => marcas_pref.Select(y => y.Key).Contains(x.modelo.fk_marca));
                var lista_vehiculos_categoria = inventario.Where(x => categoria_pref.Select(y => y.Key).Contains(x.modelo.fk_clasificacion));
                if((lista_vehiculos_categoria.Count() > 0) && (lista_vehiculos_marca.Count() > 0))
                {
                    var lista_merged = lista_vehiculos_marca.Union(lista_vehiculos_categoria);
                    Cant_Pref_Cliente = lista_merged.Count();
                }
                else 
                {
                    if (lista_vehiculos_categoria.Count() > 0)
                    {
                        Cant_Pref_Cliente = lista_vehiculos_categoria.Count();
                    }
                    if(lista_vehiculos_marca.Count() > 0)
                    {
                        Cant_Pref_Cliente = lista_vehiculos_marca.Count();
                    }

                }
                //Lista para Marca y Categoria Pref Por mercado
                consultar_transaccion = from l_transaccion in autodb.transaccion_venta where l_transaccion.concesionario.direccion.ciudad.estado.id_estado == concesionarios.direccion.ciudad.estado.id_estado select l_transaccion;
                lista_vehiculos = consultar_transaccion.SelectMany(x => x.vehiculo);
                marcas_pref = lista_vehiculos.GroupBy(x => x.modelo.marca.id_marca);
                categoria_pref = lista_vehiculos.GroupBy(x => x.modelo.modelo_clasificacion.id_clasificacion);
                lista_vehiculos_marca = inventario.Where(x => marcas_pref.Select(y => y.Key).Contains(x.modelo.fk_marca));
                lista_vehiculos_categoria = inventario.Where(x => categoria_pref.Select(y => y.Key).Contains(x.modelo.fk_clasificacion));
                if ((lista_vehiculos_categoria.Count() > 0) && (lista_vehiculos_marca.Count() > 0))
                {
                    var lista_merged = lista_vehiculos_marca.Union(lista_vehiculos_categoria);
                    Cant_Pref_Mercado = lista_merged.Count();
                }
                else
                {
                    if (lista_vehiculos_categoria.Count() > 0)
                    {
                        Cant_Pref_Mercado = lista_vehiculos_categoria.Count();
                    }
                    if (lista_vehiculos_marca.Count() > 0)
                    {
                        Cant_Pref_Mercado = lista_vehiculos_marca.Count();
                    }

                }
            }


            modelo.Cant_Inventario = Cant_Inventario;
            modelo.Cant_Pref_Cliente = Cant_Pref_Cliente;
            modelo.Cant_Pref_Mercado = Cant_Pref_Mercado;
            modelo.Cant_Prioritarios = Cant_prioritario;
            modelo.ID_Cliente = id_cliente;
            return PartialView("_PreventaMenuInicial", modelo);
        }


        public transaccion_venta ObtenerTransaccion_Preventa_EnCurso(string id_cliente )
        {
            transaccion_venta trans_cliente = new transaccion_venta();
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            if (usuario.id_T_Preventa != 0)
            {
                trans_cliente = (from l_transaccion in autodb.transaccion_venta where l_transaccion.id_venta == usuario.id_T_Venta select l_transaccion).First();

            }
            else
            {
                transaccion_venta t1 = new transaccion_venta
                {
                    fk_cliente = id_cliente,
                    fk_tipo_venta = 1,
                    fk_concesionario = usuario.id_Concesionario,
                    fk_estado_transaccion = 2,
                    fk_usuario = usuario.id_Usuario,
                    fecha = DateTime.Now

                };

                trans_cliente = autodb.transaccion_venta.Add(t1);
                autodb.SaveChanges();
                usuario.id_T_Preventa = trans_cliente.id_venta;
                this.Session["User"] = usuario;
            }

            return trans_cliente;

        }

        [HttpPost]
        [Authorize]
        public ActionResult AgregarVehiculo_Transaccion(VehiculosDetallesViewModels modelo)
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            transaccion_venta trans_cliente = new transaccion_venta();
            var auto = (from l_vehiculo in autodb.vehiculo where l_vehiculo.id_vehiculo == modelo.Vehiculo.id_vehiculo select l_vehiculo).First();

            if(usuario.id_T_Venta != 0)
            {
                trans_cliente = (from l_transaccion in autodb.transaccion_venta where l_transaccion.id_venta == usuario.id_T_Venta select l_transaccion).First();

            }
            else
            {
                transaccion_venta t1 = new transaccion_venta
                {
                    fk_cliente =  modelo.ID_Cliente,
                    fk_tipo_venta = 2,
                    fk_concesionario = usuario.id_Concesionario,
                    fk_estado_transaccion = 4,
                    fk_usuario = usuario.id_Usuario,
                    fecha = DateTime.Now

                };
                
                trans_cliente = autodb.transaccion_venta.Add(t1);
                autodb.SaveChanges();
                usuario.id_T_Venta = trans_cliente.id_venta;
                this.Session["User"] = usuario;
            }

            trans_cliente.vehiculo.Add(auto);
            try
            {
                autodb.SaveChanges();
                MensajeViewModels m1 = new MensajeViewModels
                {
                    Titulo = "Transacción Completada",
                    Cuerpo = "La operacion Nro. " + trans_cliente.id_venta + " se a completado con exito. Se ha agregado un Vehiculo: " + auto.modelo.modelo1 + " " + auto.modelo.nombre + " - " + auto.modelo.año + ", en la lista de compra.",
                    Tipo_Modal = "modal-success"
                };
                this.Session["Mensaje"] = m1;
                return Json(new { success = true });
            }catch(Exception e)
            {

                return Json(new { success = false });
            }
            
            

        }

        public ActionResult Prueba3(int id, string id_cliente)
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            VehiculosDetallesViewModels modelo = new VehiculosDetallesViewModels();
            var consulta_vehiculo = from l_vehiculos in autodb.vehiculo where l_vehiculos.id_vehiculo == id select l_vehiculos;
           
            if(consulta_vehiculo.Count() > 0)
            {
                var item_vehiculo = consulta_vehiculo.First();
                modelo.Imagen = ObtenerImagenVehiculo(item_vehiculo);
                modelo.Lista_Agentes = ObtenerAgentesFinanciamiento(usuario.id_Concesionario);
                modelo.Monto_Maximo = ObtenerPrecio_Venta_Maximo(item_vehiculo);
                modelo.Monto_Minimo = ObtenerPrecio_Venta_Minimo(item_vehiculo);
                modelo.Nivel_Pref_Cliente = ObtenerPreferencia_Cliente_Vehiculo(item_vehiculo,id_cliente);
                modelo.Tiempo_Inventario = ObtenerDiasEnInventario(item_vehiculo).ToString();
                modelo.Vehiculo = item_vehiculo;
                modelo.Comision_Obtener = CalcularComision(modelo.Monto_Maximo, usuario.id_Usuario);
                modelo.ID_Cliente = id_cliente;
                modelo.ID_Usuario = usuario.id_Usuario;
            }
            else
            {
                MensajeViewModels m1 = new MensajeViewModels
                {
                    Titulo = "Error en Operación",
                    Cuerpo = "No existen vehiculos ese vehiculo en la consulta.",
                    Tipo_Modal = "modal-danger"
                };

                this.Session["Mensaje"] = m1;
                return RedirectToAction("Index", "Home");

            }



            return View(modelo);
        }

        [HttpPost]
        public decimal CalcularComision(decimal monto, int id_usuario)
        {
            decimal Monto = 0;
            var i_usuario = (from l_usuario in autodb.usuario where l_usuario.id_usuario == id_usuario select l_usuario).First();
            var concesionario = (from l_concesionario in autodb.concesionario where l_concesionario.id_concesionario == i_usuario.fk_concesionario select l_concesionario).First();
            decimal monto_minimo_concesionario = 0;
            if(concesionario.comision_minima != null)
            {
                monto_minimo_concesionario = Convert.ToDecimal(concesionario.comision_minima);
            }
            var tipo_comision = i_usuario.estructura_comision.Last().fk_tipo_comision;
            decimal monto_por_comision = 0;
            
            if(tipo_comision == 1)
            {
                monto_por_comision = (monto * Convert.ToDecimal(i_usuario.estructura_comision.Last().valor)) / 100;

            }else
            {
                monto_por_comision = Convert.ToDecimal(i_usuario.estructura_comision.Last().valor);
            }

            
              if(monto_minimo_concesionario > monto_por_comision)
               {
                   Monto = monto_minimo_concesionario;
               }else
               {
                   Monto = monto_por_comision;
               }

            return Monto;
        }

        public int ObtenerPreferencia_Cliente_Vehiculo(vehiculo v1, string id_cliente)
        {
            int Nivel_Preferencia = 0;
            int Nivel_Preferencia_Por_Marca = CalcularPreferenciaCliente_Marca(id_cliente, v1.modelo.fk_marca);
            int Nivel_Preferencia_Por_Categoria = CalcularPreferenciaCliente_Categoria(id_cliente, v1.modelo.fk_clasificacion);
            int Nivel_Preferencia_Por_Modelo = CalcularPreferenciaCliente_Modelo(id_cliente, v1.modelo.id_modelo);
            double n_pref = (Convert.ToDouble(Nivel_Preferencia_Por_Marca) * 0.25) + (Convert.ToDouble(Nivel_Preferencia_Por_Categoria) * 0.25) + (Convert.ToDouble(Nivel_Preferencia_Por_Modelo) * 0.5);

            Nivel_Preferencia = Convert.ToInt32(n_pref);

            return Nivel_Preferencia;

        }

        public decimal ObtenerPrecio_Venta_Maximo(vehiculo v1)
        {
            decimal Monto = 0;
            var porcentaje_ganancia_concesionario = v1.concesionario.porcentaje_ganancia;

            var valor_concesionario = ((v1.valor_compra * Convert.ToDecimal(porcentaje_ganancia_concesionario)) / 100) + v1.valor_compra;
            decimal valor_financia_banco = 0;
            if (ConsultarPreferenciaBanco(v1.modelo) == true)
            {
                valor_financia_banco = v1.modelo.banco_financia_modelo.Where(x => x.valor_limite == v1.modelo.banco_financia_modelo.Max(y => y.valor_limite)).First().valor_limite;
            }

            if (valor_financia_banco > valor_concesionario)
            {
                Monto = valor_financia_banco;
            }
            else
            {
                Monto = valor_concesionario;
            }


            return Monto;
        }

        public decimal ObtenerPrecio_Venta_Minimo(vehiculo v1)
        {
            decimal Monto = 0;
            var porcentaje_ganancia_concesionario = v1.concesionario.porcentaje_ganancia;

            var valor_concesionario = ((v1.valor_compra * Convert.ToDecimal(porcentaje_ganancia_concesionario)) / 100) + v1.valor_compra;
            decimal valor_financia_banco = 0;
            if (ConsultarPreferenciaBanco(v1.modelo) == true)
            {
                valor_financia_banco = v1.modelo.banco_financia_modelo.Where(x => x.valor_limite == v1.modelo.banco_financia_modelo.Max(y => y.valor_limite)).First().valor_limite;
            }

            if ((valor_financia_banco < valor_concesionario) && (valor_financia_banco != 0))
            {
                Monto = valor_financia_banco;
            }
            else
            {
                Monto = valor_concesionario;
            }


            return Monto;
        }

        public SelectList ObtenerAgentesFinanciamiento (int id_concesionario)
        {
            var consulta_agentes = from l_agente in autodb.usuario where l_agente.fk_concesionario == id_concesionario && l_agente.fk_tipo_usuario == 5 && l_agente.fk_usuario_estado == 1 select l_agente;
            List<SelectListItem> lista_a = new List<SelectListItem>();
            string primer_agente = "0";
            if(consulta_agentes.Count() > 0)
            {
                primer_agente = consulta_agentes.First().id_usuario.ToString();
                foreach(var item in consulta_agentes)
                {
                    string nombre_completo = item.nombre + " " + item.apellido;
                    lista_a.Add(new SelectListItem() { Value = item.id_usuario.ToString(), Text = nombre_completo });
                }

            }





            SelectList lista_agentes = new SelectList(lista_a, "Value", "Text", primer_agente);

            return lista_agentes;
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