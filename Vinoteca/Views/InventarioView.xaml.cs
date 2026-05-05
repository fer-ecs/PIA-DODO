using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Vinoteca.Helpers;
using Vinoteca.Models;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class InventarioView : Page
	{
		public ObservableCollection<ProductoItemViewModel> ProductosMostrados { get; } = new();
		private List<Producto> todosLosProductos = new();
		private Producto? productoSeleccionado;

		public InventarioView()
		{
			InitializeComponent();
			InputRestrictionsHelper.AplicarSinEspaciosNiEnter(this);
			lvProductos.ItemsSource = ProductosMostrados;

			if (!SessionService.EsAdminActivo)
			{
				BloquearAcceso();
				return;
			}

			CargarDatos();
		}

		private void BloquearAcceso()
		{
			txtNombre.IsEnabled = false;
			txtMarca.IsEnabled = false;
			cmbCategoria.IsEnabled = false;
			numPrecioVenta.IsEnabled = false;
			numStock.IsEnabled = false;
			txtImagen.IsEnabled = false;
			btnGuardar.IsEnabled = false;
			btnEliminar.IsEnabled = false;
			btnLimpiar.IsEnabled = false;
			lvProductos.IsEnabled = false;
			txtBuscar.IsEnabled = false;
			MostrarMensaje("Solo un administrador puede gestionar inventario", false);
		}

		private void CargarDatos()
		{
			todosLosProductos = DataService.ObtenerProductos().OrderBy(p => p.Nombre).ToList();
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
				.Select(p => new ProductoItemViewModel(p))
				.ToList();

			ProductosMostrados.Clear();
			foreach (var producto in filtrados)
			{
				ProductosMostrados.Add(producto);
			}
		}

		private void btnGuardar_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.EsAdminActivo)
			{
				MostrarMensaje("Solo un administrador puede guardar productos", false);
				return;
			}

			if (!ValidarFormulario(out string nombre, out string marca, out string categoria, out string imagen, out double precio, out int stock))
			{
				return;
			}

			var producto = productoSeleccionado ?? new Producto();
			producto.Nombre = nombre;
			producto.Marca = marca;
			producto.Categoria = categoria;
			producto.PrecioVenta = precio;
			producto.Stock = stock;
			producto.ImagenPath = imagen;
			producto.Activo = stock > 0;

			DataService.GuardarProducto(producto);

			LimpiarFormulario();
			CargarDatos();
			MostrarMensaje(productoSeleccionado == null ? "Producto creado correctamente" : "Producto actualizado correctamente", true);
		}

		private bool ValidarFormulario(out string nombre, out string marca, out string categoria, out string imagen, out double precio, out int stock)
		{
			nombre = txtNombre.Text?.Trim() ?? string.Empty;
			marca = txtMarca.Text?.Trim() ?? string.Empty;
			categoria = (cmbCategoria.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
			imagen = txtImagen.Text?.Trim() ?? string.Empty;
			precio = numPrecioVenta.Value;
			stock = (int)numStock.Value;

			if (string.IsNullOrWhiteSpace(nombre))
			{
				MostrarMensaje("El nombre del producto es obligatorio", false);
				return false;
			}

			if (nombre.Length < 3 || nombre.Length > 60)
			{
				MostrarMensaje("El nombre debe tener entre 3 y 60 caracteres", false);
				return false;
			}

			if (string.IsNullOrWhiteSpace(marca))
			{
				MostrarMensaje("La marca es obligatoria", false);
				return false;
			}

			if (marca.Length < 2 || marca.Length > 40)
			{
				MostrarMensaje("La marca debe tener entre 2 y 40 caracteres", false);
				return false;
			}

			if (string.IsNullOrWhiteSpace(categoria))
			{
				MostrarMensaje("Selecciona una categoria", false);
				return false;
			}

			if (precio <= 0 || precio > 100000)
			{
				MostrarMensaje("El precio debe ser mayor a 0 y menor o igual a 100000", false);
				return false;
			}

			if (stock < 0 || stock > 5000)
			{
				MostrarMensaje("El stock debe estar entre 0 y 5000", false);
				return false;
			}

			if (!string.IsNullOrWhiteSpace(imagen))
			{
				if (imagen.Length > 300)
				{
					MostrarMensaje("La ruta o URL de la imagen no debe exceder 300 caracteres", false);
					return false;
				}

				bool esUrlValida = Uri.TryCreate(imagen, UriKind.Absolute, out Uri? uriResultado)
					&& (uriResultado.Scheme == Uri.UriSchemeHttp || uriResultado.Scheme == Uri.UriSchemeHttps);
				bool esRutaLocalValida = Regex.IsMatch(imagen, @"^[A-Za-z]:\\.+");

				if (!esUrlValida && !esRutaLocalValida)
				{
					MostrarMensaje("La imagen debe ser una URL valida o una ruta local valida", false);
					return false;
				}
			}

			string nombreValidado = nombre;
			string marcaValidada = marca;
			bool duplicado = DataService.ObtenerProductos().Any(p =>
				p.Id != productoSeleccionado?.Id &&
				p.Nombre != null &&
				p.Marca != null &&
				p.Nombre.Equals(nombreValidado, StringComparison.OrdinalIgnoreCase) &&
				p.Marca.Equals(marcaValidada, StringComparison.OrdinalIgnoreCase));

			if (duplicado)
			{
				MostrarMensaje("Ya existe un producto con el mismo nombre y marca", false);
				return false;
			}

			return true;
		}

		private void lvProductos_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (lvProductos.SelectedItem is not ProductoItemViewModel item)
			{
				return;
			}

			productoSeleccionado = item.Producto;
			txtNombre.Text = item.Producto.Nombre;
			txtMarca.Text = item.Producto.Marca;
			numPrecioVenta.Value = item.Producto.PrecioVenta;
			numStock.Value = item.Producto.Stock;
			txtImagen.Text = item.Producto.ImagenPath;

			foreach (ComboBoxItem comboItem in cmbCategoria.Items)
			{
				if (comboItem.Content?.ToString() == item.Producto.Categoria)
				{
					cmbCategoria.SelectedItem = comboItem;
					break;
				}
			}
		}

		private void btnEliminar_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.EsAdminActivo)
			{
				MostrarMensaje("Solo un administrador puede eliminar productos", false);
				return;
			}

			if (productoSeleccionado == null)
			{
				MostrarMensaje("Selecciona un producto para eliminar", false);
				return;
			}

			DataService.EliminarProducto(productoSeleccionado.Id);
			LimpiarFormulario();
			CargarDatos();
			MostrarMensaje("Producto eliminado correctamente", true);
		}

		private void btnLimpiar_Click(object sender, RoutedEventArgs e)
		{
			LimpiarFormulario();
			OcultarMensaje();
		}

		private void LimpiarFormulario()
		{
			productoSeleccionado = null;
			txtNombre.Text = string.Empty;
			txtMarca.Text = string.Empty;
			cmbCategoria.SelectedIndex = -1;
			numPrecioVenta.Value = 0;
			numStock.Value = 0;
			txtImagen.Text = string.Empty;
			lvProductos.SelectedItem = null;
		}

		private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
		{
			AplicarFiltro();
		}

		private void MostrarMensaje(string mensaje, bool esExito)
		{
			txtMensaje.Text = mensaje;
			txtMensaje.Foreground = new SolidColorBrush(esExito ? Microsoft.UI.Colors.Green : Microsoft.UI.Colors.Red);
			txtMensaje.Visibility = Visibility.Visible;
		}

		private void OcultarMensaje()
		{
			txtMensaje.Visibility = Visibility.Collapsed;
		}
	}

	public class ProductoItemViewModel
	{
		public Producto Producto { get; }
		public string Nombre => Producto.Nombre ?? string.Empty;
		public string Categoria => Producto.Categoria ?? string.Empty;
		public string PrecioTexto => Producto.PrecioVenta.ToString("C");
		public string StockTexto => Producto.Stock.ToString();
		public string EstadoTexto => Producto.Activo ? "Activo" : "Inactivo";

		public ProductoItemViewModel(Producto producto)
		{
			Producto = producto;
		}
	}
}
