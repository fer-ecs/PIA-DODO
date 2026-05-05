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
		private const string adminCorreo = "admin@vinoteca.com";
		private const string adminContrasena = "Admin_123*";

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
					var usuariosBase = new List<Usuario>
					{
						new Usuario
						{
							Id = Guid.NewGuid().ToString(),
							Nombre = "Administrador",
							Correo = adminCorreo,
							Contrasena = adminContrasena,
							EsAdmin = true,
							Activo = true
						}
					};
					GuardarJson(usuariosFile, usuariosBase);
				}
				else
				{
					ActualizarUsuarioAdmin();
				}

				if (!File.Exists(productosFile))
				{
					GuardarJson(productosFile, new List<Producto>());
				}

				if (!File.Exists(ventasFile))
				{
					GuardarJson(ventasFile, new List<Venta>());
				}
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

		private static void ActualizarUsuarioAdmin()
		{
			var usuarios = ObtenerUsuariosSinInicializar();
			var admin = usuarios.FirstOrDefault(u =>
				!string.IsNullOrWhiteSpace(u.Correo) &&
				u.Correo.Equals(adminCorreo, StringComparison.OrdinalIgnoreCase));

			if (admin == null)
			{
				return;
			}

			admin.Contrasena = adminContrasena;
			GuardarJson(usuariosFile, usuarios);
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

			usuarios[index] = usuario;
			GuardarJson(usuariosFile, usuarios);
		}

		public static int ContarAdministradoresActivos()
		{
			return ObtenerUsuarios().Count(u => u.EsAdmin && u.Activo);
		}

		public static List<Producto> ObtenerProductos()
		{
			InicializarArchivos();
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

		public static void GuardarVenta(Venta nuevaVenta)
		{
			var ventas = ObtenerVentas();
			ventas.Add(nuevaVenta);
			GuardarJson(ventasFile, ventas);
		}
	}
}
