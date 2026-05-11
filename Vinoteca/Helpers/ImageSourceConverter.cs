using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using Vinoteca.Services;

namespace Vinoteca.Helpers
{
	public class ImageSourceConverter : IValueConverter
	{
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

		public object? ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
