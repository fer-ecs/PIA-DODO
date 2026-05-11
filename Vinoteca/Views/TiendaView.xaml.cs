using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
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
		public ObservableCollection<ProductoVentaViewModel> ProductosCatalogo { get; } = new();
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
			gvTienda.ItemsSource = ProductosCatalogo;
			ConfigurarFiltros();
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

			CargarCategorias();
			AplicarFiltro();
		}

		private void ConfigurarFiltros()
		{
			cmbFiltroStock.SelectedIndex = 0;
			cmbOrdenCatalogo.SelectedIndex = 0;
		}

		private void CargarCategorias()
		{
			string seleccionActual = cmbFiltroCategoria.SelectedItem?.ToString() ?? "Todas";
			cmbFiltroCategoria.SelectionChanged -= Filtros_Changed;
			cmbFiltroCategoria.Items.Clear();
			cmbFiltroCategoria.Items.Add("Todas");

			foreach (var categoria in todosLosProductos
				.Select(p => p.Categoria)
				.Where(c => !string.IsNullOrWhiteSpace(c))
				.Distinct()
				.OrderBy(c => c))
			{
				cmbFiltroCategoria.Items.Add(categoria);
			}

			cmbFiltroCategoria.SelectedItem = cmbFiltroCategoria.Items.Contains(seleccionActual)
				? seleccionActual
				: "Todas";
			cmbFiltroCategoria.SelectionChanged += Filtros_Changed;
		}

		private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
		{
			AplicarFiltro();
		}

		private void AplicarFiltro(bool conservarDesplazamiento = false)
		{
			double? desplazamientoActual = null;
			if (conservarDesplazamiento && BuscarScrollViewer(gvTienda) is ScrollViewer scrollActual)
			{
				desplazamientoActual = scrollActual.VerticalOffset;
			}

			string busqueda = txtBuscar.Text?.Trim().ToLowerInvariant() ?? string.Empty;
			string categoria = cmbFiltroCategoria.SelectedItem?.ToString() ?? "Todas";
			string stock = ObtenerContenidoCombo(cmbFiltroStock);
			string orden = ObtenerContenidoCombo(cmbOrdenCatalogo);

			var reservados = CarritoService.ObtenerCarrito()
				.GroupBy(c => c.Producto.Id)
				.ToDictionary(g => g.Key, g => g.Sum(c => c.Cantidad));

			IEnumerable<ProductoVentaViewModel> consulta = todosLosProductos
				.Select(p => new ProductoVentaViewModel(p, reservados.TryGetValue(p.Id, out int cantidadReservada) ? cantidadReservada : 0))
				.Where(p =>
				string.IsNullOrEmpty(busqueda) ||
				(p.Id?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(p.Nombre?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(p.Marca?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(p.Categoria?.ToLowerInvariant().Contains(busqueda) ?? false));

			if (!string.IsNullOrWhiteSpace(categoria) && categoria != "Todas")
			{
				consulta = consulta.Where(p => p.Categoria == categoria);
			}

			consulta = stock switch
			{
				"Stock bajo" => consulta.Where(p => p.StockDisponible < 5),
				"Mas de 10" => consulta.Where(p => p.StockDisponible > 10),
				_ => consulta
			};

			consulta = orden switch
			{
				"Precio menor" => consulta.OrderBy(p => p.PrecioVenta).ThenBy(p => p.Nombre),
				"Precio mayor" => consulta.OrderByDescending(p => p.PrecioVenta).ThenBy(p => p.Nombre),
				"Stock menor" => consulta.OrderBy(p => p.StockDisponible).ThenBy(p => p.Nombre),
				"ID" => consulta.OrderBy(p => ObtenerIdNumerico(p.Id)).ThenBy(p => p.Nombre),
				_ => consulta.OrderBy(p => p.Nombre)
			};

			var filtrados = consulta.ToList();

			ProductosCatalogo.Clear();
			foreach (var producto in filtrados)
			{
				ProductosCatalogo.Add(producto);
			}

			if (desplazamientoActual.HasValue)
			{
				DispatcherQueue.TryEnqueue(() =>
				{
					if (BuscarScrollViewer(gvTienda) is ScrollViewer scrollRestaurado)
					{
						scrollRestaurado.ChangeView(null, desplazamientoActual.Value, null, true);
					}
				});
			}

			txtConteoCatalogo.Text = $"{filtrados.Count} de {todosLosProductos.Count} disponibles";
			if (filtrados.Count == 0)
			{
				txtEstado.Text = "No hay productos con esos filtros";
				txtEstado.Visibility = Visibility.Visible;
			}
			else if (txtEstado.Text == "No hay productos con esos filtros")
			{
				txtEstado.Visibility = Visibility.Collapsed;
			}
		}

		private static string ObtenerContenidoCombo(ComboBox combo)
		{
			return (combo.SelectedItem as ComboBoxItem)?.Content?.ToString()
				?? combo.SelectedItem?.ToString()
				?? string.Empty;
		}

		private static int ObtenerIdNumerico(string? id)
		{
			return int.TryParse(id, out int valor) ? valor : int.MaxValue;
		}

		private static ScrollViewer? BuscarScrollViewer(DependencyObject? origen)
		{
			if (origen is null)
			{
				return null;
			}

			if (origen is ScrollViewer scrollViewer)
			{
				return scrollViewer;
			}

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(origen); i++)
			{
				var encontrado = BuscarScrollViewer(VisualTreeHelper.GetChild(origen, i));
				if (encontrado is not null)
				{
					return encontrado;
				}
			}

			return null;
		}

		private void Filtros_Changed(object sender, object e)
		{
			AplicarFiltro();
		}

		private void btnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
		{
			txtBuscar.Text = string.Empty;
			txtCodigoEscaneo.Text = string.Empty;
			cmbFiltroCategoria.SelectedItem = "Todas";
			cmbFiltroStock.SelectedIndex = 0;
			cmbOrdenCatalogo.SelectedIndex = 0;
			txtEstado.Visibility = Visibility.Collapsed;
			AplicarFiltro();
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
				txtEstado.Text = string.IsNullOrWhiteSpace(mensaje) ? $"{producto.Nombre} agregado a la venta" : mensaje;
				txtEstado.Visibility = Visibility.Visible;
				AplicarFiltro(true);
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
			if (todosLosProductos.Count > 0)
			{
				AplicarFiltro(true);
			}
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

	public sealed class ProductoVentaViewModel
	{
		public Producto Producto { get; }
		public string Id => Producto.Id ?? string.Empty;
		public string CodigoCorto => Producto.CodigoCorto;
		public string Nombre => Producto.Nombre ?? string.Empty;
		public string Marca => Producto.Marca ?? string.Empty;
		public string Categoria => Producto.Categoria ?? string.Empty;
		public double PrecioVenta => Producto.PrecioVenta;
		public string ImagenPath => Producto.ImagenPath ?? string.Empty;
		public int StockDisponible { get; }
		public bool PuedeAgregarse => StockDisponible > 0;
		public Visibility AlertaStockVisibility => StockDisponible < 5 ? Visibility.Visible : Visibility.Collapsed;
		public string StockTexto => StockDisponible <= 0 ? "Sin stock disponible" : $"Stock disponible: {StockDisponible}";
		public string AlertaStockTexto => StockDisponible <= 0
			? "No hay stock disponible"
			: $"Stock bajo: quedan {StockDisponible}";

		public ProductoVentaViewModel(Producto producto, int reservado)
		{
			Producto = producto;
			StockDisponible = Math.Max(0, producto.Stock - reservado);
		}
	}
}
