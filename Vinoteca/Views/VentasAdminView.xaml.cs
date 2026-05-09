using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vinoteca.Models;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class VentasAdminView : Page
	{
		public ObservableCollection<CarritoItem> ItemsCarrito { get; } = new();

		public VentasAdminView()
		{
			InitializeComponent();

			if (!SessionService.PuedeComprar)
			{
				txtTitulo.Text = "Compra restringida";
				txtSubtitulo.Text = "Solo clientes pueden confirmar compras";
				txtEstado.Text = "Admin y supervisor no pueden realizar acciones de cliente";
				txtEstado.Visibility = Visibility.Visible;
				lvCarrito.IsEnabled = false;
				return;
			}

			ActualizarInterfaz();
			CarritoService.CarritoActualizado += ActualizarInterfaz;
			Unloaded += VentasAdminView_Unloaded;
		}

		private void VentasAdminView_Unloaded(object sender, RoutedEventArgs e)
		{
			CarritoService.CarritoActualizado -= ActualizarInterfaz;
			Unloaded -= VentasAdminView_Unloaded;
		}

		private void ActualizarInterfaz()
		{
			var carrito = CarritoService.ObtenerCarrito();
			ItemsCarrito.Clear();
			foreach (var item in carrito)
			{
				ItemsCarrito.Add(item);
			}

			lvCarrito.ItemsSource = ItemsCarrito;
			txtTotalCarrito.Text = CarritoService.ObtenerTotal().ToString("C");
			txtEstado.Visibility = Visibility.Collapsed;
			txtTitulo.Text = "Confirmacion de compra";
			txtSubtitulo.Text = $"{CarritoService.ObtenerCantidadTotalArticulos()} articulo(s) listos para pagar";
			txtVacio.Visibility = carrito.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		private async void btnVaciar_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeComprar)
			{
				MostrarEstado("Solo clientes pueden vaciar una compra");
				return;
			}

			if (CarritoService.ObtenerCarrito().Count == 0)
			{
				return;
			}

			bool confirmarVaciado = await CambiosPendientesService.MostrarConfirmacionAsync(
				XamlRoot,
				"Vaciar venta",
				"Deseas quitar todos los productos de esta venta?",
				"Vaciar");
			if (!confirmarVaciado)
			{
				return;
			}

			CarritoService.LimpiarCarrito();
		}

		private async void btnFinalizarVenta_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeComprar)
			{
				MostrarEstado("Solo clientes pueden confirmar compras");
				return;
			}

			var items = CarritoService.ObtenerCarrito();
			if (items.Count == 0)
			{
				MostrarEstado("No hay productos para procesar");
				return;
			}

			foreach (var item in items)
			{
				var productoActual = DataService.ObtenerProductos().FirstOrDefault(p => p.Id == item.Producto.Id && p.Activo);
				if (productoActual == null)
				{
					MostrarEstado($"El producto {item.Producto.Nombre} ya no esta disponible");
					return;
				}

				if (item.Cantidad > productoActual.Stock)
				{
					MostrarEstado($"Stock insuficiente para {item.Producto.Nombre}");
					return;
				}
			}

			bool confirmarVenta = await CambiosPendientesService.MostrarConfirmacionAsync(
				XamlRoot,
				"Confirmar venta",
				"Deseas registrar la venta con los productos actuales?",
				"Confirmar");
			if (!confirmarVenta)
			{
				return;
			}

			var nuevaVenta = new Venta
			{
				UsuarioId = SessionService.UsuarioActivo?.Id ?? string.Empty,
				NombreCliente = SessionService.UsuarioActivo?.Nombre ?? "Cliente",
				CorreoCliente = SessionService.UsuarioActivo?.Correo ?? string.Empty,
				RolUsuario = SessionService.RolActivo,
				Productos = new List<CarritoItem>(items),
				Total = CarritoService.ObtenerTotal()
			};

			DataService.GuardarVenta(nuevaVenta);

			foreach (var item in items)
			{
				var productoActual = DataService.ObtenerProductos().First(p => p.Id == item.Producto.Id);
				productoActual.Stock -= item.Cantidad;
				if (productoActual.Stock < 0)
				{
					productoActual.Stock = 0;
				}

				DataService.GuardarProducto(productoActual);
			}

			CarritoService.LimpiarCarrito();

			Frame.Navigate(typeof(MisTicketsView));
		}

		private void MostrarEstado(string mensaje)
		{
			txtEstado.Text = mensaje;
			txtEstado.Visibility = Visibility.Visible;
		}
	}
}
