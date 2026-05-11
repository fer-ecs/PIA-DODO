using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Vinoteca.Models;

namespace Vinoteca.Services
{
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

		private static readonly string legacyDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
		private static readonly string legacyUsuariosFile = Path.Combine(legacyDataFolder, "usuarios.json");
		private static readonly string legacyProductosFile = Path.Combine(legacyDataFolder, "productos.json");
		private static readonly string legacyVentasFile = Path.Combine(legacyDataFolder, "ventas.json");
		private static readonly string legacyCategoriasFile = Path.Combine(legacyDataFolder, "categorias.json");

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

		private static void MigrarArchivosAnteriores()
		{
			CopiarArchivoSiHaceFalta(legacyUsuariosFile, usuariosFile);
			CopiarArchivoSiHaceFalta(legacyProductosFile, productosFile);
			CopiarArchivoSiHaceFalta(legacyVentasFile, ventasFile);
			CopiarArchivoSiHaceFalta(legacyCategoriasFile, categoriasFile);
		}

		private static void CopiarArchivoSiHaceFalta(string origen, string destino)
		{
			if (File.Exists(destino) || !File.Exists(origen))
			{
				return;
			}

			File.Copy(origen, destino, true);
		}

		private static void GuardarJson<T>(string ruta, T objeto)
		{
			string json = JsonSerializer.Serialize(objeto, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(ruta, json);
		}

		private static List<Usuario> CrearUsuariosBase()
		{
			return new List<Usuario>
			{
				new Usuario
				{
					Nombre = "Administrador",
					Correo = AdminCorreo,
					Contrasena = AdminContrasena,
					Rol = RolesSistema.Administrador,
					Activo = true
				}
			};
		}

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

			if (actualizados)
			{
				GuardarJson(usuariosFile, usuarios);
			}
		}

		private static void AsegurarDatosMuestra()
		{
			var usuarios = ObtenerUsuariosSinInicializar();
			bool usuariosActualizados = false;

			foreach (var usuarioMuestra in CrearUsuariosMuestra())
			{
				var usuarioExistente = usuarios.FirstOrDefault(u =>
					!string.IsNullOrWhiteSpace(u.Correo) &&
					u.Correo.Equals(usuarioMuestra.Correo, StringComparison.OrdinalIgnoreCase));

				if (usuarioExistente == null)
				{
					usuarios.Add(usuarioMuestra);
					usuariosActualizados = true;
					continue;
				}

				string rolNormalizado = RolesSistema.Normalizar(usuarioExistente.Rol);
				if (usuarioExistente.Rol != rolNormalizado)
				{
					usuarioExistente.Rol = rolNormalizado;
					usuariosActualizados = true;
				}
			}

			if (usuariosActualizados)
			{
				GuardarJson(usuariosFile, usuarios);
			}

			var productos = ObtenerProductosSinInicializar();
			bool productosActualizados = false;

			foreach (var productoMuestra in CrearProductosMuestra())
			{
				bool existe = productos.Any(p =>
					!string.IsNullOrWhiteSpace(p.Nombre) &&
					!string.IsNullOrWhiteSpace(p.Marca) &&
					p.Nombre.Equals(productoMuestra.Nombre, StringComparison.OrdinalIgnoreCase) &&
					p.Marca.Equals(productoMuestra.Marca, StringComparison.OrdinalIgnoreCase));

				if (existe)
				{
					continue;
				}

				productos.Add(productoMuestra);
				productosActualizados = true;
			}

			if (productosActualizados)
			{
				GuardarJson(productosFile, productos);
			}
		}

		private static List<Usuario> CrearUsuariosMuestra()
		{
			return new List<Usuario>
			{
				new Usuario { Nombre = "Ana Lopez", Correo = "ana.lopez@vinoteca.com", Contrasena = "Ana_123*", Rol = RolesSistema.Supervisor, Activo = true },
				new Usuario { Nombre = "Carlos Mendez", Correo = "carlos.mendez@vinoteca.com", Contrasena = "Carlos_123*", Rol = RolesSistema.Supervisor, Activo = true },
				new Usuario { Nombre = "Laura Perez", Correo = "laura.perez@vinoteca.com", Contrasena = "Laura_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Diego Ruiz", Correo = "diego.ruiz@vinoteca.com", Contrasena = "Diego_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Sofia Vargas", Correo = "sofia.vargas@vinoteca.com", Contrasena = "Sofia_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Miguel Torres", Correo = "miguel.torres@vinoteca.com", Contrasena = "Miguel_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Valeria Castro", Correo = "valeria.castro@vinoteca.com", Contrasena = "Valeria_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Javier Moreno", Correo = "javier.moreno@vinoteca.com", Contrasena = "Javier_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Fernanda Gil", Correo = "fernanda.gil@vinoteca.com", Contrasena = "Fernanda_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Ricardo Salas", Correo = "ricardo.salas@vinoteca.com", Contrasena = "Ricardo_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Daniela Ortiz", Correo = "daniela.ortiz@vinoteca.com", Contrasena = "Daniela_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Eduardo Rios", Correo = "eduardo.rios@vinoteca.com", Contrasena = "Eduardo_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Mariana Vega", Correo = "mariana.vega@vinoteca.com", Contrasena = "Mariana_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Patricia Leon", Correo = "patricia.leon@vinoteca.com", Contrasena = "Patricia_123*", Rol = RolesSistema.Empleado, Activo = true },
				new Usuario { Nombre = "Hector Nava", Correo = "hector.nava@vinoteca.com", Contrasena = "Hector_123*", Rol = RolesSistema.Empleado, Activo = true }
			};
		}

		private static List<Producto> CrearProductosMuestra()
		{
			return new List<Producto>
			{
				new Producto { Nombre = "Cabernet Reserva", Marca = "Concha y Toro", Categoria = "Tinto", PrecioVenta = 289, Stock = 24, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Merlot Clasico", Marca = "Casillero del Diablo", Categoria = "Tinto", PrecioVenta = 245, Stock = 18, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Malbec Andino", Marca = "Trapiche", Categoria = "Tinto", PrecioVenta = 310, Stock = 20, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Syrah Gran Seleccion", Marca = "LA Cetto", Categoria = "Tinto", PrecioVenta = 275, Stock = 14, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Tempranillo Roble", Marca = "Freixenet", Categoria = "Tinto", PrecioVenta = 260, Stock = 12, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Sauvignon Blanc", Marca = "Santa Rita", Categoria = "Blanco", PrecioVenta = 235, Stock = 19, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Chardonnay Reserva", Marca = "Monte Xanic", Categoria = "Blanco", PrecioVenta = 365, Stock = 11, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Pinot Grigio", Marca = "Gallo Family", Categoria = "Blanco", PrecioVenta = 225, Stock = 16, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Moscato Ligero", Marca = "Barefoot", Categoria = "Blanco", PrecioVenta = 210, Stock = 15, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Rose Provence", Marca = "Minuty", Categoria = "Rosado", PrecioVenta = 420, Stock = 10, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Rose de Verano", Marca = "Lancers", Categoria = "Rosado", PrecioVenta = 255, Stock = 17, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Brut Imperial", Marca = "Moet Chandon", Categoria = "Espumoso", PrecioVenta = 1180, Stock = 8, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Prosecco Extra Dry", Marca = "Mionetto", Categoria = "Espumoso", PrecioVenta = 399, Stock = 13, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Cava Brut", Marca = "Freixenet", Categoria = "Espumoso", PrecioVenta = 345, Stock = 21, ImagenPath = string.Empty, Activo = true },
				new Producto { Nombre = "Lambrusco Rosso", Marca = "Riunite", Categoria = "Espumoso", PrecioVenta = 230, Stock = 22, ImagenPath = string.Empty, Activo = true }
			};
		}

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

		private static void AsegurarIdentificadores()
		{
			var usuarios = ObtenerUsuariosSinInicializar();
			var mapaUsuarios = AsegurarIdsUsuarios(usuarios);
			if (mapaUsuarios.Count > 0)
			{
				GuardarJson(usuariosFile, usuarios);
			}

			var productos = ObtenerProductosSinInicializar();
			var mapaProductos = AsegurarIdsProductos(productos);
			if (mapaProductos.Count > 0)
			{
				GuardarJson(productosFile, productos);
			}

			var ventas = ObtenerVentasSinInicializar();
			bool ventasActualizadas = AsegurarReferenciasVentas(ventas, mapaUsuarios, mapaProductos);
			ventasActualizadas = AsegurarIdsVentas(ventas) || ventasActualizadas;
			if (ventasActualizadas)
			{
				GuardarJson(ventasFile, ventas);
			}
		}

		private static Dictionary<string, string> AsegurarIdsUsuarios(List<Usuario> usuarios)
		{
			var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (!RequiereRenumeracion(usuarios.Select(u => u.Id)))
			{
				return mapa;
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

			return mapa;
		}

		private static Dictionary<string, string> AsegurarIdsProductos(List<Producto> productos)
		{
			var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (!RequiereRenumeracion(productos.Select(p => p.Id)))
			{
				return mapa;
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

			return mapa;
		}

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
			}

			return actualizados;
		}

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

		private static string ObtenerSiguienteId(IEnumerable<string> ids)
		{
			int ultimoId = ids
				.Select(id => int.TryParse(id, out int valor) ? valor : 0)
				.DefaultIfEmpty(0)
				.Max();

			return (ultimoId + 1).ToString();
		}

		private static bool IdNumericoDisponible(string id, IEnumerable<string> idsExistentes)
		{
			return int.TryParse(id, out int valor) &&
				valor > 0 &&
				!idsExistentes.Any(idExistente => int.TryParse(idExistente, out int existente) && existente == valor);
		}

		private static void AsegurarCategoriasDeProductos()
		{
			var categorias = ObtenerCategoriasSinInicializar()
				.Where(c => !string.IsNullOrWhiteSpace(c))
				.Select(c => c.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			foreach (var producto in ObtenerProductosSinInicializar())
			{
				if (!string.IsNullOrWhiteSpace(producto.Categoria) &&
					!categorias.Any(c => c.Equals(producto.Categoria, StringComparison.OrdinalIgnoreCase)))
				{
					categorias.Add(producto.Categoria.Trim());
				}
			}

			if (categorias.Count == 0)
			{
				categorias.AddRange(CrearCategoriasBase());
			}

			GuardarJson(categoriasFile, categorias.OrderBy(c => c).ToList());
		}

		public static List<Usuario> ObtenerUsuarios()
		{
			InicializarArchivos();
			return ObtenerUsuariosSinInicializar();
		}

		public static Usuario? ObtenerUsuarioPorCorreo(string correo)
		{
			return ObtenerUsuarios().FirstOrDefault(u =>
				!string.IsNullOrWhiteSpace(u.Correo) &&
				u.Correo.Equals(correo, StringComparison.OrdinalIgnoreCase));
		}

		public static bool GuardarUsuario(Usuario usuario)
		{
			var usuarios = ObtenerUsuarios();

			if (usuarios.Any(u =>
				!string.IsNullOrWhiteSpace(u.Correo) &&
				u.Correo.Equals(usuario.Correo, StringComparison.OrdinalIgnoreCase)))
			{
				return false;
			}

			usuario.Rol = RolesSistema.Normalizar(usuario.Rol);
			if (!IdNumericoDisponible(usuario.Id, usuarios.Select(u => u.Id)))
			{
				usuario.Id = ObtenerSiguienteId(usuarios.Select(u => u.Id));
			}

			usuarios.Add(usuario);
			GuardarJson(usuariosFile, usuarios);
			return true;
		}

		public static void ActualizarUsuario(Usuario usuario)
		{
			var usuarios = ObtenerUsuarios();
			var index = usuarios.FindIndex(u => u.Id == usuario.Id);

			if (index < 0)
			{
				return;
			}

			usuario.Rol = RolesSistema.Normalizar(usuario.Rol);
			usuarios[index] = usuario;
			GuardarJson(usuariosFile, usuarios);
		}

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
			return true;
		}

		public static int ContarAdministradoresActivos()
		{
			return ObtenerUsuarios().Count(u => u.Rol == RolesSistema.Administrador && u.Activo);
		}

		public static List<Producto> ObtenerProductos()
		{
			InicializarArchivos();
			return ObtenerProductosSinInicializar();
		}

		public static List<string> ObtenerCategorias()
		{
			InicializarArchivos();
			return ObtenerCategoriasSinInicializar()
				.Where(c => !string.IsNullOrWhiteSpace(c))
				.Select(c => c.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(c => c)
				.ToList();
		}

		public static bool GuardarCategoria(string categoria)
		{
			var categorias = ObtenerCategorias();
			string categoriaLimpia = categoria.Trim();

			if (categorias.Any(c => c.Equals(categoriaLimpia, StringComparison.OrdinalIgnoreCase)))
			{
				return false;
			}

			categorias.Add(categoriaLimpia);
			GuardarJson(categoriasFile, categorias.OrderBy(c => c).ToList());
			return true;
		}

		public static bool EliminarCategoria(string categoria)
		{
			var categorias = ObtenerCategorias();
			string categoriaLimpia = categoria.Trim();
			var categoriaActual = categorias.FirstOrDefault(c => c.Equals(categoriaLimpia, StringComparison.OrdinalIgnoreCase));

			if (categoriaActual == null)
			{
				return false;
			}

			categorias.Remove(categoriaActual);
			GuardarJson(categoriasFile, categorias);
			return true;
		}

		public static void GuardarProducto(Producto producto)
		{
			var productos = ObtenerProductos();
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
		}

		public static void EliminarProducto(string id)
		{
			var productos = ObtenerProductos();
			var productoAEliminar = productos.FirstOrDefault(p => p.Id == id);

			if (productoAEliminar != null)
			{
				productos.Remove(productoAEliminar);
				GuardarJson(productosFile, productos);
			}
		}

		public static List<Venta> ObtenerVentas()
		{
			InicializarArchivos();
			if (!File.Exists(ventasFile))
			{
				return new List<Venta>();
			}

			return ObtenerVentasSinInicializar();
		}

		public static List<Venta> ObtenerVentasPorUsuario(string usuarioId)
		{
			return ObtenerVentas()
				.Where(v => v.UsuarioId == usuarioId || v.EmpleadoId == usuarioId)
				.OrderByDescending(v => v.Fecha)
				.ToList();
		}

		public static void GuardarVenta(Venta nuevaVenta)
		{
			if (!SessionService.PuedeProcesarVentas)
			{
				throw new InvalidOperationException("Solo empleados pueden registrar ventas");
			}

			AsegurarDatosVentaPos(nuevaVenta);

			var ventas = ObtenerVentas();
			if (!IdNumericoDisponible(nuevaVenta.Id, ventas.Select(v => v.Id)))
			{
				nuevaVenta.Id = ObtenerSiguienteId(ventas.Select(v => v.Id));
			}

			ventas.Add(nuevaVenta);
			GuardarJson(ventasFile, ventas);
		}
	}
}
