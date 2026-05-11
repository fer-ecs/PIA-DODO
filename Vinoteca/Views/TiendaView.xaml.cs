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

			if (!SessionService.PuedeComprar)
			{
				txtEstado.Text = "Solo empleados pueden operar el punto de venta";
				txtEstado.Visibility = Visibility.Visible;
				txtBuscar.IsEnabled = false;
				txtCodigoEscaneo.IsEnabled = false;
				gvTienda.IsEnabled = false;
				lvCarritoRapido.IsEnabled = false;
				return;
			}

			InputRestrictionsHelper.AplicarSinEspaciosNiEnter(this);
			InputRestrictionsHelper.AplicarTextoLibreSinEnter(txtBuscar);
			InputRestrictionsHelper.AplicarSinEspacios(txtCodigoEscaneo);
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
				(p.Id?.ToLowerInvariant().Contains(busqueda) ?? false) ||
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
			if (!SessionService.PuedeComprar)
			{
				txtEstado.Text = "Solo empleados pueden agregar productos a la venta";
				txtEstado.Visibility = Visibility.Visible;
				return;
			}

			if (sender is not Button btn || btn.Tag is not Producto producto)
			{
				return;
			}

			AgregarProducto(producto);
		}

		private void btnEscanear_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeComprar)
			{
				txtEstado.Text = "Solo empleados pueden escanear productos";
				txtEstado.Visibility = Visibility.Visible;
				return;
			}

			string codigo = txtCodigoEscaneo.Text?.Trim().ToLowerInvariant() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(codigo))
			{
				txtEstado.Text = "Captura el ID corto del producto para simular el escaneo";
				txtEstado.Visibility = Visibility.Visible;
				return;
			}

			var coincidencias = todosLosProductos.Where(p =>
				(!string.IsNullOrWhiteSpace(p.CodigoCorto) && p.CodigoCorto.ToLowerInvariant() == codigo) ||
				(!string.IsNullOrWhiteSpace(p.Id) && p.Id.ToLowerInvariant().Replace("-", string.Empty).StartsWith(codigo)))
				.ToList();

			if (coincidencias.Count == 0)
			{
				txtEstado.Text = "No se encontro un producto activo con ese ID";
				txtEstado.Visibility = Visibility.Visible;
				return;
			}

			if (coincidencias.Count > 1)
			{
				txtEstado.Text = "El ID capturado coincide con varios productos. Escribe mas caracteres";
				txtEstado.Visibility = Visibility.Visible;
				return;
			}

			var producto = coincidencias[0];
			AgregarProducto(producto);
			txtCodigoEscaneo.Text = string.Empty;
			txtCodigoEscaneo.Focus(FocusState.Programmatic);
		}

		private void AgregarProducto(Producto producto)
		{
			if (CarritoService.AgregarAlCarrito(producto, out string mensaje))
			{
				txtEstado.Text = $"{producto.Nombre} agregado a la venta";
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
			if (!SessionService.PuedeComprar)
			{
				txtEstado.Text = "Solo empleados pueden cobrar ventas";
				txtEstado.Visibility = Visibility.Visible;
				return;
			}

			if (CarritoService.ObtenerCarrito().Count == 0)
			{
				txtEstado.Text = "Agrega al menos un producto a la venta";
				txtEstado.Visibility = Visibility.Visible;
				return;
			}

			Frame.Navigate(typeof(CarritoView));
		}
	}
}
