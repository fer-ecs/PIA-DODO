using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vinoteca.Models;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class VentasAdminView : Page
	{
		public ObservableCollection<CarritoItem> ItemsCarrito { get; set; }

		public VentasAdminView()
		{
			this.InitializeComponent();
			ActualizarInterfaz();
		}

		private void ActualizarInterfaz()
		{
			var carrito = CarritoService.ObtenerCarrito();
			ItemsCarrito = new ObservableCollection<CarritoItem>(carrito);
			lvCarrito.ItemsSource = ItemsCarrito;
			txtTotalCarrito.Text = CarritoService.ObtenerTotal().ToString("C");
		}

		private void btnVaciar_Click(object sender, RoutedEventArgs e)
		{
			CarritoService.LimpiarCarrito();
			ActualizarInterfaz();
		}

		private void btnFinalizarVenta_Click(object sender, RoutedEventArgs e)
		{
			var items = CarritoService.ObtenerCarrito();
			if (items.Count == 0) return;

			// 1. Crear el registro de la venta para el historial
			var nuevaVenta = new Venta
			{
				Productos = new List<CarritoItem>(items),
				Total = CarritoService.ObtenerTotal()
			};

			// 2. Guardar en el historial (JSON)
			DataService.GuardarVenta(nuevaVenta);

			// 3. Restar stock real
			foreach (var item in items)
			{
				var producto = item.Producto;
				producto.Stock -= item.Cantidad;
				DataService.GuardarProducto(producto);
			}

			// 4. Limpiar y salir
			CarritoService.LimpiarCarrito();
			this.Frame.Navigate(typeof(TiendaView));
		}
	}
}