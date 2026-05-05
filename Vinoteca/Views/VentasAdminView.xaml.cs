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
			txtTitulo.Text = SessionService.EsAdminActivo ? "Panel de venta" : "Confirmacion de compra";
			txtSubtitulo.Text = $"{CarritoService.ObtenerCantidadTotalArticulos()} articulo(s) listos para procesar";
			txtVacio.Visibility = carrito.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		private void btnVaciar_Click(object sender, RoutedEventArgs e)
		{
			CarritoService.LimpiarCarrito();
		}

		private void btnFinalizarVenta_Click(object sender, RoutedEventArgs e)
		{
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

			var nuevaVenta = new Venta
			{
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

			if (SessionService.EsAdminActivo)
			{
				Frame.Navigate(typeof(ReportesView));
				return;
			}

			Frame.Navigate(typeof(TiendaView));
		}

		private void MostrarEstado(string mensaje)
		{
			txtEstado.Text = mensaje;
			txtEstado.Visibility = Visibility.Visible;
		}
	}
}
