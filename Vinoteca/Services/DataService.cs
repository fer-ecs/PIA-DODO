using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using Vinoteca.Models;

namespace Vinoteca.Services
{
	// esta seccion sirve para agrupar los datos locales del sistema y dejar esa responsabilidad en un solo archivo - DataService
	public static class DataService
	{
		private const string AdminCorreo = "admin@vinoteca.com";
		private const string AdminContrasena = "Admin_123*";

		private static readonly string appFolder = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Vinoteca");

		private static readonly string dataFolder = Path.Combine(appFolder, "Data");
		private static readonly string usuariosFile = Path.Combine(dataFolder, "usuarios.json");
		private static readonly string productosFile = Path.Combine(dataFolder, "productos.json");
		private static readonly string ventasFile = Path.Combine(dataFolder, "ventas.json");
		private static readonly string categoriasFile = Path.Combine(dataFolder, "categorias.json");
		private static readonly string dominiosCorreoFile = Path.Combine(dataFolder, "dominios_correo.json");
		private static readonly string ventaBorradorFile = Path.Combine(dataFolder, "ventas_borrador.json");

		private static readonly string legacyDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
		private static readonly string legacyUsuariosFile = Path.Combine(legacyDataFolder, "usuarios.json");
		private static readonly string legacyProductosFile = Path.Combine(legacyDataFolder, "productos.json");
		private static readonly string legacyVentasFile = Path.Combine(legacyDataFolder, "ventas.json");
		private static readonly string legacyCategoriasFile = Path.Combine(legacyDataFolder, "categorias.json");
		private static readonly string legacyDominiosCorreoFile = Path.Combine(legacyDataFolder, "dominios_correo.json");

		public static event Action? ProductosActualizados;

		private static readonly HashSet<string> TldsCorreoPermitidos = new(StringComparer.OrdinalIgnoreCase)
		{
			"ac", "ad", "ae", "af", "ag", "ai", "al", "am", "ao", "aq", "ar", "as", "at", "au", "aw", "ax", "az",
			"ba", "bb", "bd", "be", "bf", "bg", "bh", "bi", "bj", "bm", "bn", "bo", "br", "bs", "bt", "bw", "by", "bz",
			"ca", "cc", "cd", "cf", "cg", "ch", "ci", "ck", "cl", "cm", "cn", "co", "com", "cr", "cu", "cv", "cw", "cx", "cy", "cz",
			"de", "dj", "dk", "dm", "do", "dz", "ec", "edu", "ee", "eg", "er", "es", "et", "eu", "fi", "fj", "fk", "fm", "fo", "fr",
			"ga", "gd", "ge", "gf", "gg", "gh", "gi", "gl", "gm", "gn", "gov", "gp", "gq", "gr", "gs", "gt", "gu", "gw", "gy",
			"hk", "hm", "hn", "hr", "ht", "hu", "id", "ie", "il", "im", "in", "info", "io", "iq", "ir", "is", "it",
			"je", "jm", "jo", "jp", "ke", "kg", "kh", "ki", "km", "kn", "kp", "kr", "kw", "ky", "kz",
			"la", "lb", "lc", "li", "lk", "lr", "ls", "lt", "lu", "lv", "ly",
			"ma", "mc", "md", "me", "mg", "mh", "mk", "ml", "mm", "mn", "mo", "mp", "mq", "mr", "ms", "mt", "mu", "mv", "mw", "mx", "my", "mz",
			"na", "name", "nc", "ne", "net", "nf", "ng", "ni", "nl", "no", "np", "nr", "nu", "nz",
			"om", "org", "pa", "pe", "pf", "pg", "ph", "pk", "pl", "pm", "pn", "pr", "pro", "ps", "pt", "pw", "py",
			"qa", "re", "ro", "rs", "ru", "rw", "sa", "sb", "sc", "sd", "se", "sg", "sh", "si", "sk", "sl", "sm", "sn", "so", "sr", "st", "su", "sv", "sx", "sy", "sz",
			"tc", "td", "tf", "tg", "th", "tj", "tk", "tl", "tm", "tn", "to", "tr", "tt", "tv", "tw", "tz",
			"ua", "ug", "uk", "us", "uy", "uz", "va", "vc", "ve", "vg", "vi", "vn", "vu",
			"wf", "ws", "ye", "yt", "za", "zm", "zw"
		};

		private static readonly HashSet<string> SufijosCorreoPermitidos = new(StringComparer.OrdinalIgnoreCase)
		{
			"com", "net", "org", "edu", "gov", "info", "biz", "name", "pro",
			"mx", "com.mx", "org.mx", "net.mx", "edu.mx", "gob.mx",
			"es", "com.es", "nom.es", "org.es", "gob.es", "edu.es",
			"co", "com.co", "net.co", "org.co", "edu.co", "gov.co",
			"ar", "com.ar", "net.ar", "org.ar", "edu.ar", "gob.ar",
			"br", "com.br", "net.br", "org.br", "edu.br", "gov.br",
			"cl", "pe", "com.pe", "net.pe", "org.pe", "edu.pe", "gob.pe",
			"us", "ca", "uk", "co.uk", "org.uk", "me.uk", "ac.uk", "gov.uk",
			"fr", "de", "it", "nl", "io", "me", "tv"
		};

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - InicializarArchivos
		public static void InicializarArchivos()
		{
			try
			{
				if (!Directory.Exists(dataFolder))
				{
					Directory.CreateDirectory(dataFolder);
				}

				MigrarArchivosAnteriores();

				if (!File.Exists(usuariosFile))
				{
					GuardarJson(usuariosFile, CrearUsuariosBase());
				}

				if (!File.Exists(productosFile))
				{
					GuardarJson(productosFile, new List<Producto>());
				}

				if (!File.Exists(ventasFile))
				{
					GuardarJson(ventasFile, new List<Venta>());
				}

				if (!File.Exists(categoriasFile))
				{
					GuardarJson(categoriasFile, CrearCategoriasBase());
				}

				if (!File.Exists(dominiosCorreoFile))
				{
					GuardarJson(dominiosCorreoFile, CrearDominiosCorreoBase());
				}

				if (!File.Exists(ventaBorradorFile))
				{
					GuardarJson(ventaBorradorFile, new List<VentaBorrador>());
				}

				ActualizarUsuariosSistema();
				AsegurarDatosMuestra();
				AsegurarIdentificadores();
				AsegurarCategoriasDeProductos();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error al inicializar archivos: {ex.Message}");
			}
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - MigrarArchivosAnteriores
		private static void MigrarArchivosAnteriores()
		{
			CopiarArchivoSiHaceFalta(legacyUsuariosFile, usuariosFile);
			CopiarArchivoSiHaceFalta(legacyProductosFile, productosFile);
			CopiarArchivoSiHaceFalta(legacyVentasFile, ventasFile);
			CopiarArchivoSiHaceFalta(legacyCategoriasFile, categoriasFile);
			CopiarArchivoSiHaceFalta(legacyDominiosCorreoFile, dominiosCorreoFile);
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - CopiarArchivoSiHaceFalta
		private static void CopiarArchivoSiHaceFalta(string origen, string destino)
		{
			if (File.Exists(destino) || !File.Exists(origen))
			{
				return;
			}

			File.Copy(origen, destino, true);
		}

		// esta seccion sirve para guardar informacion de los datos locales del sistema y mantener los datos persistidos - GuardarJson<T>
		private static void GuardarJson<T>(string ruta, T objeto)
		{
			string json = JsonSerializer.Serialize(objeto, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(ruta, json);
		}

		// esta seccion sirve para ordenar y ajustar datos de los datos locales del sistema para trabajar con valores limpios - NormalizarTextoVisible
		private static string NormalizarTextoVisible(string? valor)
		{
			string limpio = string.Join(" ", (valor ?? string.Empty)
				.Trim()
				.Split(' ', StringSplitOptions.RemoveEmptyEntries));

			if (string.IsNullOrWhiteSpace(limpio))
			{
				return string.Empty;
			}

			return string.Join(" ", limpio.Split(' ').Select(palabra =>
			{
				string minuscula = palabra.ToLowerInvariant();
				return minuscula.Length == 1
					? minuscula.ToUpperInvariant()
					: $"{char.ToUpperInvariant(minuscula[0])}{minuscula[1..]}";
			}));
		}

		private static string NormalizarCorreo(string? valor) => (valor ?? string.Empty).Trim().ToLowerInvariant();

		private static string NormalizarDominioCorreo(string? valor) => (valor ?? string.Empty).Trim().TrimStart('@').ToLowerInvariant();

		private static string NormalizarTextoTecnico(string? valor) => (valor ?? string.Empty).Trim();

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - AsignarSiCambio
		private static bool AsignarSiCambio(string? actual, string nuevo, Action<string> asignar)
		{
			if (string.Equals(actual ?? string.Empty, nuevo, StringComparison.Ordinal))
			{
				return false;
			}

			asignar(nuevo);
			return true;
		}

		// esta seccion sirve para ordenar y ajustar datos de los datos locales del sistema para trabajar con valores limpios - NormalizarUsuario
		private static bool NormalizarUsuario(Usuario usuario)
		{
			bool actualizado = false;

			actualizado = AsignarSiCambio(usuario.Nombre, NormalizarTextoVisible(usuario.Nombre), valor => usuario.Nombre = valor) || actualizado;
			actualizado = AsignarSiCambio(usuario.Correo, NormalizarCorreo(usuario.Correo), valor => usuario.Correo = valor) || actualizado;
			actualizado = AsignarSiCambio(usuario.Rol, RolesSistema.Normalizar(usuario.Rol), valor => usuario.Rol = valor) || actualizado;

			return actualizado;
		}

		// esta seccion sirve para ordenar y ajustar datos de los datos locales del sistema para trabajar con valores limpios - NormalizarProducto
		private static bool NormalizarProducto(Producto producto)
		{
			bool actualizado = false;

			actualizado = AsignarSiCambio(producto.Nombre, NormalizarTextoVisible(producto.Nombre), valor => producto.Nombre = valor) || actualizado;
			actualizado = AsignarSiCambio(producto.Marca, NormalizarTextoVisible(producto.Marca), valor => producto.Marca = valor) || actualizado;
			actualizado = AsignarSiCambio(producto.Categoria, NormalizarTextoVisible(producto.Categoria), valor => producto.Categoria = valor) || actualizado;
			actualizado = AsignarSiCambio(producto.Volumen, NormalizarTextoVisible(producto.Volumen), valor => producto.Volumen = valor) || actualizado;
			actualizado = AsignarSiCambio(producto.ImagenPath, NormalizarTextoTecnico(producto.ImagenPath), valor => producto.ImagenPath = valor) || actualizado;

			if (producto.PrecioVenta < 0)
			{
				producto.PrecioVenta = 0;
				actualizado = true;
			}

			if (producto.Stock < 0)
			{
				producto.Stock = 0;
				actualizado = true;
			}

			if (producto.Stock <= 0 && producto.Activo)
			{
				producto.Activo = false;
				actualizado = true;
			}

			return actualizado;
		}

		// esta seccion sirve para ordenar y ajustar datos de los datos locales del sistema para trabajar con valores limpios - NormalizarVenta
		private static bool NormalizarVenta(Venta venta)
		{
			bool actualizado = false;

			actualizado = AsignarSiCambio(venta.NombreEmpleado, NormalizarTextoVisible(venta.NombreEmpleado), valor => venta.NombreEmpleado = valor) || actualizado;
			actualizado = AsignarSiCambio(venta.CorreoEmpleado, NormalizarCorreo(venta.CorreoEmpleado), valor => venta.CorreoEmpleado = valor) || actualizado;
			actualizado = AsignarSiCambio(venta.NombreCliente, NormalizarTextoVisible(venta.NombreCliente), valor => venta.NombreCliente = valor) || actualizado;
			actualizado = AsignarSiCambio(venta.CorreoCliente, NormalizarCorreo(venta.CorreoCliente), valor => venta.CorreoCliente = valor) || actualizado;
			actualizado = AsignarSiCambio(venta.RolUsuario, RolesSistema.Normalizar(venta.RolUsuario), valor => venta.RolUsuario = valor) || actualizado;
			actualizado = AsignarSiCambio(venta.MetodoPago, NormalizarTextoVisible(venta.MetodoPago), valor => venta.MetodoPago = valor) || actualizado;
			actualizado = AsignarSiCambio(venta.ReferenciaPago, NormalizarTextoTecnico(venta.ReferenciaPago), valor => venta.ReferenciaPago = valor) || actualizado;

			if (venta.Productos == null)
			{
				venta.Productos = new List<CarritoItem>();
				actualizado = true;
			}

			foreach (var item in venta.Productos)
			{
				if (item.Producto != null)
				{
					actualizado = NormalizarProducto(item.Producto) || actualizado;
				}
			}

			return actualizado;
		}

		// esta seccion sirve para ordenar y ajustar datos de los datos locales del sistema para trabajar con valores limpios - NormalizarUsuarios
		private static bool NormalizarUsuarios(List<Usuario> usuarios)
		{
			bool actualizado = false;

			foreach (var usuario in usuarios)
			{
				actualizado = NormalizarUsuario(usuario) || actualizado;
			}

			return actualizado;
		}

		// esta seccion sirve para ordenar y ajustar datos de los datos locales del sistema para trabajar con valores limpios - NormalizarProductos
		private static bool NormalizarProductos(List<Producto> productos)
		{
			bool actualizado = false;

			foreach (var producto in productos)
			{
				actualizado = NormalizarProducto(producto) || actualizado;
			}

			return actualizado;
		}

		// esta seccion sirve para ordenar y ajustar datos de los datos locales del sistema para trabajar con valores limpios - NormalizarVentas
		private static bool NormalizarVentas(List<Venta> ventas)
		{
			bool actualizado = false;

			foreach (var venta in ventas)
			{
				actualizado = NormalizarVenta(venta) || actualizado;
			}

			return actualizado;
		}

		// esta seccion sirve para armar datos o contenido de los datos locales del sistema y devolverlo ya preparado - CrearUsuariosBase
		private static List<Usuario> CrearUsuariosBase()
		{
			return new List<Usuario>
			{
				new Usuario
				{
					Id = "1",
					Nombre = "Administrador",
					Correo = AdminCorreo,
					Contrasena = AdminContrasena,
					Rol = RolesSistema.Administrador,
					Activo = true
				}
			};
		}

		// esta seccion sirve para armar datos o contenido de los datos locales del sistema y devolverlo ya preparado - CrearCategoriasBase
		private static List<string> CrearCategoriasBase()
		{
			return new List<string>
			{
				"Tinto",
				"Blanco",
				"Rosado",
				"Espumoso"
			};
		}

		// esta seccion sirve para armar datos o contenido de los datos locales del sistema y devolverlo ya preparado - CrearDominiosCorreoBase
		private static List<string> CrearDominiosCorreoBase()
		{
			return new List<string>
			{
				"gmail.com",
				"outlook.com",
				"yahoo.com",
				"hotmail.com",
				"live.com",
				"icloud.com",
				"vinoteca.com"
			};
		}

		// esta seccion sirve para actualizar los datos locales del sistema despues de un cambio y sincronizar la pantalla - ActualizarUsuariosSistema
		private static void ActualizarUsuariosSistema()
		{
			var usuarios = ObtenerUsuariosSinInicializar();
			bool actualizados = false;

			foreach (var usuario in usuarios)
			{
				string rolOriginal = usuario.Rol;
				if (string.IsNullOrWhiteSpace(rolOriginal))
				{
					usuario.Rol = usuario.EsAdmin ? RolesSistema.Administrador : RolesSistema.Empleado;
					actualizados = true;
				}
				else
				{
					string rolNormalizado = RolesSistema.Normalizar(rolOriginal);
					if (rolNormalizado != rolOriginal)
					{
						usuario.Rol = rolNormalizado;
						actualizados = true;
					}
				}
			}

			var admin = usuarios.FirstOrDefault(u =>
				!string.IsNullOrWhiteSpace(u.Correo) &&
				u.Correo.Equals(AdminCorreo, StringComparison.OrdinalIgnoreCase));

			if (admin == null)
			{
				usuarios.Insert(0, CrearUsuariosBase().First());
				actualizados = true;
			}
			else
			{
				admin.Nombre ??= "Administrador";
				admin.Contrasena = AdminContrasena;
				admin.Rol = RolesSistema.Administrador;
				admin.Activo = true;
				actualizados = true;
			}

			actualizados = NormalizarUsuarios(usuarios) || actualizados;

			if (actualizados)
			{
				GuardarJson(usuariosFile, usuarios);
			}
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - AsegurarDatosMuestra
		private static void AsegurarDatosMuestra()
		{
			AsegurarUsuariosMuestra();
			AsegurarProductosMuestra();
			AsegurarVentasMuestra();
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - AsegurarUsuariosMuestra
		private static void AsegurarUsuariosMuestra()
		{
			var usuarios = ObtenerUsuariosSinInicializar();
			bool usuariosActualizados = false;

			foreach (var usuarioMuestra in CrearUsuariosMuestra())
			{
				NormalizarUsuario(usuarioMuestra);

				bool existe = usuarios.Any(u =>
					!string.IsNullOrWhiteSpace(u.Correo) &&
					u.Correo.Equals(usuarioMuestra.Correo, StringComparison.OrdinalIgnoreCase));

				if (existe)
				{
					continue;
				}

				usuarioMuestra.Id = ObtenerSiguienteId(usuarios.Select(u => u.Id));
				usuarios.Add(usuarioMuestra);
				usuariosActualizados = true;
			}

			usuariosActualizados = NormalizarUsuarios(usuarios) || usuariosActualizados;

			if (usuariosActualizados)
			{
				GuardarJson(usuariosFile, usuarios);
			}
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - AsegurarProductosMuestra
		private static void AsegurarProductosMuestra()
		{
			var productos = ObtenerProductosSinInicializar();
			bool productosActualizados = false;

			foreach (var productoMuestra in CrearProductosMuestra())
			{
				NormalizarProducto(productoMuestra);

				var productoExistente = productos.FirstOrDefault(p =>
					!string.IsNullOrWhiteSpace(p.Nombre) &&
					!string.IsNullOrWhiteSpace(p.Marca) &&
					p.Nombre.Equals(productoMuestra.Nombre, StringComparison.OrdinalIgnoreCase) &&
					p.Marca.Equals(productoMuestra.Marca, StringComparison.OrdinalIgnoreCase));

				if (productoExistente != null)
				{
					productosActualizados = ActualizarProductoMuestra(productoExistente, productoMuestra) || productosActualizados;
					continue;
				}

				productoMuestra.Id = ObtenerSiguienteId(productos.Select(p => p.Id));
				productos.Add(productoMuestra);
				productosActualizados = true;
			}

			productosActualizados = NormalizarProductos(productos) || productosActualizados;

			if (productosActualizados)
			{
				GuardarJson(productosFile, productos);
			}
		}

		// esta seccion sirve para actualizar los datos locales del sistema despues de un cambio y sincronizar la pantalla - ActualizarProductoMuestra
		private static bool ActualizarProductoMuestra(Producto actual, Producto muestra)
		{
			return NormalizarProducto(actual);
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - AsegurarVentasMuestra
		private static void AsegurarVentasMuestra()
		{
			var usuarios = ObtenerUsuariosSinInicializar();
			var productos = ObtenerProductosSinInicializar();
			var ventas = ObtenerVentasSinInicializar();
			bool ventasActualizadas = false;

			foreach (var ventaMuestra in CrearVentasMuestra(usuarios, productos))
			{
				AsegurarDatosVentaPos(ventaMuestra);
				NormalizarVenta(ventaMuestra);

				if (VentaMuestraExiste(ventas, ventaMuestra))
				{
					continue;
				}

				ventaMuestra.Id = ObtenerSiguienteId(ventas.Select(v => v.Id));
				ventas.Add(ventaMuestra);
				ventasActualizadas = true;
			}

			ventasActualizadas = NormalizarVentas(ventas) || ventasActualizadas;

			if (ventasActualizadas)
			{
				GuardarJson(ventasFile, ventas);
			}
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - VentaMuestraExiste
		private static bool VentaMuestraExiste(List<Venta> ventas, Venta muestra)
		{
			if (!string.IsNullOrWhiteSpace(muestra.ReferenciaPago) &&
				ventas.Any(v => !string.IsNullOrWhiteSpace(v.ReferenciaPago) &&
					v.ReferenciaPago.Equals(muestra.ReferenciaPago, StringComparison.OrdinalIgnoreCase)))
			{
				return true;
			}

			return ventas.Any(v =>
				v.Fecha == muestra.Fecha &&
				!string.IsNullOrWhiteSpace(v.CorreoEmpleado) &&
				!string.IsNullOrWhiteSpace(v.MetodoPago) &&
				v.CorreoEmpleado.Equals(muestra.CorreoEmpleado, StringComparison.OrdinalIgnoreCase) &&
				v.MetodoPago.Equals(muestra.MetodoPago, StringComparison.OrdinalIgnoreCase) &&
				Math.Abs(v.Total - muestra.Total) < 0.01);
		}

		// esta seccion sirve para armar datos o contenido de los datos locales del sistema y devolverlo ya preparado - CrearUsuariosMuestra
		private static List<Usuario> CrearUsuariosMuestra()
		{
			return new List<Usuario>
			{
				new Usuario { Nombre = "Ana Lopez", Correo = "ana_lopez@vinoteca.com", Contrasena = "Ana_123*", Rol = RolesSistema.Supervisor, Activo = true },
				new Usuario { Nombre = "Carlos Mendez", Correo = "carlos_mendez@vinoteca.com", Contrasena = "Carlos_123*", Rol = RolesSistema.Supervisor, Activo = true },
				new Usuario { Nombre = "Laura Perez", Correo = "laura_perez@vinoteca.com", Contrasena = "Laura_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Diego Ruiz", Correo = "diego_ruiz@vinoteca.com", Contrasena = "Diego_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Sofia Vargas", Correo = "sofia_vargas@vinoteca.com", Contrasena = "Sofia_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Miguel Torres", Correo = "miguel_torres@vinoteca.com", Contrasena = "Miguel_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Valeria Castro", Correo = "valeria_castro@vinoteca.com", Contrasena = "Valeria_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Javier Moreno", Correo = "javier_moreno@vinoteca.com", Contrasena = "Javier_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Fernanda Gil", Correo = "fernanda_gil@vinoteca.com", Contrasena = "Fernanda_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Ricardo Salas", Correo = "ricardo_salas@vinoteca.com", Contrasena = "Ricardo_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Daniela Ortiz", Correo = "daniela_ortiz@vinoteca.com", Contrasena = "Daniela_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Eduardo Rios", Correo = "eduardo_rios@vinoteca.com", Contrasena = "Eduardo_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Mariana Vega", Correo = "mariana_vega@vinoteca.com", Contrasena = "Mariana_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Patricia Leon", Correo = "patricia_leon@vinoteca.com", Contrasena = "Patricia_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Hector Nava", Correo = "hector_nava@vinoteca.com", Contrasena = "Hector_123*", Rol = RolesSistema.Empleado, Activo = true }
			};
		}

		// esta seccion sirve para armar datos o contenido de los datos locales del sistema y devolverlo ya preparado - CrearProductosMuestra
		private static List<Producto> CrearProductosMuestra()
		{
			return new List<Producto>
			{
				new Producto { Nombre = "Cabernet Reserva", Marca = "Concha y Toro", Categoria = "Tinto", PrecioVenta = 289.99, Stock = 20, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Merlot Clasico", Marca = "Casillero del Diablo", Categoria = "Tinto", PrecioVenta = 245, Stock = 16, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Malbec Andino", Marca = "Trapiche", Categoria = "Tinto", PrecioVenta = 310, Stock = 16, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Syrah Gran Seleccion", Marca = "LA Cetto", Categoria = "Tinto", PrecioVenta = 275, Stock = 11, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Tempranillo Roble", Marca = "Freixenet", Categoria = "Tinto", PrecioVenta = 260, Stock = 10, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Sauvignon Blanc", Marca = "Santa Rita", Categoria = "Blanco", PrecioVenta = 235, Stock = 15, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Chardonnay Reserva", Marca = "Monte Xanic", Categoria = "Blanco", PrecioVenta = 365, Stock = 9, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Pinot Grigio", Marca = "Gallo Family", Categoria = "Blanco", PrecioVenta = 225.99, Stock = 13, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Moscato Ligero", Marca = "Barefoot", Categoria = "Blanco", PrecioVenta = 210, Stock = 12, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Rose Provence", Marca = "Minuty", Categoria = "Rosado", PrecioVenta = 420, Stock = 8, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Rose de Verano", Marca = "Lancers", Categoria = "Rosado", PrecioVenta = 255, Stock = 15, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Brut Imperial", Marca = "Moet Chandon", Categoria = "Espumoso", PrecioVenta = 1180, Stock = 6, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Prosecco Extra Dry", Marca = "Mionetto", Categoria = "Espumoso", PrecioVenta = 399, Stock = 9, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Cava Brut", Marca = "Freixenet", Categoria = "Espumoso", PrecioVenta = 345, Stock = 17, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Lambrusco Rosso", Marca = "Riunite", Categoria = "Espumoso", PrecioVenta = 230, Stock = 17, ImagenPath = string.Empty, Activo = true }
			};
		}

		// esta seccion sirve para armar datos o contenido de los datos locales del sistema y devolverlo ya preparado - CrearVentasMuestra
		private static List<Venta> CrearVentasMuestra(List<Usuario> usuarios, List<Producto> productos)
		{
			var ventas = new List<Venta>();

			void AgregarVenta(
				string correoEmpleado,
				DateTime fecha,
				string metodoPago,
				string referenciaPago,
				double montoRecibido,
				params (string Nombre, string Marca, int Cantidad)[] productosVenta)
			{
				var empleado = usuarios.FirstOrDefault(u =>
					u.Activo &&
					!string.IsNullOrWhiteSpace(u.Id) &&
					!string.IsNullOrWhiteSpace(u.Nombre) &&
					!string.IsNullOrWhiteSpace(u.Correo) &&
					RolesSistema.Normalizar(u.Rol) == RolesSistema.Empleado &&
					u.Correo.Equals(correoEmpleado, StringComparison.OrdinalIgnoreCase));

				if (empleado == null)
				{
					return;
				}

				var items = new List<CarritoItem>();
				foreach (var item in productosVenta)
				{
					var producto = productos.FirstOrDefault(p =>
						!string.IsNullOrWhiteSpace(p.Nombre) &&
						!string.IsNullOrWhiteSpace(p.Marca) &&
						p.Nombre.Equals(item.Nombre, StringComparison.OrdinalIgnoreCase) &&
						p.Marca.Equals(item.Marca, StringComparison.OrdinalIgnoreCase));

					if (producto == null)
					{
						return;
					}

					items.Add(new CarritoItem
					{
						Producto = CrearProductoParaVenta(producto),
						Cantidad = item.Cantidad
					});
				}

				double total = Math.Round(items.Sum(i => i.Subtotal), 2);
				double recibido = metodoPago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase)
					? montoRecibido
					: total;
				string empleadoId = empleado.Id ?? string.Empty;
				string empleadoNombre = empleado.Nombre ?? string.Empty;
				string empleadoCorreo = empleado.Correo ?? string.Empty;

				ventas.Add(new Venta
				{
					Fecha = fecha,
					UsuarioId = empleadoId,
					EmpleadoId = empleadoId,
					NombreEmpleado = empleadoNombre,
					CorreoEmpleado = empleadoCorreo,
					NombreCliente = empleadoNombre,
					CorreoCliente = empleadoCorreo,
					RolUsuario = RolesSistema.Empleado,
					MetodoPago = metodoPago,
					MontoRecibido = recibido,
					Cambio = Math.Round(Math.Max(recibido - total, 0), 2),
					ReferenciaPago = referenciaPago,
					Productos = items,
					Total = total
				});
			}

			AgregarVenta("laura_perez@vinoteca.com", new DateTime(2026, 5, 1, 10, 15, 0), "Efectivo", "EFEC3001", 900,
				("Cabernet Reserva", "Concha y Toro", 2), ("Rose de Verano", "Lancers", 1));
			AgregarVenta("diego_ruiz@vinoteca.com", new DateTime(2026, 5, 1, 12, 40, 0), "Tarjeta", "TARJ3001", 0,
				("Brut Imperial", "Moet Chandon", 1));
			AgregarVenta("sofia_vargas@vinoteca.com", new DateTime(2026, 5, 2, 11, 5, 0), "Transferencia", "SPEI3001", 0,
				("Chardonnay Reserva", "Monte Xanic", 1), ("Prosecco Extra Dry", "Mionetto", 2));
			AgregarVenta("miguel_torres@vinoteca.com", new DateTime(2026, 5, 2, 16, 20, 0), "Efectivo", "EFEC3002", 750,
				("Merlot Clasico", "Casillero del Diablo", 1), ("Lambrusco Rosso", "Riunite", 2));
			AgregarVenta("valeria_castro@vinoteca.com", new DateTime(2026, 5, 3, 13, 10, 0), "Tarjeta", "TARJ3002", 0,
				("Malbec Andino", "Trapiche", 2), ("Cava Brut", "Freixenet", 1));
			AgregarVenta("javier_moreno@vinoteca.com", new DateTime(2026, 5, 3, 18, 35, 0), "Transferencia", "SPEI3002", 0,
				("Sauvignon Blanc", "Santa Rita", 3));
			AgregarVenta("fernanda_gil@vinoteca.com", new DateTime(2026, 5, 4, 14, 25, 0), "Efectivo", "EFEC3003", 700,
				("Rose Provence", "Minuty", 1), ("Moscato Ligero", "Barefoot", 1));
			AgregarVenta("ricardo_salas@vinoteca.com", new DateTime(2026, 5, 4, 19, 10, 0), "Tarjeta", "TARJ3003", 0,
				("Syrah Gran Seleccion", "LA Cetto", 2), ("Tempranillo Roble", "Freixenet", 1));
			AgregarVenta("daniela_ortiz@vinoteca.com", new DateTime(2026, 5, 5, 10, 50, 0), "Transferencia", "SPEI3003", 0,
				("Pinot Grigio", "Gallo Family", 2), ("Cabernet Reserva", "Concha y Toro", 1));
			AgregarVenta("eduardo_rios@vinoteca.com", new DateTime(2026, 5, 5, 17, 45, 0), "Efectivo", "EFEC3004", 700,
				("Cava Brut", "Freixenet", 2));
			AgregarVenta("mariana_vega@vinoteca.com", new DateTime(2026, 5, 6, 13, 0, 0), "Tarjeta", "TARJ3004", 0,
				("Prosecco Extra Dry", "Mionetto", 1), ("Rose de Verano", "Lancers", 1));
			AgregarVenta("patricia_leon@vinoteca.com", new DateTime(2026, 5, 6, 16, 30, 0), "Transferencia", "SPEI3004", 0,
				("Merlot Clasico", "Casillero del Diablo", 2), ("Moscato Ligero", "Barefoot", 1));
			AgregarVenta("hector_nava@vinoteca.com", new DateTime(2026, 5, 7, 11, 35, 0), "Efectivo", "EFEC3005", 700,
				("Lambrusco Rosso", "Riunite", 3));
			AgregarVenta("laura_perez@vinoteca.com", new DateTime(2026, 5, 7, 18, 15, 0), "Tarjeta", "TARJ3005", 0,
				("Brut Imperial", "Moet Chandon", 1), ("Chardonnay Reserva", "Monte Xanic", 1));
			AgregarVenta("diego_ruiz@vinoteca.com", new DateTime(2026, 5, 8, 12, 5, 0), "Transferencia", "SPEI3005", 0,
				("Malbec Andino", "Trapiche", 1), ("Syrah Gran Seleccion", "LA Cetto", 1), ("Tempranillo Roble", "Freixenet", 1));
			AgregarVenta("sofia_vargas@vinoteca.com", new DateTime(2026, 5, 8, 15, 25, 0), "Efectivo", "EFEC3006", 700,
				("Sauvignon Blanc", "Santa Rita", 1), ("Pinot Grigio", "Gallo Family", 1), ("Moscato Ligero", "Barefoot", 1));
			AgregarVenta("miguel_torres@vinoteca.com", new DateTime(2026, 5, 9, 14, 40, 0), "Tarjeta", "TARJ3006", 0,
				("Cabernet Reserva", "Concha y Toro", 1), ("Merlot Clasico", "Casillero del Diablo", 1), ("Malbec Andino", "Trapiche", 1));
			AgregarVenta("valeria_castro@vinoteca.com", new DateTime(2026, 5, 9, 19, 5, 0), "Transferencia", "SPEI3006", 0,
				("Rose Provence", "Minuty", 1), ("Prosecco Extra Dry", "Mionetto", 1), ("Cava Brut", "Freixenet", 1));

			return ventas;
		}

		// esta seccion sirve para armar datos o contenido de los datos locales del sistema y devolverlo ya preparado - CrearProductoParaVenta
		private static Producto CrearProductoParaVenta(Producto producto)
		{
			return new Producto
			{
				Id = producto.Id,
				Nombre = producto.Nombre,
				Marca = producto.Marca,
				Categoria = producto.Categoria,
				PrecioCompra = producto.PrecioCompra,
				PrecioVenta = producto.PrecioVenta,
				Stock = producto.Stock,
				Volumen = producto.Volumen,
				PorcentajeAlcohol = producto.PorcentajeAlcohol,
				ImagenPath = producto.ImagenPath,
				FechaRegistro = producto.FechaRegistro,
				Activo = producto.Activo
			};
		}

		// esta seccion sirve para leer informacion de los datos locales del sistema y regresarla lista para usarse - ObtenerUsuariosSinInicializar
		private static List<Usuario> ObtenerUsuariosSinInicializar()
		{
			try
			{
				string json = File.ReadAllText(usuariosFile);
				return JsonSerializer.Deserialize<List<Usuario>>(json) ?? new List<Usuario>();
			}
			catch
			{
				return new List<Usuario>();
			}
		}

		// esta seccion sirve para leer informacion de los datos locales del sistema y regresarla lista para usarse - ObtenerProductosSinInicializar
		private static List<Producto> ObtenerProductosSinInicializar()
		{
			try
			{
				string json = File.ReadAllText(productosFile);
				return JsonSerializer.Deserialize<List<Producto>>(json) ?? new List<Producto>();
			}
			catch
			{
				return new List<Producto>();
			}
		}

		// esta seccion sirve para leer informacion de los datos locales del sistema y regresarla lista para usarse - ObtenerCategoriasSinInicializar
		private static List<string> ObtenerCategoriasSinInicializar()
		{
			try
			{
				string json = File.ReadAllText(categoriasFile);
				return JsonSerializer.Deserialize<List<string>>(json) ?? CrearCategoriasBase();
			}
			catch
			{
				return CrearCategoriasBase();
			}
		}

		// esta seccion sirve para leer informacion de los datos locales del sistema y regresarla lista para usarse - ObtenerDominiosCorreoSinInicializar
		private static List<string> ObtenerDominiosCorreoSinInicializar()
		{
			try
			{
				string json = File.ReadAllText(dominiosCorreoFile);
				return JsonSerializer.Deserialize<List<string>>(json) ?? CrearDominiosCorreoBase();
			}
			catch
			{
				return CrearDominiosCorreoBase();
			}
		}

		// esta seccion sirve para leer informacion de los datos locales del sistema y regresarla lista para usarse - ObtenerVentasBorradorSinInicializar
		private static List<VentaBorrador> ObtenerVentasBorradorSinInicializar()
		{
			try
			{
				string json = File.ReadAllText(ventaBorradorFile);
				return JsonSerializer.Deserialize<List<VentaBorrador>>(json) ?? new List<VentaBorrador>();
			}
			catch
			{
				return new List<VentaBorrador>();
			}
		}

		// esta seccion sirve para leer informacion de los datos locales del sistema y regresarla lista para usarse - ObtenerVentasSinInicializar
		private static List<Venta> ObtenerVentasSinInicializar()
		{
			try
			{
				string json = File.ReadAllText(ventasFile);
				return JsonSerializer.Deserialize<List<Venta>>(json) ?? new List<Venta>();
			}
			catch
			{
				return new List<Venta>();
			}
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - AsegurarIdentificadores
		private static void AsegurarIdentificadores()
		{
			var usuarios = ObtenerUsuariosSinInicializar();
			bool usuariosActualizados = NormalizarUsuarios(usuarios);
			usuariosActualizados = AsegurarIdsUsuarios(usuarios, out var mapaUsuarios) || usuariosActualizados;
			if (usuariosActualizados)
			{
				GuardarJson(usuariosFile, usuarios);
			}

			var productos = ObtenerProductosSinInicializar();
			bool productosActualizados = NormalizarProductos(productos);
			productosActualizados = AsegurarIdsProductos(productos, out var mapaProductos) || productosActualizados;
			if (productosActualizados)
			{
				GuardarJson(productosFile, productos);
			}

			var ventas = ObtenerVentasSinInicializar();
			bool ventasActualizadas = NormalizarVentas(ventas);
			ventasActualizadas = AsegurarReferenciasVentas(ventas, mapaUsuarios, mapaProductos) || ventasActualizadas;
			ventasActualizadas = AsegurarIdsVentas(ventas) || ventasActualizadas;
			if (ventasActualizadas)
			{
				GuardarJson(ventasFile, ventas);
			}
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - AsegurarIdsUsuarios
		private static bool AsegurarIdsUsuarios(List<Usuario> usuarios, out Dictionary<string, string> mapa)
		{
			mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (!RequiereRenumeracion(usuarios.Select(u => u.Id)))
			{
				return false;
			}

			for (int i = 0; i < usuarios.Count; i++)
			{
				string idAnterior = usuarios[i].Id ?? string.Empty;
				string idNuevo = (i + 1).ToString();
				if (!string.IsNullOrWhiteSpace(idAnterior) && idAnterior != idNuevo)
				{
					mapa[idAnterior] = idNuevo;
				}

				usuarios[i].Id = idNuevo;
			}

			return true;
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - AsegurarIdsProductos
		private static bool AsegurarIdsProductos(List<Producto> productos, out Dictionary<string, string> mapa)
		{
			mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (!RequiereRenumeracion(productos.Select(p => p.Id)))
			{
				return false;
			}

			for (int i = 0; i < productos.Count; i++)
			{
				string idAnterior = productos[i].Id ?? string.Empty;
				string idNuevo = (i + 1).ToString();
				if (!string.IsNullOrWhiteSpace(idAnterior) && idAnterior != idNuevo)
				{
					mapa[idAnterior] = idNuevo;
				}

				productos[i].Id = idNuevo;
			}

			return true;
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - AsegurarIdsVentas
		private static bool AsegurarIdsVentas(List<Venta> ventas)
		{
			bool actualizados = false;

			if (RequiereRenumeracion(ventas.Select(v => v.Id)))
			{
				for (int i = 0; i < ventas.Count; i++)
				{
					string idNuevo = (i + 1).ToString();
					if (ventas[i].Id != idNuevo)
					{
						ventas[i].Id = idNuevo;
						actualizados = true;
					}
				}
			}

			foreach (var venta in ventas)
			{
				if (AsegurarDatosVentaPos(venta))
				{
					actualizados = true;
				}

				if (NormalizarVenta(venta))
				{
					actualizados = true;
				}
			}

			return actualizados;
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - AsegurarReferenciasVentas
		private static bool AsegurarReferenciasVentas(
			List<Venta> ventas,
			Dictionary<string, string> mapaUsuarios,
			Dictionary<string, string> mapaProductos)
		{
			bool actualizadas = false;

			foreach (var venta in ventas)
			{
				if (ReemplazarId(mapaUsuarios, venta.UsuarioId, out string usuarioId))
				{
					venta.UsuarioId = usuarioId;
					actualizadas = true;
				}

				if (ReemplazarId(mapaUsuarios, venta.EmpleadoId, out string empleadoId))
				{
					venta.EmpleadoId = empleadoId;
					actualizadas = true;
				}

				if (venta.Productos == null)
				{
					continue;
				}

				foreach (var item in venta.Productos)
				{
					if (item.Producto != null && ReemplazarId(mapaProductos, item.Producto.Id, out string productoId))
					{
						item.Producto.Id = productoId;
						actualizadas = true;
					}
				}
			}

			return actualizadas;
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - ReemplazarId
		private static bool ReemplazarId(Dictionary<string, string> mapa, string idActual, out string idNuevo)
		{
			idNuevo = idActual ?? string.Empty;
			if (string.IsNullOrWhiteSpace(idActual))
			{
				return false;
			}

			if (mapa.TryGetValue(idActual, out string? idEncontrado) && !string.IsNullOrWhiteSpace(idEncontrado))
			{
				idNuevo = idEncontrado;
				return true;
			}

			return false;
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - RequiereRenumeracion
		private static bool RequiereRenumeracion(IEnumerable<string> ids)
		{
			var vistos = new HashSet<int>();
			int esperado = 1;
			foreach (string id in ids)
			{
				if (!int.TryParse(id, out int valor) || valor != esperado || !vistos.Add(valor))
				{
					return true;
				}

				esperado++;
			}

			return false;
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - AsegurarDatosVentaPos
		private static bool AsegurarDatosVentaPos(Venta venta)
		{
			bool actualizada = false;

			if (string.IsNullOrWhiteSpace(venta.EmpleadoId))
			{
				venta.EmpleadoId = venta.UsuarioId;
				actualizada = true;
			}

			if (string.IsNullOrWhiteSpace(venta.NombreEmpleado))
			{
				venta.NombreEmpleado = venta.NombreCliente;
				actualizada = true;
			}

			if (string.IsNullOrWhiteSpace(venta.CorreoEmpleado))
			{
				venta.CorreoEmpleado = venta.CorreoCliente;
				actualizada = true;
			}

			if (string.IsNullOrWhiteSpace(venta.NombreCliente))
			{
				venta.NombreCliente = venta.NombreEmpleado;
				actualizada = true;
			}

			if (string.IsNullOrWhiteSpace(venta.CorreoCliente))
			{
				venta.CorreoCliente = venta.CorreoEmpleado;
				actualizada = true;
			}

			string rolNormalizado = RolesSistema.Normalizar(venta.RolUsuario);
			if (venta.RolUsuario != rolNormalizado)
			{
				venta.RolUsuario = rolNormalizado;
				actualizada = true;
			}

			if (string.IsNullOrWhiteSpace(venta.MetodoPago))
			{
				venta.MetodoPago = "Efectivo";
				actualizada = true;
			}

			if (venta.MontoRecibido <= 0 && venta.Total > 0)
			{
				venta.MontoRecibido = venta.Total;
				actualizada = true;
			}

			if (venta.Cambio < 0)
			{
				venta.Cambio = 0;
				actualizada = true;
			}

			return actualizada;
		}

		// esta seccion sirve para leer informacion de los datos locales del sistema y regresarla lista para usarse - ObtenerSiguienteId
		private static string ObtenerSiguienteId(IEnumerable<string> ids)
		{
			int maximo = ids
				.Select(id => int.TryParse(id, out int valor) ? valor : 0)
				.DefaultIfEmpty(0)
				.Max();

			return (maximo + 1).ToString();
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - IdNumericoDisponible
		private static bool IdNumericoDisponible(string id, IEnumerable<string> idsExistentes)
		{
			return int.TryParse(id, out int valor) &&
				valor > 0 &&
				!idsExistentes.Any(idExistente => int.TryParse(idExistente, out int existente) && existente == valor);
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - AsegurarCategoriasDeProductos
		private static void AsegurarCategoriasDeProductos()
		{
			var categorias = ObtenerCategoriasSinInicializar()
				.Where(c => !string.IsNullOrWhiteSpace(c))
				.Select(NormalizarTextoVisible)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			foreach (var producto in ObtenerProductosSinInicializar())
			{
				string categoria = NormalizarTextoVisible(producto.Categoria);
				if (!string.IsNullOrWhiteSpace(categoria) &&
					!categorias.Any(c => c.Equals(categoria, StringComparison.OrdinalIgnoreCase)))
				{
					categorias.Add(categoria);
				}
			}

			if (categorias.Count == 0)
			{
				categorias.AddRange(CrearCategoriasBase());
			}

			GuardarJson(categoriasFile, categorias.OrderBy(c => c).ToList());
		}

		// esta seccion sirve para leer informacion de los datos locales del sistema y regresarla lista para usarse - ObtenerUsuarios
		public static List<Usuario> ObtenerUsuarios()
		{
			InicializarArchivos();
			return ObtenerUsuariosSinInicializar();
		}

		// esta seccion sirve para leer informacion de los datos locales del sistema y regresarla lista para usarse - ObtenerUsuarioPorCorreo
		public static Usuario? ObtenerUsuarioPorCorreo(string correo)
		{
			return ObtenerUsuarios().FirstOrDefault(u =>
				!string.IsNullOrWhiteSpace(u.Correo) &&
				u.Correo.Equals(correo, StringComparison.OrdinalIgnoreCase));
		}

		// esta seccion sirve para guardar informacion de los datos locales del sistema y mantener los datos persistidos - GuardarUsuario
		public static bool GuardarUsuario(Usuario usuario)
		{
			var usuarios = ObtenerUsuarios();
			NormalizarUsuario(usuario);

			if (usuarios.Any(u =>
				!string.IsNullOrWhiteSpace(u.Correo) &&
				u.Correo.Equals(usuario.Correo, StringComparison.OrdinalIgnoreCase)))
			{
				return false;
			}

			if (!IdNumericoDisponible(usuario.Id, usuarios.Select(u => u.Id)))
			{
				usuario.Id = ObtenerSiguienteId(usuarios.Select(u => u.Id));
			}

			usuarios.Add(usuario);
			GuardarJson(usuariosFile, usuarios);
			AsegurarIdentificadores();
			return true;
		}

		// esta seccion sirve para actualizar los datos locales del sistema despues de un cambio y sincronizar la pantalla - ActualizarUsuario
		public static void ActualizarUsuario(Usuario usuario)
		{
			var usuarios = ObtenerUsuarios();
			NormalizarUsuario(usuario);
			var index = usuarios.FindIndex(u => u.Id == usuario.Id);

			if (index < 0)
			{
				return;
			}

			if (usuarios.Any(u =>
				u.Id != usuario.Id &&
				!string.IsNullOrWhiteSpace(u.Correo) &&
				u.Correo.Equals(usuario.Correo, StringComparison.OrdinalIgnoreCase)))
			{
				return;
			}

			usuarios[index] = usuario;
			GuardarJson(usuariosFile, usuarios);
			AsegurarIdentificadores();
		}

		// esta seccion sirve para quitar informacion de los datos locales del sistema y dejar el estado consistente - EliminarUsuario
		public static bool EliminarUsuario(string id)
		{
			var usuarios = ObtenerUsuarios();
			var usuario = usuarios.FirstOrDefault(u => u.Id == id);
			if (usuario == null)
			{
				return false;
			}

			usuarios.Remove(usuario);
			GuardarJson(usuariosFile, usuarios);
			AsegurarIdentificadores();
			return true;
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - ContarAdministradoresActivos
		public static int ContarAdministradoresActivos()
		{
			return ObtenerUsuarios().Count(u => u.Rol == RolesSistema.Administrador && u.Activo);
		}

		// esta seccion sirve para leer informacion de los datos locales del sistema y regresarla lista para usarse - ObtenerProductos
		public static List<Producto> ObtenerProductos()
		{
			InicializarArchivos();
			return ObtenerProductosSinInicializar();
		}

		// esta seccion sirve para leer informacion de los datos locales del sistema y regresarla lista para usarse - ObtenerCategorias
		public static List<string> ObtenerCategorias()
		{
			InicializarArchivos();
			return ObtenerCategoriasSinInicializar()
				.Where(c => !string.IsNullOrWhiteSpace(c))
				.Select(NormalizarTextoVisible)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(c => c)
				.ToList();
		}

		// esta seccion sirve para guardar informacion de los datos locales del sistema y mantener los datos persistidos - GuardarCategoria
		public static bool GuardarCategoria(string categoria)
		{
			var categorias = ObtenerCategorias();
			string categoriaLimpia = NormalizarTextoVisible(categoria);

			if (categorias.Any(c => c.Equals(categoriaLimpia, StringComparison.OrdinalIgnoreCase)))
			{
				return false;
			}

			categorias.Add(categoriaLimpia);
			GuardarJson(categoriasFile, categorias.OrderBy(c => c).ToList());
			return true;
		}

		// esta seccion sirve para quitar informacion de los datos locales del sistema y dejar el estado consistente - EliminarCategoria
		public static bool EliminarCategoria(string categoria)
		{
			var categorias = ObtenerCategorias();
			string categoriaLimpia = NormalizarTextoVisible(categoria);
			var categoriaActual = categorias.FirstOrDefault(c => c.Equals(categoriaLimpia, StringComparison.OrdinalIgnoreCase));

			if (categoriaActual == null)
			{
				return false;
			}

			categorias.Remove(categoriaActual);
			GuardarJson(categoriasFile, categorias);
			return true;
		}

		// esta seccion sirve para leer informacion de los datos locales del sistema y regresarla lista para usarse - ObtenerDominiosCorreo
		public static List<string> ObtenerDominiosCorreo()
		{
			InicializarArchivos();
			return ObtenerDominiosCorreoSinInicializar()
				.Concat(CrearDominiosCorreoBase())
				.Select(NormalizarDominioCorreo)
				.Where(EsDominioCorreoValido)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(d => d)
				.ToList();
		}

		// esta seccion sirve para guardar informacion de los datos locales del sistema y mantener los datos persistidos - GuardarDominioCorreo
		public static bool GuardarDominioCorreo(string dominio)
		{
			var dominios = ObtenerDominiosCorreo();
			string dominioLimpio = NormalizarDominioCorreo(dominio);

			if (!EsDominioCorreoValido(dominioLimpio) ||
				dominios.Any(d => d.Equals(dominioLimpio, StringComparison.OrdinalIgnoreCase)))
			{
				return false;
			}

			dominios.Add(dominioLimpio);
			GuardarJson(dominiosCorreoFile, dominios.OrderBy(d => d).ToList());
			return true;
		}

		// esta seccion sirve para quitar informacion de los datos locales del sistema y dejar el estado consistente - EliminarDominioCorreo
		public static bool EliminarDominioCorreo(string dominio)
		{
			var dominios = ObtenerDominiosCorreo();
			string dominioLimpio = NormalizarDominioCorreo(dominio);
			var dominioActual = dominios.FirstOrDefault(d => d.Equals(dominioLimpio, StringComparison.OrdinalIgnoreCase));

			if (dominioActual == null ||
				CrearDominiosCorreoBase().Any(d => d.Equals(dominioActual, StringComparison.OrdinalIgnoreCase)))
			{
				return false;
			}

			dominios.Remove(dominioActual);
			GuardarJson(dominiosCorreoFile, dominios);
			return true;
		}

		// esta seccion sirve para manejar los datos locales del sistema y concentrar aqui esta parte del flujo - DominioCorreoEnUso
		public static bool DominioCorreoEnUso(string dominio)
		{
			string dominioLimpio = NormalizarDominioCorreo(dominio);
			return ObtenerUsuarios().Any(u =>
				!string.IsNullOrWhiteSpace(u.Correo) &&
				u.Correo.EndsWith("@" + dominioLimpio, StringComparison.OrdinalIgnoreCase));
		}

		// esta seccion sirve para revisar reglas de los datos locales del sistema y evitar que pase un dato incorrecto - EsDominioCorreoValido
		public static bool EsDominioCorreoValido(string dominio)
		{
			string limpio = NormalizarDominioCorreo(dominio);
			if (limpio.Length is < 4 or > 80 ||
				limpio.Contains("..") ||
				limpio.StartsWith('.') ||
				limpio.EndsWith('.') ||
				!Regex.IsMatch(limpio, @"^[a-z0-9]+(?:\.[a-z0-9]+){1,2}$"))
			{
				return false;
			}

			string[] partes = limpio.Split('.');
			string sufijo = ObtenerSufijoCorreo(limpio);
			string nombreDominio = limpio[..^(sufijo.Length + 1)];

			if (string.IsNullOrWhiteSpace(sufijo) ||
				string.IsNullOrWhiteSpace(nombreDominio) ||
				!TldsCorreoPermitidos.Contains(partes[^1]))
			{
				return false;
			}

			if (TldsCorreoPermitidos.Contains(partes[0]) ||
				SufijosCorreoPermitidos.Contains(nombreDominio))
			{
				return false;
			}

			for (int i = 0; i < partes.Length; i++)
			{
				string parte = partes[i];
				if (parte.Length is < 2 or > 63 ||
					parte.All(char.IsDigit) ||
					(i > 0 && parte.Equals(partes[i - 1], StringComparison.OrdinalIgnoreCase)))
				{
					return false;
				}
			}

			return true;
		}

		// esta seccion sirve para leer informacion de los datos locales del sistema y regresarla lista para usarse - ObtenerSufijoCorreo
		private static string ObtenerSufijoCorreo(string dominio)
		{
			string[] partes = dominio.Split('.');
			int cantidad = partes.Length == 3 ? 2 : 1;
			string sufijo = string.Join('.', partes.Skip(partes.Length - cantidad));
			return SufijosCorreoPermitidos.Contains(sufijo)
				? sufijo
				: string.Empty;
		}

		// esta seccion sirve para guardar informacion de los datos locales del sistema y mantener los datos persistidos - GuardarProducto
		public static void GuardarProducto(Producto producto)
		{
			var productos = ObtenerProductos();
			NormalizarProducto(producto);
			var index = productos.FindIndex(p => p.Id == producto.Id);

			if (index >= 0)
			{
				productos[index] = producto;
			}
			else
			{
				if (!IdNumericoDisponible(producto.Id, productos.Select(p => p.Id)))
				{
					producto.Id = ObtenerSiguienteId(productos.Select(p => p.Id));
				}

				productos.Add(producto);
			}

			GuardarJson(productosFile, productos);
			AsegurarIdentificadores();
			NotificarProductosActualizados();
		}

		// esta seccion sirve para quitar informacion de los datos locales del sistema y dejar el estado consistente - EliminarProducto
		public static void EliminarProducto(string id)
		{
			var productos = ObtenerProductos();
			var productoAEliminar = productos.FirstOrDefault(p => p.Id == id);

			if (productoAEliminar != null)
			{
				productos.Remove(productoAEliminar);
				GuardarJson(productosFile, productos);
				AsegurarIdentificadores();
				NotificarProductosActualizados();
			}
		}

		// esta seccion sirve para leer informacion de los datos locales del sistema y regresarla lista para usarse - ObtenerVentas
		public static List<Venta> ObtenerVentas()
		{
			InicializarArchivos();
			if (!File.Exists(ventasFile))
			{
				return new List<Venta>();
			}

			return ObtenerVentasSinInicializar();
		}

		// esta seccion sirve para leer informacion de los datos locales del sistema y regresarla lista para usarse - ObtenerVentasPorUsuario
		public static List<Venta> ObtenerVentasPorUsuario(string usuarioId)
		{
			return ObtenerVentas()
				.Where(v => v.UsuarioId == usuarioId || v.EmpleadoId == usuarioId)
				.OrderByDescending(v => v.Fecha)
				.ToList();
		}

		// esta seccion sirve para guardar informacion de los datos locales del sistema y mantener los datos persistidos - GuardarVenta
		public static void GuardarVenta(Venta nuevaVenta)
		{
			if (!SessionService.PuedeProcesarVentas)
			{
				throw new InvalidOperationException("Solo empleados pueden registrar ventas");
			}

			AsegurarDatosVentaPos(nuevaVenta);
			NormalizarVenta(nuevaVenta);

			var ventas = ObtenerVentas();
			if (!IdNumericoDisponible(nuevaVenta.Id, ventas.Select(v => v.Id)))
			{
				nuevaVenta.Id = ObtenerSiguienteId(ventas.Select(v => v.Id));
			}

			ventas.Add(nuevaVenta);
			GuardarJson(ventasFile, ventas);
			AsegurarIdentificadores();
		}

		// esta seccion sirve para guardar informacion de los datos locales del sistema y mantener los datos persistidos - RegistrarVentaConInventario
		public static List<string> RegistrarVentaConInventario(Venta nuevaVenta)
		{
			if (!SessionService.PuedeProcesarVentas)
			{
				throw new InvalidOperationException("Solo empleados pueden registrar ventas");
			}

			InicializarArchivos();
			AsegurarDatosVentaPos(nuevaVenta);
			NormalizarVenta(nuevaVenta);

			if (nuevaVenta.Productos == null || nuevaVenta.Productos.Count == 0)
			{
				throw new InvalidOperationException("No hay productos para procesar");
			}

			var productos = ObtenerProductosSinInicializar();
			var productosVenta = new List<CarritoItem>();
			var alertasStock = new List<string>();

			foreach (var item in nuevaVenta.Productos)
			{
				if (item?.Producto == null || string.IsNullOrWhiteSpace(item.Producto.Id) || item.Cantidad <= 0)
				{
					throw new InvalidOperationException("Quita productos sin cantidad valida antes de cobrar");
				}

				var productoActual = productos.FirstOrDefault(p => p.Id == item.Producto.Id && p.Activo);
				if (productoActual == null)
				{
					throw new InvalidOperationException($"El producto {item.Producto.Nombre} ya no esta disponible");
				}

				if (productoActual.Stock <= 0)
				{
					throw new InvalidOperationException($"No hay stock disponible para {productoActual.Nombre}");
				}

				if (item.Cantidad > productoActual.Stock)
				{
					throw new InvalidOperationException($"Stock insuficiente para {productoActual.Nombre}");
				}

				productosVenta.Add(new CarritoItem
				{
					Producto = CopiarProducto(productoActual),
					Cantidad = item.Cantidad
				});

				productoActual.Stock -= item.Cantidad;
				if (productoActual.Stock <= 0)
				{
					productoActual.Stock = 0;
					productoActual.Activo = false;
					alertasStock.Add($"{productoActual.Nombre}: sin stock disponible");
				}
				else if (productoActual.Stock < 5)
				{
					alertasStock.Add($"{productoActual.Nombre}: stock bajo ({productoActual.Stock})");
				}
			}

			nuevaVenta.Productos = productosVenta;
			nuevaVenta.Total = Math.Round(productosVenta.Sum(item => item.Subtotal), 2);

			var ventas = ObtenerVentasSinInicializar();
			if (!IdNumericoDisponible(nuevaVenta.Id, ventas.Select(v => v.Id)))
			{
				nuevaVenta.Id = ObtenerSiguienteId(ventas.Select(v => v.Id));
			}

			ventas.Add(nuevaVenta);
			GuardarJson(productosFile, productos);
			GuardarJson(ventasFile, ventas);
			AsegurarIdentificadores();
			NotificarProductosActualizados();

			return alertasStock;
		}

		// esta seccion sirve para armar datos o contenido de los datos locales del sistema y devolverlo ya preparado - CopiarProducto
		private static Producto CopiarProducto(Producto producto)
		{
			return new Producto
			{
				Id = producto.Id,
				Nombre = producto.Nombre,
				Marca = producto.Marca,
				Categoria = producto.Categoria,
				PrecioCompra = producto.PrecioCompra,
				PrecioVenta = producto.PrecioVenta,
				Stock = producto.Stock,
				Volumen = producto.Volumen,
				PorcentajeAlcohol = producto.PorcentajeAlcohol,
				ImagenPath = producto.ImagenPath,
				FechaRegistro = producto.FechaRegistro,
				Activo = producto.Activo
			};
		}

		// esta seccion sirve para actualizar los datos locales del sistema despues de un cambio y sincronizar la pantalla - NotificarProductosActualizados
		private static void NotificarProductosActualizados()
		{
			ProductosActualizados?.Invoke();
		}

		// esta seccion sirve para leer informacion de los datos locales del sistema y regresarla lista para usarse - ObtenerVentaBorrador
		public static VentaBorrador? ObtenerVentaBorrador(string usuarioId)
		{
			InicializarArchivos();
			return ObtenerVentasBorradorSinInicializar()
				.FirstOrDefault(v => v.UsuarioId == usuarioId);
		}

		// esta seccion sirve para guardar informacion de los datos locales del sistema y mantener los datos persistidos - GuardarVentaBorrador
		public static void GuardarVentaBorrador(VentaBorrador borrador)
		{
			if (string.IsNullOrWhiteSpace(borrador.UsuarioId) || borrador.Productos.Count == 0)
			{
				return;
			}

			var borradores = ObtenerVentasBorradorSinInicializar();
			borradores.RemoveAll(v => v.UsuarioId == borrador.UsuarioId);
			borrador.FechaActualizacion = DateTime.Now;
			borradores.Add(borrador);
			GuardarJson(ventaBorradorFile, borradores);
		}

		// esta seccion sirve para quitar informacion de los datos locales del sistema y dejar el estado consistente - EliminarVentaBorrador
		public static void EliminarVentaBorrador(string usuarioId)
		{
			var borradores = ObtenerVentasBorradorSinInicializar();
			if (borradores.RemoveAll(v => v.UsuarioId == usuarioId) > 0)
			{
				GuardarJson(ventaBorradorFile, borradores);
			}
		}
	}
}
