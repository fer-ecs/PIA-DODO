using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vinoteca.Models;

namespace Vinoteca.Services
{
	// esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - ICambiosPendientes
	public interface ICambiosPendientes
	{
		bool TieneCambiosPendientes { get; }
		string ObtenerMensajeCambiosPendientes();
	}

	public interface IDescartaCambiosPendientes
	{
		void DescartarCambiosPendientes();
	}

	// esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - IVentaTemporal
	public interface IVentaTemporal
	{
		bool TieneVentaTemporal { get; }
		VentaBorrador CrearVentaBorrador();
	}

	// esta seccion sirve para agrupar la parte del sistema y dejar esa responsabilidad en un solo archivo - CambiosPendientesService
	public static class CambiosPendientesService
	{
		// esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - ConfirmarAccionSiHayCambiosAsync
		public static async Task<bool> ConfirmarAccionSiHayCambiosAsync(
			XamlRoot? xamlRoot,
			DependencyObject? contexto,
			string accion,
			bool incluirCarrito = true)
		{
			if (await ResolverVentaTemporalAsync(xamlRoot, contexto, incluirCarrito))
			{
				return true;
			}

			if (HayVentaTemporal(contexto, incluirCarrito))
			{
				return false;
			}

			string? mensaje = ObtenerMensajeCambios(contexto, incluirCarrito);
			if (string.IsNullOrWhiteSpace(mensaje))
			{
				return true;
			}

			bool confirmar = await MostrarConfirmacionAsync(
				xamlRoot,
				"Cambios pendientes",
				$"{mensaje} Si deseas {accion}, se perdera la informacion no guardada. Deseas continuar?",
				"Continuar");
			if (confirmar)
			{
				DescartarCambios(contexto);
			}

			return confirmar;
		}

		// esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - ConfirmarSalidaAsync
		public static async Task<bool> ConfirmarSalidaAsync(
			XamlRoot? xamlRoot,
			DependencyObject? contexto,
			string accion,
			bool incluirCarrito = true)
		{
			if (await ResolverVentaTemporalAsync(xamlRoot, contexto, incluirCarrito))
			{
				return await MostrarConfirmacionAsync(
					xamlRoot,
					"Confirmar",
					$"Deseas {accion}?",
					"Continuar");
			}

			if (HayVentaTemporal(contexto, incluirCarrito))
			{
				return false;
			}

			string? mensaje = ObtenerMensajeCambios(contexto, incluirCarrito);
			if (string.IsNullOrWhiteSpace(mensaje))
			{
				return await MostrarConfirmacionAsync(
					xamlRoot,
					"Confirmar",
					$"Deseas {accion}?",
					"Continuar");
			}

			bool confirmar = await MostrarConfirmacionAsync(
				xamlRoot,
				"Cambios pendientes",
				$"{mensaje} Si deseas {accion}, se perdera la informacion no guardada. Deseas continuar?",
				"Continuar");
			if (confirmar)
			{
				DescartarCambios(contexto);
			}

			return confirmar;
		}

		// esta seccion sirve para mostrar mensajes o ventanas de la parte del sistema para que el usuario entienda el estado - MostrarConfirmacionAsync
		public static async Task<bool> MostrarConfirmacionAsync(
			XamlRoot? xamlRoot,
			string titulo,
			string mensaje,
			string textoAceptar,
			string textoCancelar = "Cancelar")
		{
			if (xamlRoot == null)
			{
				return false;
			}

			var dialog = new ContentDialog
			{
				Title = titulo,
				Content = mensaje,
				PrimaryButtonText = textoAceptar,
				CloseButtonText = textoCancelar,
				DefaultButton = ContentDialogButton.Close,
				XamlRoot = xamlRoot
			};

			return await dialog.ShowAsync() == ContentDialogResult.Primary;
		}

		// esta seccion sirve para leer informacion de la parte del sistema y regresarla lista para usarse - ObtenerMensajeCambios
		public static string? ObtenerMensajeCambios(DependencyObject? contexto, bool incluirCarrito = true)
		{
			var fuente = BuscarFuenteCambios(contexto);
			if (fuente != null)
			{
				return fuente.ObtenerMensajeCambiosPendientes();
			}

			if (incluirCarrito && CarritoService.ObtenerCarrito().Count > 0)
			{
				return "Hay productos agregados a la venta";
			}

			return null;
		}

		// esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - HayVentaTemporal
		private static bool HayVentaTemporal(DependencyObject? contexto, bool incluirCarrito)
		{
			var ventaTemporal = BuscarFuenteVentaTemporal(contexto);
			return (ventaTemporal?.TieneVentaTemporal == true) ||
				(incluirCarrito && CarritoService.ObtenerCarrito().Count > 0);
		}

		// esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - ResolverVentaTemporalAsync
		private static async Task<bool> ResolverVentaTemporalAsync(
			XamlRoot? xamlRoot,
			DependencyObject? contexto,
			bool incluirCarrito)
		{
			if (xamlRoot == null || !HayVentaTemporal(contexto, incluirCarrito))
			{
				return false;
			}

			var dialog = new ContentDialog
			{
				Title = "Venta pendiente",
				Content = "Hay productos agregados a una venta. Puedes guardarla como borrador para continuar despues o descartarla.",
				PrimaryButtonText = "Guardar borrador",
				SecondaryButtonText = "Descartar",
				CloseButtonText = "Cancelar",
				DefaultButton = ContentDialogButton.Close,
				XamlRoot = xamlRoot
			};

			var resultado = await dialog.ShowAsync();
			if (resultado == ContentDialogResult.None)
			{
				return false;
			}

			string usuarioId = SessionService.UsuarioActivo?.Id ?? string.Empty;
			if (resultado == ContentDialogResult.Primary)
			{
				var fuente = BuscarFuenteVentaTemporal(contexto);
				var borrador = fuente?.CrearVentaBorrador() ?? CrearBorradorBasico(usuarioId);
				DataService.GuardarVentaBorrador(borrador);
			}
			else if (!string.IsNullOrWhiteSpace(usuarioId))
			{
				DataService.EliminarVentaBorrador(usuarioId);
			}

			CarritoService.LimpiarCarrito();
			return true;
		}

		// esta seccion sirve para armar datos o contenido de la parte del sistema y devolverlo ya preparado - CrearBorradorBasico
		private static VentaBorrador CrearBorradorBasico(string usuarioId)
		{
			return new VentaBorrador
			{
				UsuarioId = usuarioId,
				Productos = CarritoService.ObtenerCarrito().ToList()
			};
		}

		// esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - BuscarFuenteCambios
		private static ICambiosPendientes? BuscarFuenteCambios(DependencyObject? raiz)
		{
			if (raiz == null)
			{
				return null;
			}

			if (raiz is Frame frame && frame.Content is DependencyObject contenidoFrame)
			{
				var encontradoEnFrame = BuscarFuenteCambios(contenidoFrame);
				if (encontradoEnFrame != null)
				{
					return encontradoEnFrame;
				}
			}

			if (raiz is ICambiosPendientes fuente && fuente.TieneCambiosPendientes)
			{
				return fuente;
			}

			int totalHijos = VisualTreeHelper.GetChildrenCount(raiz);
			for (int i = 0; i < totalHijos; i++)
			{
				var encontrado = BuscarFuenteCambios(VisualTreeHelper.GetChild(raiz, i));
				if (encontrado != null)
				{
					return encontrado;
				}
			}

			return null;
		}

		private static void DescartarCambios(DependencyObject? contexto)
		{
			if (BuscarFuenteCambios(contexto) is IDescartaCambiosPendientes fuente)
			{
				fuente.DescartarCambiosPendientes();
			}
		}

		// esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - BuscarFuenteVentaTemporal
		private static IVentaTemporal? BuscarFuenteVentaTemporal(DependencyObject? raiz)
		{
			if (raiz == null)
			{
				return null;
			}

			if (raiz is Frame frame && frame.Content is DependencyObject contenidoFrame)
			{
				var encontradoEnFrame = BuscarFuenteVentaTemporal(contenidoFrame);
				if (encontradoEnFrame != null)
				{
					return encontradoEnFrame;
				}
			}

			if (raiz is IVentaTemporal fuente && fuente.TieneVentaTemporal)
			{
				return fuente;
			}

			int totalHijos = VisualTreeHelper.GetChildrenCount(raiz);
			for (int i = 0; i < totalHijos; i++)
			{
				var encontrado = BuscarFuenteVentaTemporal(VisualTreeHelper.GetChild(raiz, i));
				if (encontrado != null)
				{
					return encontrado;
				}
			}

			return null;
		}
	}
}
