using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vinoteca.Models;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class CarritoView : Page
	{
		public ObservableCollection<CarritoItem> ItemsCarrito { get; } = new();

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

		private void CarritoView_Unloaded(object sender, RoutedEventArgs e)
		{
			CarritoService.CarritoActualizado -= CargarCarrito;
			Unloaded -= CarritoView_Unloaded;
		}

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

		private void MostrarEstado(string mensaje)
		{
			txtEstado.Text = mensaje;
			txtEstado.Visibility = Visibility.Visible;
		}
	}
}
