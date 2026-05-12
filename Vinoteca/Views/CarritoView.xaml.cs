using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vinoteca.Models;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	// esta seccion sirve para agrupar la pantalla del carrito y dejar esa responsabilidad en un solo archivo - CarritoView
	public sealed partial class CarritoView : Page
	{
		public ObservableCollection<CarritoItem> ItemsCarrito { get; } = new();

		// esta seccion sirve para agrupar la pantalla del carrito y dejar esa responsabilidad en un solo archivo - CarritoView
		public CarritoView()
		{
			InitializeComponent();

			if (!SessionService.PuedeComprar)
			{
				txtResumen.Text = "Solo empleados pueden consultar la venta";
				txtEstado.Text = "Acceso restringido para caja";
				txtEstado.Visibility = Visibility.Visible;
				lvCarrito.IsEnabled = false;
				return;
			}

			CargarCarrito();
			CarritoService.CarritoActualizado += CargarCarrito;
			Unloaded += CarritoView_Unloaded;
		}

		// esta seccion sirve para responder a la accion del usuario en la pantalla del carrito y mover el flujo al siguiente paso - CarritoView_Unloaded
		private void CarritoView_Unloaded(object sender, RoutedEventArgs e)
		{
			CarritoService.CarritoActualizado -= CargarCarrito;
			Unloaded -= CarritoView_Unloaded;
		}

		// esta seccion sirve para cargar informacion de la pantalla del carrito y preparar lo que se muestra en pantalla - CargarCarrito
		private void CargarCarrito()
		{
			var items = CarritoService.ObtenerCarrito().OrderBy(i => i.Producto.Nombre).ToList();
			ItemsCarrito.Clear();
			foreach (var item in items)
			{
				ItemsCarrito.Add(item);
			}

			lvCarrito.ItemsSource = ItemsCarrito;
			txtTotal.Text = CarritoService.ObtenerTotal().ToString("C");
			txtResumen.Text = $"{CarritoService.ObtenerCantidadTotalArticulos()} articulo(s) en la venta";
			txtEstado.Visibility = Visibility.Collapsed;
			txtVacio.Visibility = ItemsCarrito.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		// esta seccion sirve para responder a la accion del usuario en la pantalla del carrito y mover el flujo al siguiente paso - btnAumentar_Click
		private void btnAumentar_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeComprar)
			{
				MostrarEstado("Solo empleados pueden modificar la venta");
				return;
			}

			if (sender is not Button button || button.Tag is not CarritoItem item)
			{
				return;
			}

			CarritoService.CambiarCantidad(item.Producto.Id, item.Cantidad + 1, out string mensaje);
			if (!string.IsNullOrWhiteSpace(mensaje))
			{
				MostrarEstado(mensaje);
			}
		}

		// esta seccion sirve para responder a la accion del usuario en la pantalla del carrito y mover el flujo al siguiente paso - btnDisminuir_Click
		private async void btnDisminuir_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeComprar)
			{
				MostrarEstado("Solo empleados pueden modificar la venta");
				return;
			}

			if (sender is not Button button || button.Tag is not CarritoItem item)
			{
				return;
			}

			if (item.Cantidad == 1)
			{
				bool confirmar = await CambiosPendientesService.MostrarConfirmacionAsync(
					XamlRoot,
					"Quitar producto",
					$"Deseas quitar {item.Producto.Nombre} de la venta?",
					"Quitar");
				if (!confirmar)
				{
					return;
				}
			}

			CarritoService.CambiarCantidad(item.Producto.Id, item.Cantidad - 1, out _);
		}

		// esta seccion sirve para responder a la accion del usuario en la pantalla del carrito y mover el flujo al siguiente paso - btnEliminar_Click
		private async void btnEliminar_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeComprar)
			{
				MostrarEstado("Solo empleados pueden modificar la venta");
				return;
			}

			if (sender is not Button button || button.Tag is not CarritoItem item)
			{
				return;
			}

			bool confirmar = await CambiosPendientesService.MostrarConfirmacionAsync(
				XamlRoot,
				"Quitar producto",
				$"Deseas quitar {item.Producto.Nombre} de la venta?",
				"Quitar");
			if (!confirmar)
			{
				return;
			}

			CarritoService.QuitarDelCarrito(item.Producto.Id);
		}

		// esta seccion sirve para responder a la accion del usuario en la pantalla del carrito y mover el flujo al siguiente paso - btnVaciar_Click
		private async void btnVaciar_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeComprar)
			{
				MostrarEstado("Solo empleados pueden vaciar la venta");
				return;
			}

			if (CarritoService.ObtenerCarrito().Count == 0)
			{
				return;
			}

			bool confirmarVaciado = await CambiosPendientesService.MostrarConfirmacionAsync(
				XamlRoot,
				"Vaciar venta",
				$"Deseas quitar los {CarritoService.ObtenerCantidadTotalArticulos()} articulo(s) de la venta?",
				"Vaciar");
			if (!confirmarVaciado)
			{
				return;
			}

			CarritoService.LimpiarCarrito();
		}

		// esta seccion sirve para responder a la accion del usuario en la pantalla del carrito y mover el flujo al siguiente paso - btnFinalizarCompra_Click
		private void btnFinalizarCompra_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeComprar)
			{
				MostrarEstado("Solo empleados pueden cobrar ventas");
				return;
			}

			if (CarritoService.ObtenerCarrito().Count == 0)
			{
				MostrarEstado("La venta esta vacia");
				return;
			}

			Frame.Navigate(typeof(VentasAdminView));
		}

		// esta seccion sirve para mostrar mensajes o ventanas de la pantalla del carrito para que el usuario entienda el estado - MostrarEstado
		private void MostrarEstado(string mensaje)
		{
			txtEstado.Text = mensaje;
			txtEstado.Visibility = Visibility.Visible;
		}
	}
}
