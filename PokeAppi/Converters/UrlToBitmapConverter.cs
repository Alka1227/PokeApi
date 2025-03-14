using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PokeAppi.Converters
{
    public class UrlToBitmapConverter : IValueConverter
    {
        private static readonly HttpClient _httpClient = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string url && !string.IsNullOrEmpty(url))
            {
                Debug.WriteLine($"Loading image from: {url}");
                return Task.Run(async () => await LoadBitmapFromUrl(url)).Result;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static async Task<Bitmap> LoadBitmapFromUrl(string url)
        {
            try
            {
                var response = await _httpClient.GetByteArrayAsync(url);
                using var stream = new MemoryStream(response);
                return new Bitmap(stream);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

}
