using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using Vinoteca.Services;

namespace Vinoteca.Helpers
{
	// esta seccion sirve para agrupar las validaciones y apoyos de entrada y dejar esa responsabilidad en un solo archivo - ImageSourceConverter
	public class ImageSourceConverter : IValueConverter
	{
		// esta seccion sirve para adaptar valores de las validaciones y apoyos de entrada para que XAML los pueda mostrar bien - Convert
		public object? Convert(object value, Type targetType, object parameter, string language)
		{
			var path = value as string;

            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

			try
			{
				string? rutaImagen = ImageAssetService.ResolverRutaAbsoluta(path);
				if (!string.IsNullOrWhiteSpace(rutaImagen))
				{
					return new BitmapImage(new Uri(rutaImagen, UriKind.Absolute));
				}

				return null;
			}
			catch
			{
				return null;
			}
		}

		// esta seccion sirve para adaptar valores de las validaciones y apoyos de entrada para que XAML los pueda mostrar bien - ConvertBack
		public object? ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
