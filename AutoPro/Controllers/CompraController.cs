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

        public ActionResult BusquedaPorMarca()
        {
            BusquedaPorMarcaViewModels modelo = new BusquedaPorMarcaViewModels();

            var marcas = (from l_marcas in autodb.marca select l_marcas).OrderBy(x => x.nombre);
            List<MarcaViewModels> lista_marcas = new List<MarcaViewModels>();

            foreach (var item in marcas)
            {
               if(item.modelo.Count() > 0)
               {
                   List<ModeloViewModels> lista_modelo = new List<ModeloViewModels>();
                   foreach (var i_modelo in item.modelo)
                   {
                       ModeloViewModels a_modelo = new ModeloViewModels
                       {
                           Modelo = i_modelo
                       };
                       lista_modelo.Add(a_modelo);
                   }


                   MarcaViewModels i_marca = new MarcaViewModels
                   {
                       Marca = item,
                       Cant_Modelos = item.modelo.Count(),
                       Lista_Modelo = lista_modelo

                   };
                   lista_marcas.Add(i_marca);
               }
                   
            }

            modelo.Lista_Marcas = lista_marcas;
            modelo.Total_Modelos = lista_marcas.Sum(x => x.Cant_Modelos);


            return View(modelo);
        }

        public ActionResult BusquedaPorCategoria()
        {
            BusquedaPorCategoriaViewModels modelo = new BusquedaPorCategoriaViewModels();

            var categoria = (from l_cat in autodb.modelo_clasificacion select l_cat).OrderBy(x => x.descripcion);
            List<CategoriaViewModels> lista_categoria = new List<CategoriaViewModels>();

            foreach (var item in categoria)
            {
                if (item.modelo.Count() > 0)
                {
                    List<ModeloViewModels> lista_modelo = new List<ModeloViewModels>();
                    foreach (var i_modelo in item.modelo)
                    {
                        ModeloViewModels a_modelo = new ModeloViewModels
                        {
                            Modelo = i_modelo
                        };
                        lista_modelo.Add(a_modelo);
                    }


                    CategoriaViewModels i_cat = new CategoriaViewModels
                    {
                        Categoria = item,
                        Cant_Modelos = item.modelo.Count(),
                        Lista_Modelo = lista_modelo

                    };
                    lista_categoria.Add(i_cat);
                }

            }

            modelo.Lista_Categoria = lista_categoria;
            modelo.Total_Modelos = lista_categoria.Sum(x => x.Cant_Modelos);


            return View(modelo);
        }

        [Authorize]
        // GET: Busqueda por Modelo
        public ActionResult BusquedaPorModelo ()
        {
            int id_concesionario_session = Convert.ToInt32(this.Session["Concesionario"]);
            var nro_marca = (from c_marca in autodb.marca select c_marca).Count();
            var nro_categoria = (from c_cat in autodb.modelo_clasificacion select c_cat).Count();
            var nro_inventario = (from c_inv in autodb.vehiculo where c_inv.fecha_salida != null && c_inv.fk_concesionario == id_concesionario_session select c_inv).Count();
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            O_Transaccion_CompraViewModels info_transaccion = new O_Transaccion_CompraViewModels();
            if(usuario.id_T_Compra != 0)
            {
                transaccion_compra trans_c = (from l_transaccion_c in autodb.transaccion_compra where l_transaccion_c.id_compra == usuario.id_T_Compra select l_transaccion_c).First();
                info_transaccion.Transaccion = trans_c;
                info_transaccion.Cant_Vehiculo = trans_c.vehiculo.Count();
                info_transaccion.Monto_Total = trans_c.vehiculo.Sum(x => x.valor_compra);
                info_transaccion.Lista_Vehiculo = trans_c.vehiculo.ToList();
            }
            
            BusquedaPorModeloViewModels busqueda_modelo = new BusquedaPorModeloViewModels { 
            Nro_Categoria = nro_categoria,
            Nro_Inventario = nro_inventario,
            Nro_Marca = nro_marca,
            Transaccion = info_transaccion
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
                
                modelo.Valor_Calculado_Maximo = ConsultarValorMaximoModelo(modelo.id,id_concesionario_session,modelo.Estado_Vehiculo);
                modelo.Valor_Calculado_Minimo = ConsultarValorMinimoModelo(modelo.id, id_concesionario_session, modelo.Estado_Vehiculo);
                
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
                

                SelectList lista = LlenarColores();
                
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
                    Nombre = item_auto.modelo1 + " " + item_auto.nombre,
                    id_Concesionario = id_concesionario_session
                };


                return View(compra_modelo);

            }

        }

        

        [HttpPost]
        public ActionResult AdquirirVehiculo(ComprarVehiculoViewModels model)
        {
            if (!ModelState.IsValid)
            {
                SelectList lista = LlenarColores();
                model.Lista_Colores = lista;
                return View(model);
            }
            if (model.id_Concesionario == null)
            {
                return RedirectToAction("Home", "Index");
            }
            
            var concesionario = (from l_concesionario in autodb.concesionario where l_concesionario.id_concesionario == model.id_Concesionario select l_concesionario).First();
            var auto = from l_auto in autodb.modelo where l_auto.id_modelo == model.id_Modelo select l_auto;
            string m_imagen = "";

             HttpPostedFileBase ufile = (HttpPostedFileBase)model.FileUpload;
             if(ufile != null)
             {
                 if (IsImage(ufile) == false)
                 {
                     SelectList lista = LlenarColores();
                     model.Lista_Colores = lista;
                     return View(model);

                 }
                 //Updating File on Server BEGIN
                 var folder = Path.Combine(Server.MapPath("~/Imagen/"), concesionario.nombre, "/Inventario/");
                 var imagePath = Path.Combine(folder, ufile.FileName);
                 if (!System.IO.Directory.Exists(folder))
                 {
                     System.IO.Directory.CreateDirectory(folder);
                 }

                 ufile.SaveAs(imagePath);
                 m_imagen = imagePath;

             }
             else
             {
                 m_imagen = "";
             }

             vehiculo v1 = new vehiculo
             {
                 color = ObtenerColores(model.id_Color),
                 imagen = m_imagen,
                 fecha_ingreso = DateTime.Now,
                 kilometraje = Convert.ToInt32(model.Kilometraje),
                 fk_concesionario = model.id_Concesionario,
                 fk_modelo = model.id_Modelo,
                 fk_vehiculo_estado = ObtenerEstado(model.Estado_Vehiculo),
                 valor_compra = Convert.ToDecimal(model.Valor_Compra)
             };

             var op_add_vehiculo = autodb.vehiculo.Add(v1);
             UsuarioViewModel u1 = this.Session["User"] as UsuarioViewModel;
             transaccion_compra op_add_transaccion = new transaccion_compra();
            
            if(u1.id_T_Compra != 0)
            {   
                //En caso de que exista una transaccion de compra en curso
                op_add_transaccion = (from l_t_compra in autodb.transaccion_compra where l_t_compra.id_compra == u1.id_T_Compra select l_t_compra).First();
            }else
            {
                //En caso de que no exista una transaccion de compra en curso
                transaccion_compra t1 = new transaccion_compra
                {
                    fecha = DateTime.Now,
                    fk_concesionario = model.id_Concesionario,
                    fk_estado_transaccion = 4,
                    fk_usuario = u1.id_Usuario
                };
                op_add_transaccion = autodb.transaccion_compra.Add(t1);
                autodb.SaveChanges();
                u1.id_T_Compra = op_add_transaccion.id_compra;
                this.Session["User"] = u1;
            }
             
             op_add_transaccion.vehiculo.Add(op_add_vehiculo);
            //Alerta a gerente
             model.Valor_Maximo = ConsultarValorMaximoModelo(model.id_Modelo, concesionario.id_concesionario, model.Estado_Vehiculo);
            if(Convert.ToDouble(model.Valor_Compra) > model.Valor_Maximo)
            {
                var usu_gerente = from l_usu in autodb.usuario where l_usu.fk_tipo_usuario == 2 && l_usu.fk_concesionario == model.id_Concesionario select l_usu;
                mensaje m1 = new mensaje { 
                    fecha =  DateTime.Now,
                    texto = "Alerta. En la transaccion de compra Nro. " + op_add_transaccion.id_compra + " realizada por el usuario" + u1.Nombre + " " + u1.Apellido + ", paso el limite del precio estimado en el articulo: " + model.Modelo + " - " +model.Año,
                    titulo = "Limite del valor estimado sobrepasado"
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
                 MensajeViewModels m1 = new MensajeViewModels
                 {
                     Titulo = "Transacción Completada",
                     Cuerpo = "La operacion Nro. " + op_add_transaccion.id_compra + " se a completado con exito. Se ha agregado un Vehiculo: " + model.Nombre + " - " + model.Año + ", Color: " + op_add_vehiculo.color + " y Estado: " + model.Estado_Vehiculo + "% en el Inventario.",
                     Tipo_Modal = "modal-success"
                 };

                 this.Session["Mensaje"] = m1;
                 return RedirectToAction("BusquedaPorModelo", "Compra", new { react = "Transaccion-Completada" });
             }catch(Exception e)
             {
                 SelectList lista = LlenarColores();
                 model.Lista_Colores = lista;
                 ModelState.AddModelError("", "Error: "+ e);
                 return View(model);

             }





        }

        [Authorize]
        public ActionResult ConcretarTransaccion(int id)
        {
            var trans_c = from l_trans in autodb.transaccion_compra where l_trans.id_compra == id select l_trans;
            if (trans_c.Count() > 0)
            {
                var transaccion = trans_c.First();
                UsuarioViewModel user_compra = this.Session["User"] as UsuarioViewModel;
                if (transaccion.fk_usuario == user_compra.id_Usuario)
                {
                    if (transaccion.fk_estado_transaccion == 4)
                    {
                        transaccion.fk_estado_transaccion = 1;
                        try
                        {
                            autodb.SaveChanges();
                            user_compra.id_T_Compra = 0;
                            this.Session["User"] = user_compra;
                            MensajeViewModels m1 = new MensajeViewModels
                            {
                                Titulo = "Operación Completada",
                                Cuerpo = "La Transacción de compra Nro. " + id + " se concreto satisfactoriamente.",
                                Tipo_Modal = "modal-info"
                            };

                            this.Session["Mensaje"] = m1;
                            return RedirectToAction("Index", "Home", new { react = "Transaccion-Completada" });
                        }
                        catch (Exception e)
                        {
                            MensajeViewModels m1 = new MensajeViewModels
                            {
                                Titulo = "Error al completar la operación",
                                Cuerpo = "La Transacción de compra Nro. " + id + " no fue concretada, por favor intente más tarde.",
                                Tipo_Modal = "modal-warning"
                            };
                            this.Session["Mensaje"] = m1;
                            return RedirectToAction("BusquedaPorModelo", "Compra", new { react = "Transaccion-Error" });

                        }

                    }
                    else
                    {
                        MensajeViewModels m1 = new MensajeViewModels
                        {
                            Titulo = "Error al completar la operación",
                            Cuerpo = "Usted no puede concretar una transaccion completada o anulada previamente.",
                            Tipo_Modal = "modal-danger"
                        };

                        this.Session["Mensaje"] = m1;
                        return RedirectToAction("Index", "Home", new { react = "Transaccion-Error" });
                    }


                }
                else
                {
                    MensajeViewModels m1 = new MensajeViewModels
                    {
                        Titulo = "Error al completar la operación",
                        Cuerpo = "Usted no es el usuario autorizado para realizar esta operación",
                        Tipo_Modal = "modal-danger"
                    };

                    this.Session["Mensaje"] = m1;
                    return RedirectToAction("Index", "Home", new { react = "Transaccion-Error" });
                }


            }
            else
            {
                MensajeViewModels m1 = new MensajeViewModels
                {
                    Titulo = "Error al completar la operación",
                    Cuerpo = "La transacción de compra " + id + " no existe.",
                    Tipo_Modal = "modal-danger"
                };

                this.Session["Mensaje"] = m1;
                return RedirectToAction("Index", "Home", new { react = "Transaccion-Error" });
            }


        }

        [AllowAnonymous]
        [HttpPost]
        public JsonResult EliminarVehiculoTransaccion(int id)
        {
            var vehiculo_c = from l_vehiculo in autodb.vehiculo where l_vehiculo.id_vehiculo == id select l_vehiculo;
            UsuarioViewModel usuario = this.Session["User"] as UsuarioViewModel;
            var notificacion = "";
            if(vehiculo_c.Count() > 0)
            {
                var vehiculo_i = vehiculo_c.First();
                var trans = (from l_trans in autodb.transaccion_compra where l_trans.id_compra == usuario.id_T_Compra select l_trans).First();
                trans.vehiculo.Remove(vehiculo_i);
                autodb.vehiculo.Remove(vehiculo_i);
                try
                {
                    autodb.SaveChanges();
                    
                   
                    notificacion = "OK";
                    
                    if (trans.vehiculo.Count() == 0)
                    {
                        trans.fk_estado_transaccion = 2;
                        autodb.SaveChanges();
                        usuario.id_T_Compra = 0;
                        this.Session["User"] = usuario;
                        MensajeViewModels m1 = new MensajeViewModels
                        {
                            Titulo = "Error al completar la operación",
                            Cuerpo = "La Transacción de compra Nro. " + id + " no fue anulada, por favor intente más tarde.",
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


            return Json(notificacion, JsonRequestBehavior.AllowGet);



        }

        [Authorize]
        public ActionResult AnularTransaccion(int id)
        {
            var trans_c = from l_trans in autodb.transaccion_compra where l_trans.id_compra == id select l_trans;
            if(trans_c.Count() > 0)
            {
                var transaccion = trans_c.First();
                UsuarioViewModel user_compra = this.Session["User"] as UsuarioViewModel;
                if(transaccion.fk_usuario == user_compra.id_Usuario)
                {
                    if(transaccion.fk_estado_transaccion == 4)
                    {
                        autodb.vehiculo.RemoveRange(transaccion.vehiculo);
                        transaccion.fk_estado_transaccion = 2;
                        try
                        {
                            autodb.SaveChanges();
                            user_compra.id_T_Compra = 0;
                            this.Session["User"] = user_compra;
                            MensajeViewModels m1 = new MensajeViewModels
                            {
                                Titulo = "Operación Completada",
                                Cuerpo = "La Transacción de compra Nro. "+ id + " fue anulada satisfactoriamente.",
                                Tipo_Modal = "modal-info"
                            };

                            this.Session["Mensaje"] = m1;
                            return RedirectToAction("Index", "Home", new { react = "Transaccion-Completada" });
                        }
                        catch (Exception e)
                        {
                            MensajeViewModels m1 = new MensajeViewModels
                            {
                                Titulo = "Error al completar la operación",
                                Cuerpo = "La Transacción de compra Nro. " + id + " no fue anulada, por favor intente más tarde.",
                                Tipo_Modal = "modal-warning"
                            };
                            this.Session["Mensaje"] = m1;
                            return RedirectToAction("BusquedaPorModelo", "Compra", new { react = "Transaccion-Error" });

                        }

                    }
                    else
                    {
                        MensajeViewModels m1 = new MensajeViewModels
                        {
                            Titulo = "Error al completar la operación",
                            Cuerpo = "Usted no puede anular una transaccion completada o anulada previamente.",
                            Tipo_Modal = "modal-danger"
                        };

                        this.Session["Mensaje"] = m1;
                        return RedirectToAction("Index", "Home", new { react = "Transaccion-Error" });
                    }


                }else
                {
                    MensajeViewModels m1 = new MensajeViewModels
                    {
                        Titulo = "Error al completar la operación",
                        Cuerpo = "Usted no es el usuario autorizado para realizar esta operación",
                        Tipo_Modal = "modal-danger"
                    };

                    this.Session["Mensaje"] = m1;
                    return RedirectToAction("Index", "Home", new { react = "Transaccion-Error" });
                }


            }
            else
            {
                MensajeViewModels m1 = new MensajeViewModels
                {
                    Titulo = "Error al completar la operación",
                    Cuerpo = "La transacción de compra "+id+" no existe.",
                    Tipo_Modal = "modal-danger"
                };

                this.Session["Mensaje"] = m1;
                return RedirectToAction("Index", "Home", new { react = "Transaccion-Error" });
            }

            
        
        }


        //GET Historial
        [Authorize]
        public ActionResult Historial()
        {
            UsuarioViewModel user_compra = (UsuarioViewModel)this.Session["User"];
            var c_l_transaccion_c = (from l_trasaccion in autodb.transaccion_compra where l_trasaccion.fk_usuario == user_compra.id_Usuario && l_trasaccion.fk_estado_transaccion == 1 select l_trasaccion).OrderByDescending(x => x.fecha);
            List<TransaccionCompraViewModels> t_list = new List<TransaccionCompraViewModels>();

            foreach (var item in c_l_transaccion_c)
            {
                List<VehiculoHistorialViewModels> l_vehiculo = new List<VehiculoHistorialViewModels>();

                foreach(var vehiculo in item.vehiculo.ToList())
                {
                    VehiculoHistorialViewModels i_vehiculo = new VehiculoHistorialViewModels
                    {
                        Vehiculo = vehiculo,
                        Costo_Generado = Convert.ToDouble(vehiculo.estructura_costo.Sum(x => x.monto)),
                        Preferencia_Publico = Convert.ToInt32(ConsultarPreferenciaCliente(vehiculo.modelo,vehiculo.fk_concesionario)*100/5)
                    };
                    l_vehiculo.Add(i_vehiculo);

                }
                
                
                TransaccionCompraViewModels item_transaccion = new TransaccionCompraViewModels
                {
                    Transaccion = item,
                    Lista_Vehiculo = l_vehiculo,
                    Tiempo_Transcurrido = CalcularTiempoFecha(item.fecha)

                };

                t_list.Add(item_transaccion);
            }
            HistorialViewModels modelo = new HistorialViewModels
            {
                Lista_Transaccion = t_list
            };


            return View(modelo);
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
                limite_sup = (limite_sup * estado_vehiculo) / 100;

            }
            else
            {
                //Ningun banco los financia, por ende el valor sera de acuerdo a la kbb
                limite_sup = (Convert.ToDouble(m1.valor) * (100 - concesionario.porcentaje_ganancia)) / 100;
                limite_sup = (limite_sup * estado_vehiculo) / 100;

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

        public byte ObtenerEstado(int valor)
        {
            var l_estados = from l_estado in autodb.vehiculo_estado select l_estado;
            byte aux = 0;
            foreach(var item in l_estados)
            {
                if(valor <= item.factor)
                {
                    aux = item.id_vehiculo_estado;
                    break;
                }
            }
            return aux;


        }


        public bool IsImage(HttpPostedFileBase postedFile)
        {
            //-------------------------------------------
            //  Check the image mime types
            //-------------------------------------------
            if (postedFile.ContentType.ToLower() != "image/jpg" &&
                        postedFile.ContentType.ToLower() != "image/jpeg" &&
                        postedFile.ContentType.ToLower() != "image/pjpeg" &&
                        postedFile.ContentType.ToLower() != "image/gif" &&
                        postedFile.ContentType.ToLower() != "image/x-png" &&
                        postedFile.ContentType.ToLower() != "image/png")
            {
                return false;
            }


            return true;
        }

        public SelectList LlenarColores()
        {

            List<SelectListItem> ListaDeColores = new List<SelectListItem>();
                ListaDeColores.Add(new SelectListItem() { Value = "1", Text = "Negro" });
                ListaDeColores.Add(new SelectListItem() { Value = "2", Text = "Blanco" });
                ListaDeColores.Add(new SelectListItem() { Value = "3", Text = "Rojo" });
                ListaDeColores.Add(new SelectListItem() { Value = "4", Text = "Azul" });
                ListaDeColores.Add(new SelectListItem() { Value = "5", Text = "Verde" });
                ListaDeColores.Add(new SelectListItem() { Value = "6", Text = "Dorado" });
                ListaDeColores.Add(new SelectListItem() { Value = "7", Text = "Plateado" });

                SelectList lista = new SelectList(ListaDeColores, "Value", "Text", "1");

                return lista;
        }

        public string ObtenerColores(int valor)
        {
            SelectList lista = LlenarColores();

            var color_l = lista.Where(x => x.Value == valor.ToString());
            var color = color_l.First().Text;



            return color;
        }

        public string CalcularTiempoFecha(DateTime fecha)
        {
            var dias = DateTime.Now.Subtract(fecha).TotalDays;
            var dias_for = DateTime.Now.Subtract(fecha).Days;
            string dias_entre_fecha = "";
            if(dias < 1)
            { 
                var horas = DateTime.Now.Subtract(fecha).TotalHours;
                var horas_for = DateTime.Now.Subtract(fecha).Hours;
                if(horas < 1)
                {
                    var minutos = DateTime.Now.Subtract(fecha).TotalMinutes;
                    var minutos_for = DateTime.Now.Subtract(fecha).Minutes;
                    if(minutos < 1)
                    {
                        dias_entre_fecha = "Ahora";

                    }else{
                        dias_entre_fecha = "Hace " + minutos_for + " minutos.";

                    }

                }else{
                    dias_entre_fecha = "Hace " + horas_for + " horas.";
                }

            }else{
                dias_entre_fecha = "Hace " + dias_for + " dias.";
            }

            return dias_entre_fecha;
        }


    }
}