using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Vinoteca.Services
{
	public interface ICambiosPendientes
	{
		bool TieneCambiosPendientes { get; }
		string ObtenerMensajeCambiosPendientes();
	}

	public static class CambiosPendientesService
	{
		public static async Task<bool> ConfirmarAccionSiHayCambiosAsync(
			XamlRoot? xamlRoot,
			DependencyObject? contexto,
			string accion,
			bool incluirCarrito = true)
		{
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
	}
}
