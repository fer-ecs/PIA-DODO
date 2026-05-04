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
		// Definición de rutas de archivos organizadas en la carpeta Data
		private static readonly string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
		private static readonly string usuariosFile = Path.Combine(dataFolder, "usuarios.json");
		private static readonly string productosFile = Path.Combine(dataFolder, "productos.json");
		private static readonly string ventasFile = Path.Combine(dataFolder, "ventas.json");

		/// <summary>
		/// Crea la carpeta Data y los archivos JSON iniciales si no existen.
		/// </summary>
		public static void InicializarArchivos()
		{
			try
			{
				if (!Directory.Exists(dataFolder))
				{
					Directory.CreateDirectory(dataFolder);
				}

				// Inicializar Usuarios
				if (!File.Exists(usuariosFile))
				{
					var usuariosBase = new List<Usuario>
					{
						new Usuario
						{
							Id = Guid.NewGuid().ToString(),
							Nombre = "Administrador",
							Correo = "admin@vinoteca.com",
							Contrasena = "admin123",
							EsAdmin = true,
							Activo = true
						}
					};
					GuardarJson(usuariosFile, usuariosBase);
				}

				// Inicializar Productos
				if (!File.Exists(productosFile))
				{
					GuardarJson(productosFile, new List<Producto>());
				}

				// Inicializar Ventas
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

		// --- MÉTODOS AUXILIARES (Para evitar repetir código de JSON) ---
		private static void GuardarJson<T>(string ruta, T objeto)
		{
			string json = JsonSerializer.Serialize(objeto, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(ruta, json);
		}

		// --- MÉTODOS PARA USUARIOS ---
		public static List<Usuario> ObtenerUsuarios()
		{
			InicializarArchivos();
			try
			{
				string json = File.ReadAllText(usuariosFile);
				return JsonSerializer.Deserialize<List<Usuario>>(json) ?? new List<Usuario>();
			}
			catch { return new List<Usuario>(); }
		}

		// --- MÉTODOS PARA PRODUCTOS ---
		public static List<Producto> ObtenerProductos()
		{
			InicializarArchivos();
			try
			{
				string json = File.ReadAllText(productosFile);
				return JsonSerializer.Deserialize<List<Producto>>(json) ?? new List<Producto>();
			}
			catch { return new List<Producto>(); }
		}

		public static void GuardarProducto(Producto producto)
		{
			var productos = ObtenerProductos();
			var index = productos.FindIndex(p => p.Id == producto.Id);

			if (index >= 0) productos[index] = producto;
			else productos.Add(producto);

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

		// --- MÉTODOS PARA VENTAS ---
		public static List<Venta> ObtenerVentas()
		{
			InicializarArchivos();
			if (!File.Exists(ventasFile)) return new List<Venta>();
			try
			{
				string json = File.ReadAllText(ventasFile);
				return JsonSerializer.Deserialize<List<Venta>>(json) ?? new List<Venta>();
			}
			catch { return new List<Venta>(); }
		}

		public static void GuardarVenta(Venta nuevaVenta)
		{
			var ventas = ObtenerVentas();
			ventas.Add(nuevaVenta);
			GuardarJson(ventasFile, ventas);
		}
	}
}