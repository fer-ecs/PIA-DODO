using System;
using System.IO;
using System.Linq;

namespace Vinoteca.Services
{
	// esta seccion sirve para agrupar la parte del sistema y dejar esa responsabilidad en un solo archivo - ImageAssetService
	public static class ImageAssetService
	{
		private const string AssetsFolder = "Assets";
		private const string ProductImagesFolder = "productos";

		// esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - CopiarImagenAProyecto
		public static string CopiarImagenAProyecto(string rutaOrigen)
		{
			if (string.IsNullOrWhiteSpace(rutaOrigen) || !File.Exists(rutaOrigen))
			{
				return string.Empty;
			}

			string carpetaDestino = ObtenerCarpetaImagenesProducto();
			Directory.CreateDirectory(carpetaDestino);

			string extension = Path.GetExtension(rutaOrigen).ToLowerInvariant();
			string nombreBase = LimpiarNombreArchivo(Path.GetFileNameWithoutExtension(rutaOrigen));
			if (string.IsNullOrWhiteSpace(nombreBase))
			{
				nombreBase = "producto";
			}

			string nombreArchivo = $"{nombreBase}-{DateTime.Now:yyyyMMddHHmmssfff}{extension}";
			string destino = Path.Combine(carpetaDestino, nombreArchivo);
			File.Copy(rutaOrigen, destino, overwrite: false);

			return Path.Combine(AssetsFolder, ProductImagesFolder, nombreArchivo);
		}

		// esta seccion sirve para revisar reglas de la parte del sistema y evitar que pase un dato incorrecto - EsImagenDelProyectoValida
		public static bool EsImagenDelProyectoValida(string ruta)
		{
			string? rutaCompleta = ResolverRutaAbsoluta(ruta);
			return rutaCompleta != null &&
				File.Exists(rutaCompleta) &&
				EstaDentroDeCarpeta(rutaCompleta, ObtenerCarpetaImagenesProducto());
		}

		// esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - ResolverRutaAbsoluta
		public static string? ResolverRutaAbsoluta(string? ruta)
		{
			if (string.IsNullOrWhiteSpace(ruta))
			{
				return null;
			}

			if (Path.IsPathRooted(ruta))
			{
				return File.Exists(ruta) ? ruta : null;
			}

			string rutaProyecto = Path.Combine(ObtenerRaizProyecto(), ruta);
			return File.Exists(rutaProyecto) ? rutaProyecto : null;
		}

		// esta seccion sirve para leer informacion de la parte del sistema y regresarla lista para usarse - ObtenerCarpetaImagenesProducto
		private static string ObtenerCarpetaImagenesProducto()
		{
			return Path.Combine(ObtenerRaizProyecto(), AssetsFolder, ProductImagesFolder);
		}

		// esta seccion sirve para leer informacion de la parte del sistema y regresarla lista para usarse - ObtenerRaizProyecto
		private static string ObtenerRaizProyecto()
		{
			var directorio = new DirectoryInfo(AppContext.BaseDirectory);
			while (directorio != null)
			{
				if (File.Exists(Path.Combine(directorio.FullName, "Vinoteca.csproj")))
				{
					return directorio.FullName;
				}

				directorio = directorio.Parent;
			}

			return Directory.GetCurrentDirectory();
		}

		// esta seccion sirve para revisar reglas de la parte del sistema y evitar que pase un dato incorrecto - EstaDentroDeCarpeta
		private static bool EstaDentroDeCarpeta(string archivo, string carpeta)
		{
			string archivoCompleto = Path.GetFullPath(archivo);
			string carpetaCompleta = Path.GetFullPath(carpeta).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
			return archivoCompleto.StartsWith(carpetaCompleta, StringComparison.OrdinalIgnoreCase);
		}

		// esta seccion sirve para quitar informacion de la parte del sistema y dejar el estado consistente - LimpiarNombreArchivo
		private static string LimpiarNombreArchivo(string nombre)
		{
			var invalidos = Path.GetInvalidFileNameChars();
			string limpio = new string(nombre
				.Select(c => invalidos.Contains(c) ? '-' : c)
				.ToArray());

			return limpio.Trim(' ', '.', '-');
		}
	}
}
