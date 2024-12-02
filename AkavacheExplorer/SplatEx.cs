using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

using Splat;

namespace AkavacheExplorer;

public static class SplatEx {
    public static async Task<BitmapImage> ToWpfBitmap(this IBitmap splatBitmap) {
        // Create a memory stream to hold the saved image
        using var memoryStream = new MemoryStream();

        // Save the Splat bitmap to the memory stream in PNG format
        await splatBitmap.Save(CompressedBitmapFormat.Png, 1.0f, memoryStream);

        // Reset the position of the stream to the beginning
        memoryStream.Seek(0, SeekOrigin.Begin);

        // Create a BitmapImage and load the memory stream
        var wpfBitmap = new BitmapImage();
        wpfBitmap.BeginInit();
        wpfBitmap.StreamSource = memoryStream;
        wpfBitmap.CacheOption = BitmapCacheOption.OnLoad;
        wpfBitmap.EndInit();

        return wpfBitmap;
    }
}
