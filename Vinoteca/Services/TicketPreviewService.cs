using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Vinoteca.Models;
using Windows.UI.Text;

namespace Vinoteca.Services
{
	public static class TicketPreviewService
	{
		public static async Task MostrarAsync(Venta venta, XamlRoot xamlRoot)
		{
			var panel = new StackPanel
			{
				Spacing = 12,
				MaxWidth = 620
			};

			panel.Children.Add(CrearEncabezado(venta));

			foreach (var item in venta.Productos)
			{
				panel.Children.Add(CrearFilaProducto(
					item.Producto.Nombre ?? "Producto",
					item.Producto.Marca ?? string.Empty,
					item.Cantidad,
					item.Subtotal));
			}

			panel.Children.Add(CrearTotales(venta));

			var dialog = new ContentDialog
			{
				Title = "Vista previa del ticket",
				Content = new ScrollViewer
				{
					Content = new Border
					{
						Background = ObtenerBrocha("WinePanelBrush"),
						BorderBrush = ObtenerBrocha("WineBorderBrush"),
						BorderThickness = new Thickness(1),
						CornerRadius = new CornerRadius(8),
						Padding = new Thickness(20),
						Child = panel
					},
					MaxHeight = 620
				},
				CloseButtonText = "Cerrar",
				XamlRoot = xamlRoot
			};

			await dialog.ShowAsync();
		}

		private static UIElement CrearEncabezado(Venta venta)
		{
			var contenedor = new Border
			{
				Background = ObtenerBrocha("WineSubtleBrush"),
				BorderBrush = ObtenerBrocha("WineBorderBrush"),
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(8),
				Padding = new Thickness(18)
			};

			var panel = new Grid
			{
				ColumnSpacing = 18
			};
			panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			var marca = new StackPanel
			{
				Spacing = 8
			};

			var marcaLinea = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 10
			};

			marcaLinea.Children.Add(new Image
			{
				Source = new BitmapImage(new System.Uri("ms-appx:///Assets/StoreLogo.png")),
				Width = 30,
				Height = 30
			});

			marcaLinea.Children.Add(new TextBlock
			{
				Text = "Vinoteca",
				FontSize = 24,
				FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
				Foreground = ObtenerBrocha("WineTextBrush"),
				VerticalAlignment = VerticalAlignment.Center
			});

			marca.Children.Add(marcaLinea);
			marca.Children.Add(CrearTexto($"Cliente: {venta.NombreCliente}", "WineTextBrush", 13, Microsoft.UI.Text.FontWeights.SemiBold));
			marca.Children.Add(CrearTexto(venta.CorreoCliente ?? string.Empty, "WineMutedBrush", 13));

			var datos = new StackPanel
			{
				Spacing = 6,
				HorizontalAlignment = HorizontalAlignment.Right
			};
			datos.Children.Add(CrearEtiqueta($"Ticket #{venta.Id}"));
			datos.Children.Add(CrearTexto(venta.Fecha.ToString("g", CultureInfo.CurrentCulture), "WineMutedBrush", 13));

			Grid.SetColumn(marca, 0);
			Grid.SetColumn(datos, 1);
			panel.Children.Add(marca);
			panel.Children.Add(datos);

			contenedor.Child = panel;
			return contenedor;
		}

		private static UIElement CrearFilaProducto(string nombre, string marca, int cantidad, double subtotal)
		{
			var contenedor = new Border
			{
				Background = ObtenerBrocha("WinePanelBrush"),
				BorderBrush = ObtenerBrocha("WineBorderBrush"),
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(8),
				Padding = new Thickness(14)
			};

			var grid = new Grid
			{
				ColumnSpacing = 12
			};
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			var datos = new StackPanel { Spacing = 3 };
			datos.Children.Add(CrearTexto(nombre, "WineTextBrush", 14, Microsoft.UI.Text.FontWeights.SemiBold));
			datos.Children.Add(CrearTexto(marca, "WineMutedBrush", 12));
			datos.Children.Add(CrearTexto($"{cantidad} unidad(es)", "WineLabelBrush", 12, Microsoft.UI.Text.FontWeights.SemiBold));

			var total = CrearTexto(subtotal.ToString("C", CultureInfo.CurrentCulture), "WineHeroBrush", 14, Microsoft.UI.Text.FontWeights.SemiBold);
			total.VerticalAlignment = VerticalAlignment.Center;

			Grid.SetColumn(datos, 0);
			Grid.SetColumn(total, 1);
			grid.Children.Add(datos);
			grid.Children.Add(total);
			contenedor.Child = grid;

			return contenedor;
		}

		private static UIElement CrearTotales(Venta venta)
		{
			var totales = TicketPdfService.CalcularTotales(venta);
			var panel = new StackPanel
			{
				Spacing = 8
			};

			panel.Children.Add(CrearLineaTotal("Subtotal sin IVA", totales.Subtotal, 14, "WineMutedBrush"));
			panel.Children.Add(CrearLineaTotal("IVA incluido 16%", totales.Iva, 14, "WineMutedBrush"));
			panel.Children.Add(CrearLineaTotal("Total pagado", totales.Total, 20, "WineHeroBrush"));

			return new Border
			{
				Background = ObtenerBrocha("WineSubtleBrush"),
				CornerRadius = new CornerRadius(8),
				Padding = new Thickness(16),
				Margin = new Thickness(0, 8, 0, 0),
				Child = panel
			};
		}

		private static UIElement CrearLineaTotal(string etiqueta, double valor, double tamano, string recurso)
		{
			var grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			var texto = CrearTexto(etiqueta, recurso, tamano, Microsoft.UI.Text.FontWeights.SemiBold);
			var monto = CrearTexto(valor.ToString("C", CultureInfo.CurrentCulture), recurso, tamano, Microsoft.UI.Text.FontWeights.SemiBold);

			Grid.SetColumn(texto, 0);
			Grid.SetColumn(monto, 1);
			grid.Children.Add(texto);
			grid.Children.Add(monto);

			return grid;
		}

		private static TextBlock CrearTexto(string texto, string recurso, double tamano, FontWeight? peso = null)
		{
			return new TextBlock
			{
				Text = texto,
				FontSize = tamano,
				FontWeight = peso ?? Microsoft.UI.Text.FontWeights.Normal,
				Foreground = ObtenerBrocha(recurso),
				TextWrapping = TextWrapping.Wrap
			};
		}

		private static Border CrearEtiqueta(string texto)
		{
			return new Border
			{
				Background = ObtenerBrocha("WineHeroSoftBrush"),
				CornerRadius = new CornerRadius(6),
				Padding = new Thickness(10, 6, 10, 6),
				Child = CrearTexto(texto, "WineHeroBrush", 13, Microsoft.UI.Text.FontWeights.SemiBold)
			};
		}

		private static Brush ObtenerBrocha(string recurso)
		{
			return (Brush)Application.Current.Resources[recurso];
		}
	}
}
