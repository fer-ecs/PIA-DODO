using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Vinoteca.Helpers;
using Vinoteca.Models;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class TiendaView : Page
	{
		public ObservableCollection<Producto> ProductosCatalogo { get; } = new();
		private List<Producto> todosLosProductos = new();

		public TiendaView()
		{
			InitializeComponent();
			InputRestrictionsHelper.AplicarSinEspaciosNiEnter(this);
			CargarCatalogo();
			RefrescarCarritoVisual();
			CarritoService.CarritoActualizado += RefrescarCarritoVisual;
			Unloaded += TiendaView_Unloaded;
		}

		private void TiendaView_Unloaded(object sender, RoutedEventArgs e)
		{
			CarritoService.CarritoActualizado -= RefrescarCarritoVisual;
			Unloaded -= TiendaView_Unloaded;
		}

		private void CargarCatalogo()
		{
			todosLosProductos = DataService.ObtenerProductos()
				.Where(p => p.Stock > 0 && p.Activo)
				.OrderBy(p => p.Nombre)
				.ToList();

			AplicarFiltro();
		}

		private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
		{
			AplicarFiltro();
		}

		private void AplicarFiltro()
		{
			string busqueda = txtBuscar.Text?.Trim().ToLowerInvariant() ?? string.Empty;
			var filtrados = todosLosProductos.Where(p =>
				string.IsNullOrEmpty(busqueda) ||
				(p.Nombre?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(p.Marca?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(p.Categoria?.ToLowerInvariant().Contains(busqueda) ?? false))
				.ToList();

			ProductosCatalogo.Clear();
			foreach (var producto in filtrados)
			{
				ProductosCatalogo.Add(producto);
			}

			gvTienda.ItemsSource = ProductosCatalogo;
		}

		private void btnAgregar_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button btn || btn.Tag is not Producto producto)
			{
				return;
			}

			if (CarritoService.AgregarAlCarrito(producto, out string mensaje))
			{
				txtEstado.Text = $"{producto.Nombre} agregado al carrito";
				txtEstado.Visibility = Visibility.Visible;
				CargarCatalogo();
				return;
			}

			txtEstado.Text = mensaje;
			txtEstado.Visibility = Visibility.Visible;
		}

		private void RefrescarCarritoVisual()
		{
			var items = CarritoService.ObtenerCarrito();
			lvCarritoRapido.ItemsSource = new ObservableCollection<CarritoItem>(items);
			txtTotalRapido.Text = CarritoService.ObtenerTotal().ToString("C");
			txtCantidadRapida.Text = $"{CarritoService.ObtenerCantidadTotalArticulos()} articulo(s)";
		}

		private void btnIrAPagar_Click(object sender, RoutedEventArgs e)
		{
			if (CarritoService.ObtenerCarrito().Count == 0)
			{
				txtEstado.Text = "Agrega al menos un producto al carrito";
				txtEstado.Visibility = Visibility.Visible;
				return;
			}

			Frame.Navigate(typeof(CarritoView));
		}
	}
}
