using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace Dominion.NET_WPF.Caching
{
	public static class BrushRepository
	{
		private static Dictionary<DominionBase.Cards.Category, Brush> _BGBrushCache = new Dictionary<DominionBase.Cards.Category, Brush>();

		public static Brush GetForegroundBrush(DominionBase.Cards.Category cardType)
		{
			if ((cardType & DominionBase.Cards.Category.Attack) == DominionBase.Cards.Category.Attack)
				return Brushes.Firebrick;
			if ((cardType & DominionBase.Cards.Category.Curse) == DominionBase.Cards.Category.Curse)
				return Brushes.Snow;
			return Brushes.Black;
		}

		public static Brush GetBackgroundBrush(DominionBase.Cards.Category cardType)
		{
			if (!_BGBrushCache.ContainsKey(cardType))
			{
				Color topColor = Colors.Transparent;
				Color bottomColor = Colors.Transparent;
				List<String> cardTypes = new List<String>();
				if ((cardType & DominionBase.Cards.Category.Action) == DominionBase.Cards.Category.Action)
					topColor = Color.FromRgb(231, 231, 231);

				if ((cardType & DominionBase.Cards.Category.Curse) == DominionBase.Cards.Category.Curse)
					topColor = Color.FromRgb(129, 0, 127);

				if ((cardType & DominionBase.Cards.Category.Duration) == DominionBase.Cards.Category.Duration)
					topColor = Color.FromRgb(248, 119, 35);

				if ((cardType & DominionBase.Cards.Category.Ruins) == DominionBase.Cards.Category.Ruins)
					topColor = Color.FromRgb(162, 123, 23);

				if ((cardType & DominionBase.Cards.Category.Shelter) == DominionBase.Cards.Category.Shelter)
				{
					if (topColor == Colors.Transparent)
						topColor = Color.FromRgb(232, 98, 87);
					else
						bottomColor = Color.FromRgb(232, 98, 87);
				}

				if ((cardType & DominionBase.Cards.Category.Treasure) == DominionBase.Cards.Category.Treasure)
				{
					if (topColor == Colors.Transparent)
						topColor = Color.FromRgb(247, 214, 98);
					else
						bottomColor = Color.FromRgb(247, 214, 98);
				}

				if ((cardType & DominionBase.Cards.Category.Victory) == DominionBase.Cards.Category.Victory)
				{
					if (topColor == Colors.Transparent)
						topColor = Color.FromRgb(144, 238, 144);
					else
						bottomColor = Color.FromRgb(144, 238, 144);
				}

				if ((cardType & DominionBase.Cards.Category.Reaction) == DominionBase.Cards.Category.Reaction)
				{
					if (topColor == Colors.Transparent || (topColor.R == 231 && topColor.G == 231 && topColor.B == 231))
						topColor = Color.FromRgb(64, 103, 224);
					else
						bottomColor = Color.FromRgb(64, 103, 224);
				}

				if (topColor == Colors.Transparent)
					topColor = bottomColor;
				if (bottomColor == Colors.Transparent)
					bottomColor = topColor;

				GradientStop gs1 = new GradientStop(topColor, 0.0);
				GradientStop gs2 = new GradientStop(topColor, 0.35);
				GradientStop gs3 = new GradientStop(bottomColor, 0.65);
				GradientStop gs4 = new GradientStop(bottomColor, 1.0);
				_BGBrushCache[cardType] = new LinearGradientBrush(new GradientStopCollection() { gs1, gs2, gs3, gs4 }, 90.0);
			}

			if (_BGBrushCache[cardType].CanFreeze)
				_BGBrushCache[cardType].Freeze();
			return _BGBrushCache[cardType];
		}
	}
}
