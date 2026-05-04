using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Vinoteca.Models;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class InventarioView : Page
	{
		// ObservableCollection permite que la UI se actualice sola cuando cambia la lista
		public ObservableCollection<Producto> ProductosMostrados { get; set; }
		private List<Producto> todosLosProductos;
		private Producto productoSeleccionado;

		public InventarioView()
		{
			this.InitializeComponent();
			CargarDatos();
		}

		private void CargarDatos()
		{
			todosLosProductos = DataService.ObtenerProductos();
			ProductosMostrados = new ObservableCollection<Producto>(todosLosProductos);
			lvProductos.ItemsSource = ProductosMostrados;
		}

		private void btnGuardar_Click(object sender, RoutedEventArgs e)
		{
			// 1. Validar campos básicos
			if (string.IsNullOrWhiteSpace(txtNombre.Text)) return;

			// 2. Crear o actualizar objeto
			var p = productoSeleccionado ?? new Producto();
			p.Nombre = txtNombre.Text;
			p.Marca = txtMarca.Text;
			p.Categoria = (cmbCategoria.SelectedItem as ComboBoxItem)?.Content.ToString();
			p.PrecioVenta = numPrecioVenta.Value;
			p.Stock = (int)numStock.Value;
			p.ImagenPath = txtImagen.Text;

			// 3. Guardar en JSON
			DataService.GuardarProducto(p);

			// 4. Refrescar UI
			LimpiarFormulario();
			CargarDatos();
		}

		private void lvProductos_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (lvProductos.SelectedItem is Producto seleccionado)
			{
				productoSeleccionado = seleccionado;
				txtNombre.Text = seleccionado.Nombre;
				txtMarca.Text = seleccionado.Marca;
				numPrecioVenta.Value = seleccionado.PrecioVenta;
				numStock.Value = seleccionado.Stock;
				txtImagen.Text = seleccionado.ImagenPath;

				// Seleccionar categoría en el ComboBox
				foreach (ComboBoxItem item in cmbCategoria.Items)
				{
					if (item.Content.ToString() == seleccionado.Categoria)
					{
						cmbCategoria.SelectedItem = item;
						break;
					}
				}
			}
		}

		private void btnEliminar_Click(object sender, RoutedEventArgs e)
		{
			if (productoSeleccionado != null)
			{
				DataService.EliminarProducto(productoSeleccionado.Id);
				LimpiarFormulario();
				CargarDatos();
			}
		}

		private void btnLimpiar_Click(object sender, RoutedEventArgs e)
		{
			LimpiarFormulario();
		}

		private void LimpiarFormulario()
		{
			productoSeleccionado = null;
			txtNombre.Text = "";
			txtMarca.Text = "";
			cmbCategoria.SelectedIndex = -1;
			numPrecioVenta.Value = 0;
			numStock.Value = 0;
			txtImagen.Text = "";
			lvProductos.SelectedItem = null;
		}

		private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
		{
			var busqueda = txtBuscar.Text.ToLower();
			var filtrados = todosLosProductos.Where(p =>
				p.Nombre.ToLower().Contains(busqueda) ||
				p.Marca.ToLower().Contains(busqueda)).ToList();

			ProductosMostrados.Clear();
			foreach (var p in filtrados) ProductosMostrados.Add(p);
		}
	}
}