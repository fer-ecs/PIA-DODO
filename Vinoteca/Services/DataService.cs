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

		private static readonly string legacyDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
		private static readonly string legacyUsuariosFile = Path.Combine(legacyDataFolder, "usuarios.json");
		private static readonly string legacyProductosFile = Path.Combine(legacyDataFolder, "productos.json");
		private static readonly string legacyVentasFile = Path.Combine(legacyDataFolder, "ventas.json");

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

				ActualizarUsuariosSistema();
				AsegurarDatosMuestra();
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
					Id = Guid.NewGuid().ToString(),
					Nombre = "Administrador",
					Correo = AdminCorreo,
					Contrasena = AdminContrasena,
					Rol = RolesSistema.Administrador,
					Activo = true
				}
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
					usuario.Rol = usuario.EsAdmin ? RolesSistema.Administrador : RolesSistema.Cliente;
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
				new Usuario { Id = Guid.NewGuid().ToString(), Nombre = "Ana Lopez", Correo = "ana.lopez@vinoteca.com", Contrasena = "Ana_123*", Rol = RolesSistema.Supervisor, Activo = true },
				new Usuario { Id = Guid.NewGuid().ToString(), Nombre = "Carlos Mendez", Correo = "carlos.mendez@vinoteca.com", Contrasena = "Carlos_123*", Rol = RolesSistema.Supervisor, Activo = true },
				new Usuario { Id = Guid.NewGuid().ToString(), Nombre = "Laura Perez", Correo = "laura.perez@vinoteca.com", Contrasena = "Laura_123*", Rol = RolesSistema.Cliente, Activo = true },
				new Usuario { Id = Guid.NewGuid().ToString(), Nombre = "Diego Ruiz", Correo = "diego.ruiz@vinoteca.com", Contrasena = "Diego_123*", Rol = RolesSistema.Cliente, Activo = true },
				new Usuario { Id = Guid.NewGuid().ToString(), Nombre = "Sofia Vargas", Correo = "sofia.vargas@vinoteca.com", Contrasena = "Sofia_123*", Rol = RolesSistema.Cliente, Activo = true },
				new Usuario { Id = Guid.NewGuid().ToString(), Nombre = "Miguel Torres", Correo = "miguel.torres@vinoteca.com", Contrasena = "Miguel_123*", Rol = RolesSistema.Cliente, Activo = true },
				new Usuario { Id = Guid.NewGuid().ToString(), Nombre = "Valeria Castro", Correo = "valeria.castro@vinoteca.com", Contrasena = "Valeria_123*", Rol = RolesSistema.Cliente, Activo = true },
				new Usuario { Id = Guid.NewGuid().ToString(), Nombre = "Javier Moreno", Correo = "javier.moreno@vinoteca.com", Contrasena = "Javier_123*", Rol = RolesSistema.Cliente, Activo = true },
				new Usuario { Id = Guid.NewGuid().ToString(), Nombre = "Fernanda Gil", Correo = "fernanda.gil@vinoteca.com", Contrasena = "Fernanda_123*", Rol = RolesSistema.Cliente, Activo = true },
				new Usuario { Id = Guid.NewGuid().ToString(), Nombre = "Ricardo Salas", Correo = "ricardo.salas@vinoteca.com", Contrasena = "Ricardo_123*", Rol = RolesSistema.Cliente, Activo = true },
				new Usuario { Id = Guid.NewGuid().ToString(), Nombre = "Daniela Ortiz", Correo = "daniela.ortiz@vinoteca.com", Contrasena = "Daniela_123*", Rol = RolesSistema.Cliente, Activo = true },
				new Usuario { Id = Guid.NewGuid().ToString(), Nombre = "Eduardo Rios", Correo = "eduardo.rios@vinoteca.com", Contrasena = "Eduardo_123*", Rol = RolesSistema.Cliente, Activo = true },
				new Usuario { Id = Guid.NewGuid().ToString(), Nombre = "Mariana Vega", Correo = "mariana.vega@vinoteca.com", Contrasena = "Mariana_123*", Rol = RolesSistema.Cliente, Activo = true },
				new Usuario { Id = Guid.NewGuid().ToString(), Nombre = "Patricia Leon", Correo = "patricia.leon@vinoteca.com", Contrasena = "Patricia_123*", Rol = RolesSistema.Cliente, Activo = true },
				new Usuario { Id = Guid.NewGuid().ToString(), Nombre = "Hector Nava", Correo = "hector.nava@vinoteca.com", Contrasena = "Hector_123*", Rol = RolesSistema.Cliente, Activo = true }
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

		public static List<Venta> ObtenerVentasPorUsuario(string usuarioId)
		{
			return ObtenerVentas()
				.Where(v => v.UsuarioId == usuarioId)
				.OrderByDescending(v => v.Fecha)
				.ToList();
		}

		public static void GuardarVenta(Venta nuevaVenta)
		{
			if (!SessionService.PuedeComprar)
			{
				throw new InvalidOperationException("Solo clientes pueden registrar compras");
			}

			var ventas = ObtenerVentas();
			ventas.Add(nuevaVenta);
			GuardarJson(ventasFile, ventas);
		}
	}
}
