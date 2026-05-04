using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace Vinoteca.Helpers
{
	public class ImageSourceConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			string path = value as string;

			if (string.IsNullOrWhiteSpace(path))
				return null; // Si está vacío, no manda nada y evita el crash

			try
			{
				// Si la ruta no empieza con http o ms-appx, asumimos que es una URL o recurso
				return new BitmapImage(new Uri(path));
			}
			catch
			{
				return null; // Si la URL está mal formada, devuelve null de forma segura
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}