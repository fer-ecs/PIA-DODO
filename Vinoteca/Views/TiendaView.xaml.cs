using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Vinoteca.Models;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class TiendaView : Page
	{
		public ObservableCollection<Producto> ProductosCatalogo { get; set; }
		private List<Producto> todosLosProductos;

		public TiendaView()
		{
			this.InitializeComponent();
			CargarCatalogo();
			RefrescarCarritoVisual(); // Para que el carrito aparezca si ya hay algo guardado
		}

		private void CargarCatalogo()
		{
			todosLosProductos = DataService.ObtenerProductos()
				.Where(p => p.Stock > 0 && p.Activo).ToList();

			ProductosCatalogo = new ObservableCollection<Producto>(todosLosProductos);
			gvTienda.ItemsSource = ProductosCatalogo;
		}

		private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
		{
			var busqueda = txtBuscar.Text.ToLower();
			var filtrados = todosLosProductos.Where(p =>
				p.Nombre.ToLower().Contains(busqueda) ||
				p.Marca.ToLower().Contains(busqueda)).ToList();

			ProductosCatalogo.Clear();
			foreach (var p in filtrados) ProductosCatalogo.Add(p);
		}

		private void btnAgregar_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button btn && btn.Tag is Producto producto)
			{
				// 1. Mandar al servicio
				CarritoService.AgregarAlCarrito(producto);

				// 2. Refrescar la columna derecha
				RefrescarCarritoVisual();

				// 3. Debug en consola
				System.Diagnostics.Debug.WriteLine($"✅ Agregado: {producto.Nombre} | Total: {CarritoService.ObtenerTotal():C}");
			}
		}

		private void RefrescarCarritoVisual()
		{
			var items = CarritoService.ObtenerCarrito();

			// Forzamos el refresco de la lista
			lvCarritoRapido.ItemsSource = null;
			lvCarritoRapido.ItemsSource = new ObservableCollection<CarritoItem>(items);

			// Actualizamos la etiqueta de total
			txtTotalRapido.Text = CarritoService.ObtenerTotal().ToString("C");
		}

		private void btnIrAPagar_Click(object sender, RoutedEventArgs e)
		{
			if (CarritoService.ObtenerCarrito().Count > 0)
			{
				// Navegamos a la vista de ventas del Admin
				this.Frame.Navigate(typeof(VentasAdminView));
			}
		}

		private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine($"Error de imagen: {e.ErrorMessage}");
		}
	}
}