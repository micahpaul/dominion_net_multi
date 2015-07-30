using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Text;

namespace Dominion.NET_WPF.Caching
{
	public class ImageRepository
	{
#if (DEBUG)
		public static readonly String ImageRoot = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName).FullName).FullName).FullName;
#else
		public static readonly String ImageRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName;
#endif

		private static Dictionary<String, Dictionary<String, BitmapImage>> _BitmapImageCache = new Dictionary<String, Dictionary<String, BitmapImage>>();

		private static System.Threading.Mutex _Mutex;
		private static ImageRepository _Instance;

		private ImageRepository() { }
		static ImageRepository()
		{
			_Instance = new ImageRepository();
			_Mutex = new System.Threading.Mutex();
		}

		public static void Reset()
		{
			_BitmapImageCache.Clear();
		}

		public static ImageRepository Acquire()
		{
			_Mutex.WaitOne();
			return _Instance;
		}

		public static void Release()
		{
			_Mutex.ReleaseMutex();
		}

		public BitmapImage GetBitmapImage(String imageName, String imageSize)
		{
			if (wMain.Settings == null)
				return null;

			if (!_BitmapImageCache.ContainsKey(imageName))
				_BitmapImageCache[imageName] = new Dictionary<String, BitmapImage>();
			if (!_BitmapImageCache[imageName].ContainsKey(imageSize))
			{
				String customPath = String.Empty;
				switch (imageSize)
				{
					case "small":
						if (wMain.Settings.UseCustomImages)
							customPath = wMain.Settings.CustomImagesPathSmall;
						else
							customPath = "small";
						break;
					case "medium":
						if (wMain.Settings.UseCustomImages)
							customPath = wMain.Settings.CustomImagesPathMedium;
						else
							customPath = "medium";
						break;
					case "full":
						if (wMain.Settings.UseCustomToolTips)
							customPath = wMain.Settings.CustomToolTipsPath;
						break;
				}
				if (!System.IO.Path.IsPathRooted(customPath))
					customPath = BetterCombine(ImageRoot, "images", customPath);
				_BitmapImageCache[imageName][imageSize] = LoadImage(Path.Combine(customPath, imageName));

				// Fall back to the standard if we can't find the custom image
				if (_BitmapImageCache[imageName][imageSize] == null)
					_BitmapImageCache[imageName][imageSize] = LoadImage(BetterCombine(ImageRoot, "images", imageSize, imageName));
			}

			BitmapImage im = _BitmapImageCache[imageName][imageSize];
			if (im != null && im.CanFreeze)
				im.Freeze();

			return im;
		}

		private BitmapImage LoadImage(string filename)
		{
			if (File.Exists(filename + ".png"))
				return new BitmapImage(new Uri(filename + ".png"));
			else if (File.Exists(filename + ".jpg"))
				return new BitmapImage(new Uri(filename + ".jpg"));
			return null;
		}

		private String BetterCombine(params String[] pathList)
		{
			String ret = String.Empty;
			foreach (String path in pathList)
			{
				if (ret == String.Empty)
					ret = path;
				else
					ret = System.IO.Path.Combine(ret, path);
			}
			return ret;
		}


	}
}
