using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Text;

namespace Dominion.NET_WPF.Caching
{
	public static class DropShadowRepository
	{
		private static Dictionary<int, DropShadowEffect> _DSECache = new Dictionary<int, DropShadowEffect>();

		public static DropShadowEffect GetDSE(int blurRadius, Color color, double opacity)
		{
			return GetDSE(blurRadius, color, opacity, true);
		}

		public static DropShadowEffect GetDSE(int blurRadius, Color color, double opacity, Boolean isFrozen)
		{
			int hashKey = 3 * blurRadius.GetHashCode() + 5 * color.GetHashCode() + 7 * opacity.GetHashCode();

			if (!isFrozen || !_DSECache.ContainsKey(hashKey))
			{
				DropShadowEffect dse = new DropShadowEffect();
				dse.BlurRadius = blurRadius;
				dse.Color = color;
				dse.Opacity = opacity;
				dse.ShadowDepth = 0;
				dse.Direction = 0;
				if (!isFrozen)
					return dse;

				_DSECache[hashKey] = dse;
			}

			if (_DSECache[hashKey].CanFreeze)
				_DSECache[hashKey].Freeze();
			return _DSECache[hashKey];
		}
	}
}
