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
        public ActionResult Preventa(string id_cliente)
        {
            PreventaViewModels modelo = new PreventaViewModels();
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            List<TransaccionesVentaViewModels> lista_trans = new List<TransaccionesVentaViewModels>();
            O_Transaccion_VentaViewModels trans_en_curso = new O_Transaccion_VentaViewModels();

            usuario.id_T_Preventa = 0;
            usuario.id_T_Venta = 0;
            this.Session["User"] = usuario;
            var tran_activa = (from l_tran_a in autodb.transaccion_venta where l_tran_a.fk_estado_transaccion == 4 && l_tran_a.fk_concesionario == usuario.id_Concesionario && l_tran_a.fk_tipo_venta == 2 select l_tran_a);
            if(tran_activa.Any())
            {
                var t_i = tran_activa.First();
                usuario.id_T_Venta = t_i.id_venta;
                trans_en_curso.Transaccion = t_i;
                trans_en_curso.Monto_Total = t_i.vehiculo.Sum(x => x.valor_venta.Value);
                trans_en_curso.Cant_Vehiculo = t_i.vehiculo.Count();
                trans_en_curso.Lista_Vehiculo = t_i.vehiculo.ToList();
            }
            this.Session["User"] = usuario;

            var i_cliente = (from l_clientes in autodb.cliente where l_clientes.id_cliente == id_cliente select new { l_clientes.nombre, l_clientes.apellido, l_clientes.telefono, l_clientes.correo_electronico, direcion = l_clientes.direccion.descripcion + ", " + l_clientes.direccion.ciudad.nombre, l_clientes.id_cliente }).First();
            ClienteViewModels c1 = new ClienteViewModels
            {
                Direccion = i_cliente.direcion,
                Email = i_cliente.correo_electronico,
                Nombre_Completo = i_cliente.nombre + " " + i_cliente.apellido,
                Nro_Telefono = i_cliente.telefono,
                id = i_cliente.id_cliente
            };

            var trans = (from l_trans in autodb.transaccion_venta where l_trans.fk_cliente == id_cliente && l_trans.fk_concesionario == usuario.id_Concesionario && l_trans.fk_estado_transaccion == 1 select new { l_trans.id_venta, l_trans.fecha, l_trans.fk_tipo_venta, l_trans.vehiculo, l_trans.usuario }).OrderByDescending(x => x.fecha).Take(10);

            foreach (var item in trans)
            {
                TransaccionesVentaViewModels t1 = new TransaccionesVentaViewModels();

                t1.id = item.id_venta;
                t1.fecha = item.fecha;
                t1.Tipo_Transaccion = item.fk_tipo_venta;
                t1.Lista_Vehiculo = item.vehiculo.ToList();
                var u = item.usuario;
                t1.Nombre_Operador = u.nombre + " " + u.apellido;
                t1.Cant_Vehiculos = item.vehiculo.Count();
                t1.Cant_Tiempo_Transcurrido = CalcularTiempoFecha(item.fecha);
                if(item.fk_tipo_venta == 1)
                {
                    t1.Mayor_Monto = item.vehiculo.Max(x => x.valor_compra);
                    t1.Menor_Monto = item.vehiculo.Min(x => x.valor_compra );
                    t1.Monto_Total = 0;
                }else{
                    t1.Mayor_Monto = item.vehiculo.Max(x => x.valor_venta.Value);
                    t1.Menor_Monto = item.vehiculo.Min(x => x.valor_venta.Value );
                    t1.Monto_Total = item.vehiculo.Sum(x => x.valor_venta.Value);
                }
                lista_trans.Add(t1);
            }

            modelo.Transaccion = trans_en_curso;
            modelo.Lista_Transacciones = lista_trans;
            modelo.Cliente = c1;
            return View(modelo);
        }

        public ActionResult ConsultarCliente()
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            ConsultarClienteViewModels modelo = new ConsultarClienteViewModels();
            List<O_Transaccion_VentaViewModels> l_transaccion = new List<O_Transaccion_VentaViewModels>();
            var clientes_t_concesionario  = (from l_usuario in autodb.cliente where l_usuario.transaccion_venta.Any(x => x.fk_concesionario == usuario.id_Concesionario) select l_usuario.id_cliente).Count();
            modelo.Nro_Cliente = clientes_t_concesionario;
            var transaccion_usuario = from l_trans in autodb.transaccion_venta where l_trans.fk_tipo_venta == 2 && l_trans.fk_estado_transaccion == 4 && l_trans.fk_usuario == usuario.id_Usuario select l_trans;
            if(transaccion_usuario.Any())
            {
                foreach(var item in transaccion_usuario)
                {
                    O_Transaccion_VentaViewModels item_tran = new O_Transaccion_VentaViewModels
                    {
                        Transaccion = item,
                        Cant_Vehiculo = item.vehiculo.Count(),
                        Lista_Vehiculo = item.vehiculo.ToList(),
                        Monto_Total = item.vehiculo.Sum(x => x.valor_venta.Value)
                    };
                    l_transaccion.Add(item_tran);
                }
                modelo.Transaccion = l_transaccion;
            }
          
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
            if (tipo == 1)
            {
                consulta_transacciones = from l_tran in autodb.transaccion_venta where l_tran.fk_cliente == id_cliente select l_tran;


            }
            else
            {
                //Preferencia del Mercado
                var c_usuario = (from l_concesionario in autodb.concesionario where l_concesionario.id_concesionario == usuario.id_Concesionario select new { id_estado = l_concesionario.direccion.ciudad.fk_estado }).First();
                consulta_transacciones = from l_tran in autodb.transaccion_venta where l_tran.concesionario.direccion.ciudad.estado.id_estado == c_usuario.id_estado select l_tran;
            }

            /////////MARCAS ////////
            //Obtener Lista de las Marcas en Inventario
            var marcas_inventario = inventario.GroupBy(x => x.modelo.fk_marca);
            //Obtener Lista de las Marcar preferidas por el cliente en general
            var marcas_preferidas_cliente = consulta_transacciones.SelectMany(x => x.vehiculo).GroupBy(y => y.modelo.fk_marca);

            if (marcas_preferidas_cliente.Count() > 0)
            {
                //Obtener las Marcas que entasn en inventario y que el cliente prefiere
                var marcas_inventario_preferidas_cliente = marcas_preferidas_cliente.Where(x => marcas_inventario.Select(y => y.Key).Contains(x.Key));

                /////////////Estadisticas de Preventa////////////////// Se le asigna el 25% de la importancia en el calculo
                var cant_veh_preventa_total = marcas_inventario_preferidas_cliente.Sum(x => x.Distinct().Sum(y => y.transaccion_venta.Count(z => z.fk_tipo_venta == 1)));
                if (cant_veh_preventa_total == 0)
                {
                    cant_veh_preventa_total = 1;
                }
                var cant_veh_venta_total = marcas_inventario_preferidas_cliente.Sum(x => x.Distinct().Sum(y => y.transaccion_venta.Count(z => z.fk_tipo_venta == 2)));
                if (cant_veh_venta_total == 0)
                {
                    cant_veh_venta_total = 1;
                }

                foreach (var item_marca in marcas_inventario_preferidas_cliente)
                {

                    var cant_veh_preventa_marca = item_marca.Distinct().Sum(x => x.transaccion_venta.Count(y => y.fk_tipo_venta == 1));
                    var cant_veh_venta_marca = item_marca.Distinct().Sum(x => x.transaccion_venta.Count(y => y.fk_tipo_venta == 2));

                    Venta_MarcasViewModels i_marca = new Venta_MarcasViewModels
                    {
                        Marca = item_marca.First().modelo.marca,
                        Nro_Inventario = inventario.Count(x => x.modelo.fk_marca == item_marca.Key),
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
                var categoria_inventario_preferidas_cliente = categoria_preferidas_cliente.Where(x => categoria_inventario.Select(y => y.Key).Contains(x.Key));

                /////////////Estadisticas de Preventa////////////////// Se le asigna el 25% de la importancia en el calculo
                var cant_veh_preventa_total = categoria_inventario_preferidas_cliente.Sum(x => x.Distinct().Sum(y => y.transaccion_venta.Count(z => z.fk_tipo_venta == 1)));
                if (cant_veh_preventa_total == 0)
                {
                    cant_veh_preventa_total = 1;
                }
                var cant_veh_venta_total = categoria_inventario_preferidas_cliente.Sum(x => x.Distinct().Sum(y => y.transaccion_venta.Count(z => z.fk_tipo_venta == 2)));
                if (cant_veh_venta_total == 0)
                {
                    cant_veh_venta_total = 1;
                }



                foreach (var item_cat in categoria_inventario_preferidas_cliente)
                {
                    //Calcular cantidad de vehiculos en preventas perteneciente a la marca
                    var cant_veh_preventa_categoria = item_cat.Distinct().Sum(x => x.transaccion_venta.Count(y => y.fk_tipo_venta == 1));
                    var cant_veh_venta_categoria = item_cat.Distinct().Sum(x => x.transaccion_venta.Count(y => y.fk_tipo_venta == 2));


                    Venta_CategoriaViewModels i_cat = new Venta_CategoriaViewModels
                    {
                        Categoria = item_cat.First().modelo.modelo_clasificacion,
                        Nro_Inventario = inventario.Count(x => x.modelo.fk_clasificacion == item_cat.Key),
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
                var c_usuario = (from l_concesionario in autodb.concesionario where l_concesionario.id_concesionario == usuario.id_Concesionario select new { id_estado = l_concesionario.direccion.ciudad.fk_estado }).First();
                consulta_transacciones = from l_tran in autodb.transaccion_venta where l_tran.concesionario.direccion.ciudad.estado.id_estado == c_usuario.id_estado select l_tran;
            }

            /////////MARCAS ////////
            //Obtener Lista de las Marcas en Inventario
            var marcas_inventario = inventario.GroupBy(x => x.modelo.fk_marca);
            //Obtener Lista de las Marcar preferidas por el cliente en general
            var marcas_preferidas_cliente = consulta_transacciones.SelectMany(x => x.vehiculo).GroupBy(y => y.modelo.fk_marca);

            if (marcas_preferidas_cliente.Count() > 0)
            {
                //Obtener las Marcas que entasn en inventario y que el cliente prefiere
                var marcas_inventario_preferidas_cliente = marcas_preferidas_cliente.Where(x => marcas_inventario.Select(y => y.Key).Contains(x.Key));

                /////////////Estadisticas de Preventa////////////////// Se le asigna el 25% de la importancia en el calculo
                var cant_veh_preventa_total = marcas_inventario_preferidas_cliente.Sum(x => x.Distinct().Sum(y => y.transaccion_venta.Count(z => z.fk_tipo_venta == 1)));
                if (cant_veh_preventa_total == 0)
                {
                    cant_veh_preventa_total = 1;
                }
                var cant_veh_venta_total = marcas_inventario_preferidas_cliente.Sum(x => x.Distinct().Sum(y => y.transaccion_venta.Count(z => z.fk_tipo_venta == 2)));
                if (cant_veh_venta_total == 0)
                {
                    cant_veh_venta_total = 1;
                }

                foreach (var item_marca in marcas_inventario_preferidas_cliente)
                {

                    var cant_veh_preventa_marca = item_marca.Distinct().Sum(x => x.transaccion_venta.Count(y => y.fk_tipo_venta == 1));
                    var cant_veh_venta_marca = item_marca.Distinct().Sum(x => x.transaccion_venta.Count(y => y.fk_tipo_venta == 2));

                    Venta_MarcasViewModels i_marca = new Venta_MarcasViewModels
                    {
                        Marca = item_marca.First().modelo.marca,
                        Nro_Inventario = inventario.Count(x => x.modelo.fk_marca == item_marca.Key),
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
                var categoria_inventario_preferidas_cliente = categoria_preferidas_cliente.Where(x => categoria_inventario.Select(y => y.Key).Contains(x.Key));

                /////////////Estadisticas de Preventa////////////////// Se le asigna el 25% de la importancia en el calculo
                var cant_veh_preventa_total = categoria_inventario_preferidas_cliente.Sum(x => x.Distinct().Sum(y => y.transaccion_venta.Count(z => z.fk_tipo_venta == 1)));
                if (cant_veh_preventa_total == 0)
                {
                    cant_veh_preventa_total = 1;
                }
                var cant_veh_venta_total = categoria_inventario_preferidas_cliente.Sum(x => x.Distinct().Sum(y => y.transaccion_venta.Count(z => z.fk_tipo_venta == 2)));
                if (cant_veh_venta_total == 0)
                {
                    cant_veh_venta_total = 1;
                }



                foreach (var item_cat in categoria_inventario_preferidas_cliente)
                {
                    //Calcular cantidad de vehiculos en preventas perteneciente a la marca
                    var cant_veh_preventa_categoria = item_cat.Distinct().Sum(x => x.transaccion_venta.Count(y => y.fk_tipo_venta == 1));
                    var cant_veh_venta_categoria = item_cat.Distinct().Sum(x => x.transaccion_venta.Count(y => y.fk_tipo_venta == 2));


                    Venta_CategoriaViewModels i_cat = new Venta_CategoriaViewModels
                    {
                        Categoria = item_cat.First().modelo.modelo_clasificacion,
                        Nro_Inventario = inventario.Count(x => x.modelo.fk_clasificacion == item_cat.Key),
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
                trans_cliente = (from l_transaccion in autodb.transaccion_venta where l_transaccion.id_venta == usuario.id_T_Preventa select l_transaccion).First();

            }
            else
            {
                transaccion_venta t1 = new transaccion_venta
                {
                    fk_cliente = id_cliente,
                    fk_tipo_venta = 1,
                    fk_concesionario = usuario.id_Concesionario,
                    fk_estado_transaccion = 1,
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
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, tipo= "1" }, JsonRequestBehavior.AllowGet);
            }

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
            //Agrega Comisiones
            var usuarios_c = from l_usuario in autodb.usuario where l_usuario.id_usuario == usuario.id_Usuario || l_usuario.id_usuario == modelo.id_Financista select l_usuario;
          
            foreach (var item_user in usuarios_c)
            {
                usuario_gana_comision u_g_1 = new usuario_gana_comision();
                if (item_user.usuario_gana_comision.Any(x => x.fk_transaccion_venta == trans_cliente.id_venta))
                {
                    u_g_1 = (item_user.usuario_gana_comision.Where(x => x.fk_transaccion_venta == trans_cliente.id_venta)).First();
                    u_g_1.monto = u_g_1.monto + ObtenerComision(item_user, Convert.ToDecimal(modelo.Valor_Venta));
                }
                else
                {
                    u_g_1.fk_transaccion_venta = trans_cliente.id_venta;
                    u_g_1.fk_usuario = item_user.id_usuario;
                    u_g_1.monto = ObtenerComision(item_user, Convert.ToDecimal(modelo.Valor_Venta));
                    trans_cliente.usuario_gana_comision.Add(u_g_1);
                }
            }
            //Agrega el auto a la transccion
            auto.fecha_salida = DateTime.Now;
            auto.valor_venta = Convert.ToDecimal(modelo.Valor_Venta);
            trans_cliente.vehiculo.Add(auto);
            //auto.transaccion_venta.Add(trans_cliente);
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
                return Json(new { success = true, tipo = "0", titulo = m1.Titulo, cuerpo = m1.Cuerpo, transaccion_id = trans_cliente.id_venta }, JsonRequestBehavior.AllowGet);
            }catch(Exception e)
            {

                return Json(new { success = false, tipo = "2" }, JsonRequestBehavior.AllowGet);
            }
            
            

        }

        [HttpPost]
        [Authorize]
        public ActionResult AgregarVehiculo_Preventa(VehiculosDetallesViewModels modelo)
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            transaccion_venta trans_cliente = new transaccion_venta();

            var auto = (from l_vehiculo in autodb.vehiculo where l_vehiculo.id_vehiculo == modelo.Vehiculo.id_vehiculo select l_vehiculo).First();


            trans_cliente = ObtenerTransaccion_Preventa_EnCurso(modelo.ID_Cliente);
            
            if(trans_cliente.vehiculo.Any(x => x.id_vehiculo == auto.id_vehiculo))
            {
                return Json(new { success = true, tipo = "0" }, JsonRequestBehavior.AllowGet);
            }else
            {
                trans_cliente.vehiculo.Add(auto);
                //auto.transaccion_venta.Add(trans_cliente);
                try
                {
                    autodb.SaveChanges();


                    return Json(new { success = true, tipo = "0" }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception e)
                {

                    return Json(new { success = false, tipo = "2" }, JsonRequestBehavior.AllowGet);
                }

            }
            

        }


        public ActionResult AnularVenta(int id_venta)
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            var trans = from l_trans in autodb.transaccion_venta where l_trans.id_venta == id_venta select l_trans;
            if (trans.Any())
            {
                var item_transaccion = trans.First();
                foreach(var item in item_transaccion.vehiculo)
                {
                    item.valor_venta = null;
                    item.fecha_salida = null;
                }
                item_transaccion.vehiculo.Clear();
                item_transaccion.usuario_gana_comision.Clear();
                
                autodb.transaccion_venta.Remove(item_transaccion);
                try
                {
                    autodb.SaveChanges();
                    usuario.id_T_Venta = 0;
                    this.Session["User"] = usuario;
                    var mens = "Se a completado la anulación de la transacción.";
                 
                    MensajeViewModels m1 = new MensajeViewModels
                    {
                        Titulo = "Transacción de Venta Nro. " + item_transaccion.id_venta + " Anulada",
                        Cuerpo = mens,
                        Tipo_Modal = "modal-success"
                    };

                    this.Session["Mensaje"] = m1;
                    return Json(new { success = true, tipo = "0" }, JsonRequestBehavior.AllowGet);

                }catch(Exception e)
                {
                    MensajeViewModels m1 = new MensajeViewModels
                    {
                        Titulo = "Anulación de transacción de Venta Nro. " + item_transaccion.id_venta + " fallo",
                        Cuerpo = "Intente mas tarde.",
                        Tipo_Modal = "modal-danger"
                    };

                    this.Session["Mensaje"] = m1;
                    return Json(new { success = false, tipo = "2" }, JsonRequestBehavior.AllowGet);

                }



            }

            MensajeViewModels m2 = new MensajeViewModels
            {
                Titulo = "Transacción de Venta Nro. " + id_venta + " No existe",
                Cuerpo = "Intente nuevamente.",
                Tipo_Modal = "modal-danger"
            };

            this.Session["Mensaje"] = m2;
            return Json(new { success = false, tipo = "1" }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ConcretarVenta(int id_venta)
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            var trans = from l_trans in autodb.transaccion_venta where l_trans.id_venta == id_venta select l_trans;
            if(trans.Any())
            {
                var item_transaccion = trans.First();
                item_transaccion.fk_estado_transaccion = 1;
                if(item_transaccion.vehiculo.Any(x => x.valor_venta < ObtenerPrecio_Venta_Maximo(x)))
                {
                    var usu_gerente = from l_usu in autodb.usuario where l_usu.fk_tipo_usuario == 2 && l_usu.fk_concesionario == usuario.id_Concesionario select l_usu;
                    mensaje m1 = new mensaje
                    {
                        fecha = DateTime.Now,
                        texto = "Alerta. En la transaccion de venta Nro. " + item_transaccion.id_venta + " realizada por el usuario" + usuario.Nombre + " " + usuario.Apellido + ", uno o varios de los vehiculos fueron vendido por debajo del valor minimo estimado",
                        titulo = "Valor minimo de venta"
                    };

                    var op_add_mensaje = autodb.mensaje.Add(m1);

                    foreach (var usuario_item in usu_gerente)
                    {
                        usuario_tiene_mensaje usu_mensaje = new usuario_tiene_mensaje
                        {
                            fk_mensaje = op_add_mensaje.id_mensaje,
                            fk_usuario = usuario_item.id_usuario,
                            chequeado = 0
                        };
                        autodb.usuario_tiene_mensaje.Add(usu_mensaje);
                    }

                }

                try
                {
                    autodb.SaveChanges();
                    var mens = "Se a completado con exito la transción.";
                    mens += " En la misma se registro la venta de " + item_transaccion.vehiculo.Count() + " vehiculo(s) al cliente " + item_transaccion.cliente.nombre + " " + item_transaccion.cliente.apellido + ", con un monto total de " + item_transaccion.vehiculo.Sum(x => x.valor_venta.Value) + " USD";
                    mens += " y logrando una comisión de " + item_transaccion.usuario_gana_comision.Where(x => x.fk_usuario == usuario.id_Usuario).First().monto + " USD ";
                    usuario.id_T_Venta = 0;
                    this.Session["User"] = usuario;

                    MensajeViewModels m1 = new MensajeViewModels
                    {
                        Titulo = "Transacción de Venta Nro. "+item_transaccion.id_venta + " Concretada",
                        Cuerpo = mens,
                        Tipo_Modal = "modal-success"
                    };

                    this.Session["Mensaje"] = m1;
                    return Json(new { success = true, tipo = "0" }, JsonRequestBehavior.AllowGet);


                }catch(Exception e){
                    MensajeViewModels m1 = new MensajeViewModels
                    {
                        Titulo = "Transacción de Venta Nro. " + item_transaccion.id_venta + " Fallo",
                        Cuerpo = "Intente mas tarde.",
                        Tipo_Modal = "modal-danger"
                    };

                    this.Session["Mensaje"] = m1;
                    return Json(new { success = false, tipo = "1" }, JsonRequestBehavior.AllowGet);

                }
            }

            MensajeViewModels m2 = new MensajeViewModels
            {
                Titulo = "Transacción de Venta Nro. " + id_venta + " No existe",
                Cuerpo = "Intente nuevamente.",
                Tipo_Modal = "modal-danger"
            };

            this.Session["Mensaje"] = m2;
            return Json(new { success = false, tipo = "2" }, JsonRequestBehavior.AllowGet);
           
        }


        public decimal ObtenerComision(usuario usuario, decimal monto)
        {
            decimal Monto = 0;
            var i_usuario = usuario;
            var concesionario = (from l_concesionario in autodb.concesionario where l_concesionario.id_concesionario == i_usuario.fk_concesionario select new { l_concesionario.comision_minima }).First();
            decimal monto_minimo_concesionario = 0;
            if (concesionario.comision_minima != null)
            {
                monto_minimo_concesionario = Convert.ToDecimal(concesionario.comision_minima);
            }
            var tipo_comision = i_usuario.estructura_comision.Last().fk_tipo_comision;
            decimal monto_por_comision = 0;

            if (tipo_comision == 1)
            {
                monto_por_comision = (monto * Convert.ToDecimal(i_usuario.estructura_comision.Last().valor)) / 100;

            }
            else
            {
                monto_por_comision = Convert.ToDecimal(i_usuario.estructura_comision.Last().valor);
            }


            if (monto_minimo_concesionario > monto_por_comision)
            {
                Monto = monto_minimo_concesionario;
            }
            else
            {
                Monto = monto_por_comision;
            }

            return Monto;
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
            var i_usuario = (from l_usuario in autodb.usuario where l_usuario.id_usuario == id_usuario select new { l_usuario.estructura_comision, l_usuario.fk_concesionario }).First();
            var concesionario = (from l_concesionario in autodb.concesionario where l_concesionario.id_concesionario == i_usuario.fk_concesionario select new { l_concesionario.comision_minima }).First();
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

        public ActionResult Menu_Prioritarios(string id_cliente)
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            Venta_VehiculoViewModels modelo = new Venta_VehiculoViewModels();
            List<Venta_ModeloViewModels> lista_vehiculos = new List<Venta_ModeloViewModels>();
            var concesionario = (from l_c in autodb.concesionario where l_c.id_concesionario == usuario.id_Concesionario select new { l_c.dia_max_inventario}).First();
            var consultar_vehiculos = from l_vehiculos in autodb.vehiculo where l_vehiculos.fk_concesionario == usuario.id_Concesionario && l_vehiculos.fecha_salida == null && DbFunctions.DiffDays(l_vehiculos.fecha_ingreso, DateTime.Now) > concesionario.dia_max_inventario select l_vehiculos;


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
            modelo.ID_Cliente = id_cliente;
            return PartialView("_Menu_Prioritarios", modelo);
        }

        public ActionResult Menu_Inventario(string id_cliente)
        {
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            Venta_VehiculoViewModels modelo = new Venta_VehiculoViewModels();
            List<Venta_ModeloViewModels> lista_vehiculos = new List<Venta_ModeloViewModels>();
            var consultar_vehiculos = from l_vehiculos in autodb.vehiculo where l_vehiculos.fk_concesionario == usuario.id_Concesionario && l_vehiculos.fecha_salida == null  select l_vehiculos;


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
            modelo.ID_Cliente = id_cliente;
            return PartialView("_Menu_Inventario", modelo);
        }

        public ActionResult Menu_Botones(int tipo, string id_cliente)
        {
            MenuBotonViewModels modelo = new MenuBotonViewModels();

            modelo.tipo = tipo;
            modelo.cliente = id_cliente;
            return PartialView("_Menu_Botones", modelo);
        }
        public ActionResult Prueba4(int tipo, string id_cliente)
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
            return View(modelo);
        }

        public ActionResult HistorialComision()
        {
            HistorialComisionViewModels modelo = new HistorialComisionViewModels();
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            List<TrasaccionComisionViewModels> lista_t = new List<TrasaccionComisionViewModels>();
            List<ComisionGraficoViewModels> lista_t_grafico = new List<ComisionGraficoViewModels>();
            var lista_transaccion = (from l_trans in autodb.transaccion_venta where l_trans.fk_usuario == usuario.id_Usuario && l_trans.fk_tipo_venta == 2 select new { l_trans.id_venta, l_trans.vehiculo, l_trans.fecha }).OrderByDescending(x => x.fecha);
            var comision_usuario = (from l_usuarios in autodb.usuario where l_usuarios.id_usuario == usuario.id_Usuario select new { l_usuarios.estructura_comision, l_usuarios.id_usuario}).First();
            if(lista_transaccion.Any())
            {
                foreach(var trans in lista_transaccion)
                {
                    List<VehiculoComisionViewModels> l_vehiculo = new List<VehiculoComisionViewModels>();

                    foreach(var vehi in trans.vehiculo)
                    {
                        VehiculoComisionViewModels item_vehi = new VehiculoComisionViewModels
                        {
                            Año = vehi.modelo.año,
                            Comision = CalcularComision(vehi.valor_venta.Value, usuario.id_Usuario),
                            Imagen = ObtenerImagenVehiculo(vehi),
                            Marca = vehi.modelo.marca.nombre,
                            Nombre = vehi.modelo.modelo1 + " " + vehi.modelo.nombre,
                            id = vehi.id_vehiculo

                        };
                        l_vehiculo.Add(item_vehi);
                    }

                    TrasaccionComisionViewModels item_trans = new TrasaccionComisionViewModels
                    {
                        Fecha = trans.fecha,
                        id = trans.id_venta,
                        Monto_Total = CalcularComision(trans.vehiculo.Sum(c => c.valor_venta.Value), usuario.id_Usuario),
                        Lista_Vehiculos = l_vehiculo
                    };
                    lista_t.Add(item_trans);
                }
                var agrupar_trans = lista_t.GroupBy(x => new { x.Fecha.Year, x.Fecha.Month });

                foreach(var a_item in agrupar_trans)
                {
                    ComisionGraficoViewModels item_graf = new ComisionGraficoViewModels
                    {
                        Monto_Acumulado = Convert.ToInt32(a_item.Sum(x => x.Monto_Total)),
                        Fecha_F = a_item.Key.Year + "-" + FixedMes(a_item.Key.Month)
                    };
                    lista_t_grafico.Add(item_graf);

                }
            }



            //Armando modelo
            modelo.Lista_Transaccion = lista_t;
            modelo.Valor_Comision_Fijo = Convert.ToInt32(comision_usuario.estructura_comision.Last().valor);
            modelo.Tipo_Comision = comision_usuario.estructura_comision.Last().fk_tipo_comision;
            modelo.Monto_Total = lista_t.Sum(x => x.Monto_Total);
            modelo.Total_Ventas = lista_t.Sum(x => x.Lista_Vehiculos.Count());
            modelo.Fecha_Inicial = lista_t.Last().Fecha;
            modelo.Fecha_Final = DateTime.Now;
            modelo.Comision_Total = modelo.Monto_Total;
            if(lista_t.Where(x => DateTime.Now.Subtract(x.Fecha).TotalDays >= 365).Count() > 0)
            {
                modelo.Comision_1_Año = lista_t.Where(x => DateTime.Now.Subtract(x.Fecha).TotalDays <= 365).Sum(x => x.Monto_Total);
            }else
            {
                modelo.Comision_1_Año = 0;
            }

            if (lista_t.Where(x => DateTime.Now.Subtract(x.Fecha).TotalDays >= 90).Count() > 0)
            {
                modelo.Comision_3_Mes = lista_t.Where(x => DateTime.Now.Subtract(x.Fecha).TotalDays <= 90).Sum(x => x.Monto_Total);
            }
            else
            {
                modelo.Comision_3_Mes = 0;
            }

            if (lista_t.Where(x => DateTime.Now.Subtract(x.Fecha).TotalDays >= 30).Count() > 0)
            {
                modelo.Comision_30_dias = lista_t.Where(x => DateTime.Now.Subtract(x.Fecha).TotalDays <= 30).Sum(x => x.Monto_Total);
            }
            else
            {
                modelo.Comision_30_dias = 0;
            }

            modelo.Lista_Comision_Grafico = lista_t_grafico;

            return View(modelo);
        }

        public ActionResult HistorialComisionConsulta(string fecha_inicio, string fecha_fin)
        {
            DateTime f_inicio = Convert.ToDateTime(fecha_inicio);
            DateTime f_fin = Convert.ToDateTime(fecha_fin);

            HistorialComisionViewModels modelo = new HistorialComisionViewModels();
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            List<TrasaccionComisionViewModels> lista_t = new List<TrasaccionComisionViewModels>();

            var lista_transaccion = (from l_trans in autodb.transaccion_venta where l_trans.fk_usuario == usuario.id_Usuario && l_trans.fk_tipo_venta == 2 && (l_trans.fecha >= f_inicio && l_trans.fecha <= f_fin) select new { l_trans.id_venta, l_trans.vehiculo, l_trans.fecha }).OrderByDescending(x => x.fecha);
            var comision_usuario = (from l_usuarios in autodb.usuario where l_usuarios.id_usuario == usuario.id_Usuario select new { l_usuarios.estructura_comision, l_usuarios.id_usuario }).First();
            if (lista_transaccion.Any())
            {
                foreach (var trans in lista_transaccion)
                {
                    List<VehiculoComisionViewModels> l_vehiculo = new List<VehiculoComisionViewModels>();

                    foreach (var vehi in trans.vehiculo)
                    {
                        VehiculoComisionViewModels item_vehi = new VehiculoComisionViewModels
                        {
                            Año = vehi.modelo.año,
                            Comision = CalcularComision(vehi.valor_venta.Value, usuario.id_Usuario),
                            Imagen = ObtenerImagenVehiculo(vehi),
                            Marca = vehi.modelo.marca.nombre,
                            Nombre = vehi.modelo.modelo1 + " " + vehi.modelo.nombre,
                            id = vehi.id_vehiculo

                        };
                        l_vehiculo.Add(item_vehi);
                    }

                    TrasaccionComisionViewModels item_trans = new TrasaccionComisionViewModels
                    {
                        Fecha = trans.fecha,
                        id = trans.id_venta,
                        Monto_Total = CalcularComision(trans.vehiculo.Sum(c => c.valor_venta.Value), usuario.id_Usuario),
                        Lista_Vehiculos = l_vehiculo
                    };
                    lista_t.Add(item_trans);
                }
                
            }



            //Armando modelo
            modelo.Lista_Transaccion = lista_t;
            modelo.Monto_Total = lista_t.Sum(x => x.Monto_Total);
            modelo.Total_Ventas = lista_t.Sum(x => x.Lista_Vehiculos.Count());
            modelo.Fecha_Inicial = f_inicio;
            modelo.Fecha_Final = f_fin;


            return PartialView("_HistorialComisionConsulta", modelo);
        }



        public string FixedMes(int mes)
        {
            string mes_correcto = "";
            if(mes < 10)
            {
                mes_correcto = "0" + mes;
            }else
            {
                mes_correcto += mes;
            }


            return mes_correcto;

        }

        [AllowAnonymous]
        public JsonResult ObtenerModelos(string q)
        {
            int id_concesionario_session = Convert.ToInt32(this.Session["Concesionario"]);
            
            var lista_modelos = (from l_model in autodb.vehiculo where (l_model.modelo.modelo1 + " " + l_model.modelo.nombre + " " + l_model.modelo.año).Contains(q) && l_model.fecha_salida == null && l_model.fk_concesionario == id_concesionario_session select l_model).OrderBy(x => x.modelo.modelo1).ToList();
            List<ModelosDetallesViewModels> l_modelos = new List<ModelosDetallesViewModels>();
            var inventario = (from inv in autodb.vehiculo where inv.fk_concesionario == id_concesionario_session select inv).ToList();

            foreach (var item in lista_modelos)
            {

                    ModelosDetallesViewModels model = new ModelosDetallesViewModels
                    {
                        id = item.id_vehiculo,
                        Image = ObtenerImagenVehiculo(item),
                        Descripcion = "" + item.modelo.descripcion,
                        Nombre = item.modelo.modelo1 + " " + item.modelo.nombre + " " + item.modelo.año,
                        Nro_Inventario = Convert.ToInt32(item.valor_compra),
                        Nro_Rezagados = item.kilometraje,
                        Nro_Vendidos = Convert.ToInt32(DateTime.Now.Subtract(item.fecha_ingreso).TotalDays)
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

        [AllowAnonymous]
        public JsonResult BuscarCliente(string q)
        {
            int id_concesionario_session = Convert.ToInt32(this.Session["Concesionario"]);

            var lista_cliente = (from l_cliente in autodb.cliente where (l_cliente.nombre + " " + l_cliente.apellido).Contains(q) || l_cliente.id_cliente.Contains(q) select new { l_cliente.id_cliente, l_cliente.nombre, l_cliente.apellido, l_cliente.transaccion_venta }).OrderBy(x => x.nombre);
            List<Cliente_ItemBoxViewModels> l_clientes = new List<Cliente_ItemBoxViewModels>();
           
            foreach (var item in lista_cliente)
            {

                Cliente_ItemBoxViewModels model = new Cliente_ItemBoxViewModels
                {
                    id = item.id_cliente,
                    Image = "/Images/imagen_user_160.png",
                    Descripcion = "ID: " + item.id_cliente,
                    Nombre = item.nombre + " " + item.apellido,
                    Nro_Preventas = item.transaccion_venta.Count(x => x.fk_tipo_venta == 1 && x.fk_concesionario == id_concesionario_session),
                    Nro_Ventas = item.transaccion_venta.Count(x => x.fk_tipo_venta == 2 && x.fk_concesionario == id_concesionario_session),
                };

                l_clientes.Add(model);

            }
            l_clientes.OrderBy(x => x.Nombre);
            BoxClienteViewModels t_modelos = new BoxClienteViewModels
            {
                Lista_Cliente = l_clientes,
                Total_Clientes = l_clientes.Count()

            };

            var json = t_modelos;
            return Json(json, JsonRequestBehavior.AllowGet);
        }

        public string CalcularTiempoFecha(DateTime fecha)
        {
            var dias = DateTime.Now.Subtract(fecha).TotalDays;
            var dias_for = DateTime.Now.Subtract(fecha).Days;
            string dias_entre_fecha = "";
            if (dias < 1)
            {
                var horas = DateTime.Now.Subtract(fecha).TotalHours;
                var horas_for = DateTime.Now.Subtract(fecha).Hours;
                if (horas < 1)
                {
                    var minutos = DateTime.Now.Subtract(fecha).TotalMinutes;
                    var minutos_for = DateTime.Now.Subtract(fecha).Minutes;
                    if (minutos < 1)
                    {
                        dias_entre_fecha = "Ahora";

                    }
                    else
                    {
                        dias_entre_fecha = "Hace " + minutos_for + " minutos.";

                    }

                }
                else
                {
                    dias_entre_fecha = "Hace " + horas_for + " horas.";
                }

            }
            else
            {
                dias_entre_fecha = "Hace " + dias_for + " dias.";
            }

            return dias_entre_fecha;
        }




        public void ObternerPais()
        {

            List<SelectListItem> ListaPais = new List<SelectListItem>();
            var paises = from p in autodb.pais orderby p.nombre select new RegistroPais { idPais = p.id_pais, Nombre = p.nombre };

            foreach (RegistroPais p in paises)
            {
                ListaPais.Add(new SelectListItem() { Value = p.Nombre, Text = p.idPais.ToString() });

            }

            ViewBag.DropPaisValue = new SelectList(ListaPais, "Text", "Value");

        }


        [AllowAnonymous]
        public JsonResult RegisterEstados(string Pais)
        {
            int PaisID = Convert.ToInt32(Pais);
            List<SelectListItem> ListaEstado = new List<SelectListItem>();
            var estados = from e in autodb.estado where PaisID == e.fk_pais select new RegitroEstado { idEstado = e.id_estado, Nombre = e.nombre };

            foreach (RegitroEstado e in estados)
            {
                ListaEstado.Add(new SelectListItem() { Value = e.Nombre, Text = e.idEstado.ToString() });

            }
            ViewBag.DropEstadoValue = new SelectList(ListaEstado, "Text", "Value");

            return Json(estados.ToList());
        }


        [AllowAnonymous]
        public JsonResult RegisterLocalidad(string Estado)
        {
            int EstadoID = Convert.ToInt32(Estado);
            List<SelectListItem> ListaLocalidad = new List<SelectListItem>();
            var localidades = from l in autodb.ciudad where EstadoID == l.fk_estado select new RegistroLocalidad { idLocalidad = l.id_ciudad, Nombre = l.nombre };
            foreach (RegistroLocalidad l in localidades)
            {
                ListaLocalidad.Add(new SelectListItem() { Value = l.Nombre, Text = l.idLocalidad.ToString() });

            }
            ViewBag.DropLocalidadValue = new SelectList(ListaLocalidad, "Text", "Value");
            return Json(localidades.ToList());
        }

        public ActionResult RegistarFormulario()
        {
            RegistroClienteViewModel modelo = new RegistroClienteViewModel();
            ObternerPais();
            return PartialView("_RegistarFormulario", modelo);;
        }


        public ActionResult ConsultarCliente_Venta(string id_cliente)
        {
            var cliente_i = from l_clientes in autodb.cliente where l_clientes.id_cliente == id_cliente select new { l_clientes.id_cliente };
            if(cliente_i.Any())
            {

                return Json(new { success = true, tipo = "1" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = false, tipo = "2" }, JsonRequestBehavior.AllowGet);
            }

        }

        public List<string> GetModelStateErrors(ModelStateDictionary ModelState)
        {
            List<string> errorMessages = new List<string>();

            var validationErrors = ModelState.Values.Select(x => x.Errors);
            validationErrors.ToList().ForEach(ve =>
            {
                var errorStrings = ve.Select(x => x.ErrorMessage);
                errorStrings.ToList().ForEach(em =>
                {
                    errorMessages.Add(em);
                });
            });

            return errorMessages;
        }

        [HttpPost]
        public ActionResult RegistroCliente(RegistroClienteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, errors = GetModelStateErrors(ModelState) , tipo = "0" });
            }

            var client = from l_cliente in autodb.cliente where l_cliente.id_cliente == model.ID_Cliente select new { l_cliente.id_cliente };
            if(client.Any())
            {
                return Json(new { success = false, tipo="2" }, JsonRequestBehavior.AllowGet);
            }

            direccion d1 = new direccion
            {

                fk_ciudad = model.localidad,
                descripcion = model.DireccionDetallada
            };
            var dir = autodb.direccion.Add(d1);
            autodb.SaveChanges();

            cliente c1 = new cliente
            {
                apellido = model.Apellido,
                nombre = model.Nombre,
                telefono = model.NumeroTlf,
                correo_electronico = model.Email,
                id_cliente = model.ID_Cliente,
                fk_direccion = dir.id_direccion

            };

            var cliente_nuevo = autodb.cliente.Add(c1);
            try
            {
                autodb.SaveChanges();
                return Json(new { success = true, id_cliente = cliente_nuevo.id_cliente }, JsonRequestBehavior.AllowGet);

            }catch(Exception e)
            {
                return Json(new { success = false, tipo="1" }, JsonRequestBehavior.AllowGet);
            }


        }

        [AllowAnonymous]
        [HttpPost]
        public JsonResult EliminarVehiculoTransaccion(int id, int id_venta)
        {
            var vehiculo_c = from l_vehiculo in autodb.vehiculo where l_vehiculo.id_vehiculo == id select l_vehiculo;
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            var notificacion = "";
            decimal monto_total = 0;
            if (vehiculo_c.Any())
            {
                var vehiculo_i = vehiculo_c.First();
                var trans = (from l_trans in autodb.transaccion_venta where l_trans.id_venta == id_venta select l_trans).First();
                trans.vehiculo.Remove(vehiculo_i);
                //Quitar Comision -x
                foreach (var item in trans.usuario_gana_comision)
                {
                    item.monto = item.monto - CalcularComision(vehiculo_i.valor_venta.Value, item.fk_usuario);
                }
                //Auto disponible
                vehiculo_i.valor_venta = null;
                vehiculo_i.fecha_salida = null;

                try
                {
                    autodb.SaveChanges();


                    notificacion = "OK";
                    monto_total = trans.vehiculo.Sum(x => x.valor_venta.Value);

                    if (trans.vehiculo.Count() == 0)
                    {
                        trans.vehiculo.Clear();
                        trans.usuario_gana_comision.Clear();

                        autodb.transaccion_venta.Remove(trans);
                        autodb.SaveChanges();
                        usuario.id_T_Venta = 0;
                        this.Session["User"] = usuario;
                        MensajeViewModels m1 = new MensajeViewModels
                        {
                            Titulo = "Operacón Completada",
                            Cuerpo = "La Transacción de venta Nro. " + trans.id_venta + " fue anulada, debido a que ya no presenta vehículo en la lista",
                            Tipo_Modal = "modal-warning"
                        };
                        this.Session["Mensaje"] = m1;
                        notificacion = "OK Todos";
                    }

                }
                catch (Exception e)
                {
                    notificacion = "Mas Tarde";
                }
            }
            else
            {
                notificacion = "No Existe";
            }


            return Json(new { tipo = notificacion, monto = monto_total }, JsonRequestBehavior.AllowGet);



        }






    }
}