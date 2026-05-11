using System;
using System.IO;
using System.Linq;

namespace Vinoteca.Services
{
	public static class ImageAssetService
	{
		private const string AssetsFolder = "Assets";
		private const string ProductImagesFolder = "productos";

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

		public static bool EsImagenDelProyectoValida(string ruta)
		{
			string? rutaCompleta = ResolverRutaAbsoluta(ruta);
			return rutaCompleta != null &&
				File.Exists(rutaCompleta) &&
				EstaDentroDeCarpeta(rutaCompleta, ObtenerCarpetaImagenesProducto());
		}

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

		private static string ObtenerCarpetaImagenesProducto()
		{
			return Path.Combine(ObtenerRaizProyecto(), AssetsFolder, ProductImagesFolder);
		}

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

		private static bool EstaDentroDeCarpeta(string archivo, string carpeta)
		{
			string archivoCompleto = Path.GetFullPath(archivo);
			string carpetaCompleta = Path.GetFullPath(carpeta).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
			return archivoCompleto.StartsWith(carpetaCompleta, StringComparison.OrdinalIgnoreCase);
		}

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
