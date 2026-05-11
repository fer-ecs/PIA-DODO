using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vinoteca.Models;

namespace Vinoteca.Services
{
	public interface ICambiosPendientes
	{
		bool TieneCambiosPendientes { get; }
		string ObtenerMensajeCambiosPendientes();
	}

	public interface IVentaTemporal
	{
		bool TieneVentaTemporal { get; }
		VentaBorrador CrearVentaBorrador();
	}

	public static class CambiosPendientesService
	{
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

			return await MostrarConfirmacionAsync(
				xamlRoot,
				"Cambios pendientes",
				$"{mensaje} Si deseas {accion}, se perdera la informacion no guardada. Deseas continuar?",
				"Continuar");
		}

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

			return await MostrarConfirmacionAsync(
				xamlRoot,
				"Cambios pendientes",
				$"{mensaje} Si deseas {accion}, se perdera la informacion no guardada. Deseas continuar?",
				"Continuar");
		}

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

		private static bool HayVentaTemporal(DependencyObject? contexto, bool incluirCarrito)
		{
			var ventaTemporal = BuscarFuenteVentaTemporal(contexto);
			return (ventaTemporal?.TieneVentaTemporal == true) ||
				(incluirCarrito && CarritoService.ObtenerCarrito().Count > 0);
		}

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

		private static VentaBorrador CrearBorradorBasico(string usuarioId)
		{
			return new VentaBorrador
			{
				UsuarioId = usuarioId,
				Productos = CarritoService.ObtenerCarrito().ToList()
			};
		}

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
