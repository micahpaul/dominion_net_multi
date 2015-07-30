using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Xml;

namespace Dominion.NET_WPF
{
	public enum CardSize
	{
		Text,
		SmallText,
		Small,
		Medium,
		Full
	}

	public class ConstraintTypeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			if (value.GetType() == typeof(DominionBase.Cards.ConstraintType))
			{
				return ((DominionBase.Cards.ConstraintType)value).ToDescription();
			}
			return value.ToString();
		}
		public object ConvertBack(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			return null;
		}
	}

	public class ConstraintTypeToolTipConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			if (value.GetType() == typeof(DominionBase.Cards.ConstraintType))
			{
				return ((DominionBase.Cards.ConstraintType)value).ToToolTip();
			}
			return value.ToString();
		}
		public object ConvertBack(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			return null;
		}
	}

	public class ConstraintConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			if (value == null)
				return value;

			Type vt = value.GetType();
			if (vt == typeof(KeyValuePair<DominionBase.Cards.Cost, int>))
				return String.Format("({1}) Cost: {0}", Utilities.RenderText(((KeyValuePair<DominionBase.Cards.Cost, int>)value).Key.ToString(", ")), ((KeyValuePair<DominionBase.Cards.Cost, int>)value).Value);
			else if (vt == typeof(KeyValuePair<DominionBase.Cards.Group, int>))
				return String.Format("({1}) {0}", ((KeyValuePair<DominionBase.Cards.Group, int>)value).Key.ToDescription(), ((KeyValuePair<DominionBase.Cards.Group, int>)value).Value);
			else if (vt == typeof(KeyValuePair<DominionBase.Cards.Source, int>))
				return String.Format("({1}) {0}", ((KeyValuePair<DominionBase.Cards.Source, int>)value).Key, ((KeyValuePair<DominionBase.Cards.Source, int>)value).Value);
			else if (vt == typeof(KeyValuePair<DominionBase.Cards.Category, int>))
				return String.Format("({1}) {0}", ((KeyValuePair<DominionBase.Cards.Category, int>)value).Key, ((KeyValuePair<DominionBase.Cards.Category, int>)value).Value);
			return value.ToString();
		}
		public object ConvertBack(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			return null;
		}
	}

	public static class ExtensionMethods
	{
		public static string ToDescription(this Enum en) //ext method
		{
			Type type = en.GetType();
			MemberInfo[] memInfo = type.GetMember(en.ToString());
			if (memInfo != null && memInfo.Length > 0)
			{
				object[] attrs = memInfo[0].GetCustomAttributes(
											  typeof(DescriptionAttribute),
											  false);
				if (attrs != null && attrs.Length > 0)
					return ((DescriptionAttribute)attrs[0]).Description;
			}
			return en.ToString();
		}
		public static string ToToolTip(this Enum en) //ext method
		{
			Type type = en.GetType();
			MemberInfo[] memInfo = type.GetMember(en.ToString());
			if (memInfo != null && memInfo.Length > 0)
			{
				object[] attrs = memInfo[0].GetCustomAttributes(
											  typeof(DominionBase.Cards.ToolTipAttribute),
											  false);
				if (attrs != null && attrs.Length > 0)
					return ((DominionBase.Cards.ToolTipAttribute)attrs[0]).ToolTip;
			}
			return en.ToString();
		}
	}

	static class CustomCommands
	{
		public static RoutedCommand CardViewer = new RoutedCommand();
		public static RoutedCommand SaveGame = new RoutedCommand();
		public static RoutedCommand LoadGame = new RoutedCommand();
	}

	public enum SupplyVisibility
	{
		Plain,
		Gainable,
		Selectable,
		NotClickable
	}

	public class DisplayObjects
	{
		private static ObservableCollection<DominionBase.Cards.Card> _Cards = null;
		private static ObservableCollection<KeyValuePair<DominionBase.Cards.Group, int>> _Groups = null;
		private static Dictionary<DominionBase.Cards.Group, int> _GroupsDict = null;
		private static ObservableCollection<KeyValuePair<DominionBase.Cards.Cost, int>> _Costs = null;
		private static Dictionary<DominionBase.Cards.Cost, int> _CostsDict = null;
		private static ObservableCollection<KeyValuePair<DominionBase.Cards.Source, int>> _Sources = null;
		private static Dictionary<DominionBase.Cards.Source, int> _SourcesDict = null;
		private static ObservableCollection<KeyValuePair<DominionBase.Cards.Category, int>> _CategoriesExact = null;
		private static Dictionary<DominionBase.Cards.Category, int> _CategoriesExactDict = null;
		private static ObservableCollection<KeyValuePair<DominionBase.Cards.Category, int>> _CategoriesContains = null;
		private static Dictionary<DominionBase.Cards.Category, int> _CategoriesContainsDict = null;

		public static ObservableCollection<DominionBase.Cards.Card> Cards
		{
			get
			{
				if (_Cards == null)
				{
					DominionBase.Cards.CardCollection cards = DominionBase.Cards.CardCollection.GetAllCards(c => c.Location == DominionBase.Cards.Location.Kingdom);
					cards.Sort(delegate(DominionBase.Cards.Card c1, DominionBase.Cards.Card c2) { return c1.Name.CompareTo(c2.Name); });
					_Cards = new ObservableCollection<DominionBase.Cards.Card>(cards);
				}
				return _Cards;
			}
		}

		public static ObservableCollection<KeyValuePair<DominionBase.Cards.Group, int>> Groups
		{
			get
			{
				if (_Groups == null)
				{
					_Groups = new ObservableCollection<KeyValuePair<DominionBase.Cards.Group, int>>(GroupsDict.OrderBy(kvp => (int)kvp.Key));
				}
				return _Groups;
			}
		}

		private static Dictionary<DominionBase.Cards.Group, int> GroupsDict
		{
			get
			{
				if (_GroupsDict == null)
				{
					_GroupsDict = new Dictionary<DominionBase.Cards.Group, int>();
					foreach (DominionBase.Cards.Card card in Cards)
					{
						foreach (DominionBase.Cards.Group group in Enum.GetValues(typeof(DominionBase.Cards.Group)))
						{
							if (group == DominionBase.Cards.Group.Basic || group == DominionBase.Cards.Group.None)
								continue;
							if ((card.GroupMembership & group) == group)
							{
								if (!_GroupsDict.ContainsKey(group))
									_GroupsDict[group] = 0;
								_GroupsDict[group]++;
							}
						}
					}
				}
				return _GroupsDict;
			}
		}

		public static ObservableCollection<KeyValuePair<DominionBase.Cards.Cost, int>> Costs
		{
			get
			{
				if (_Costs == null)
				{
					_Costs = new ObservableCollection<KeyValuePair<DominionBase.Cards.Cost, int>>(CostsDict.OrderBy(kvp => (DominionBase.Cards.Cost)kvp.Key));
				}
				return _Costs;
			}
		}

		private static Dictionary<DominionBase.Cards.Cost, int> CostsDict
		{
			get
			{
				if (_CostsDict == null)
				{
					_CostsDict = new Dictionary<DominionBase.Cards.Cost, int>();
					foreach (DominionBase.Cards.Card card in Cards)
					{
						if (!_CostsDict.ContainsKey(card.BaseCost))
							_CostsDict[card.BaseCost] = 0;
						_CostsDict[card.BaseCost]++;
					}
				}
				return _CostsDict;
			}
		}

		public static ObservableCollection<KeyValuePair<DominionBase.Cards.Source, int>> Sources
		{
			get
			{
				if (_Sources == null)
				{
					_Sources = new ObservableCollection<KeyValuePair<DominionBase.Cards.Source, int>>(SourcesDict.OrderBy(kvp => (int)kvp.Key));
				}
				return _Sources;
			}
		}

		private static Dictionary<DominionBase.Cards.Source, int> SourcesDict
		{
			get
			{
				if (_SourcesDict == null)
				{
					_SourcesDict = new Dictionary<DominionBase.Cards.Source, int>();
					foreach (DominionBase.Cards.Card card in Cards)
					{
						foreach (DominionBase.Cards.Source source in Enum.GetValues(typeof(DominionBase.Cards.Source)))
						{
							if (source == DominionBase.Cards.Source.All)
								continue;
							if (card.Source == source)
							{
								if (!_SourcesDict.ContainsKey(source))
									_SourcesDict[source] = 0;
								_SourcesDict[source]++;
							}
						}
					}
				}
				return _SourcesDict;
			}
		}

		public static ObservableCollection<KeyValuePair<DominionBase.Cards.Category, int>> CategoriesExact
		{
			get
			{
				if (_CategoriesExact == null)
				{
					_CategoriesExact = new ObservableCollection<KeyValuePair<DominionBase.Cards.Category, int>>(CategoriesExactDict.OrderBy(kvp => (int)kvp.Key));
				}
				return _CategoriesExact;
			}
		}

		private static Dictionary<DominionBase.Cards.Category, int> CategoriesExactDict
		{
			get
			{
				if (_CategoriesExactDict == null)
				{
					_CategoriesExactDict = new Dictionary<DominionBase.Cards.Category, int>();
					foreach (DominionBase.Cards.Card card in Cards)
					{
						foreach (DominionBase.Cards.Category category in Enum.GetValues(typeof(DominionBase.Cards.Category)))
						{
							if (category == DominionBase.Cards.Category.Unknown || category == DominionBase.Cards.Category.Prize)
								continue;
							if (card.Category == category)
							{
								if (!_CategoriesExactDict.ContainsKey(category))
									_CategoriesExactDict[category] = 0;
								_CategoriesExactDict[category]++;
							}
						}
					}
				}
				return _CategoriesExactDict;
			}
		}

		public static ObservableCollection<KeyValuePair<DominionBase.Cards.Category, int>> CategoriesContains
		{
			get
			{
				if (_CategoriesContains == null)
				{
					_CategoriesContains = new ObservableCollection<KeyValuePair<DominionBase.Cards.Category, int>>(CategoriesContainsDict.OrderBy(kvp => (int)kvp.Key));
				}
				return _CategoriesContains;
			}
		}

		private static Dictionary<DominionBase.Cards.Category, int> CategoriesContainsDict
		{
			get
			{
				if (_CategoriesContainsDict == null)
				{
					_CategoriesContainsDict = new Dictionary<DominionBase.Cards.Category, int>();
					foreach (DominionBase.Cards.Card card in Cards)
					{
						foreach (DominionBase.Cards.Category category in Enum.GetValues(typeof(DominionBase.Cards.Category)))
						{
							if (category == DominionBase.Cards.Category.Unknown || category == DominionBase.Cards.Category.Prize)
								continue;
							if ((card.Category & category) == category)
							{
								if (!_CategoriesContainsDict.ContainsKey(category))
									_CategoriesContainsDict[category] = 0;
								_CategoriesContainsDict[category]++;
							}
						}
					}
				}
				return _CategoriesContainsDict;
			}
		}
	}

	[ContentProperty("OverriddenDisplayEntries")]
	public class EnumDisplayer : IValueConverter
	{
		private Type type;
		private IDictionary displayValues;
		private IDictionary reverseValues;
		private List<EnumDisplayEntry> overriddenDisplayEntries;

		public EnumDisplayer()
		{
		}

		public EnumDisplayer(Type type)
		{
			this.Type = type;
		}

		public Type Type
		{
			get { return type; }
			set
			{
				if (!value.IsEnum)
					throw new ArgumentException("parameter is not an Enumermated type", "value");
				this.type = value;
			}
		}

		public ReadOnlyCollection<string> DisplayNames
		{
			get
			{
				Type displayValuesType = typeof(Dictionary<,>)
								.GetGenericTypeDefinition().MakeGenericType(typeof(string), type);
				this.displayValues = (IDictionary)Activator.CreateInstance(displayValuesType);

				this.reverseValues =
				   (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>)
							.GetGenericTypeDefinition()
							.MakeGenericType(type, typeof(string)));

				var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
				foreach (var field in fields)
				{
					DisplayStringAttribute[] a = (DisplayStringAttribute[])
												field.GetCustomAttributes(typeof(DisplayStringAttribute), false);

					string displayString = GetDisplayStringValue(a);
					object enumValue = field.GetValue(null);

					if (displayString == null)
					{
						displayString = GetBackupDisplayStringValue(enumValue);
					}
					if (displayString != null)
					{
						displayValues.Add(enumValue, displayString);
						reverseValues.Add(displayString, enumValue);
					}
				}
				return new List<string>((IEnumerable<string>)displayValues.Values).AsReadOnly();
			}
		}

		private string GetBackupDisplayStringValue(object enumValue)
		{
			if (overriddenDisplayEntries != null && overriddenDisplayEntries.Count > 0)
			{
				EnumDisplayEntry foundEntry = overriddenDisplayEntries.Find(delegate(EnumDisplayEntry entry)
												 {
													 object e = Enum.Parse(type, entry.EnumValue);
													 return enumValue.Equals(e);
												 });
				if (foundEntry != null)
				{
					if (foundEntry.ExcludeFromDisplay) return null;
					return foundEntry.DisplayString;

				}
			}
			return Enum.GetName(type, enumValue);
		}

		public List<EnumDisplayEntry> OverriddenDisplayEntries
		{
			get
			{
				if (overriddenDisplayEntries == null)
					overriddenDisplayEntries = new List<EnumDisplayEntry>();
				return overriddenDisplayEntries;
			}
		}

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return displayValues[value];
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return reverseValues[value];
		}

		private string GetDisplayStringValue(DisplayStringAttribute[] a)
		{
			if (a == null || a.Length == 0) return null;
			DisplayStringAttribute dsa = a[0];
			if (!string.IsNullOrEmpty(dsa.ResourceKey))
			{
				ResourceManager rm = new ResourceManager(type);
				return rm.GetString(dsa.ResourceKey);
			}
			return dsa.Value;
		}
	}

	public class EnumDisplayEntry
	{
		public string EnumValue { get; set; }
		public string DisplayString { get; set; }
		public bool ExcludeFromDisplay { get; set; }
	}

	public enum RenderSize
	{
		Tiny,
		Small,
		Large,
		ExtraLarge
	}

	public class Utilities
	{
		private static Regex tagMatch = new Regex(@"<(?<tag>[a-z]+)(/>|>(?<text>[^<]*?)</\k<tag>>)", RegexOptions.Singleline);

		public static String RenderText(String text)
		{
			StringBuilder sb = new StringBuilder();
			foreach (Run run in ((TextBlock)RenderText(text, RenderSize.Small, true)[0]).Inlines.OfType<Run>())
				sb.Append(run.Text);
			return sb.ToString();
		}
		public static List<UIElement> RenderText(String text, RenderSize renderSize, Boolean textOnly)
		{
			List<UIElement> elements = new List<UIElement>();
			TextBlock textBlockTemp = new TextBlock();
			InlineCollection inlines = textBlockTemp.Inlines;
			Match match = tagMatch.Match(text);

			while (match.Success)
			{
				switch (match.Groups["tag"].Value)
				{
					case "b":  // Bold
						if (match.Index > 0)
							inlines.Add(new Run(text.Substring(0, match.Index)));
						Run rB = new Run(match.Groups["text"].Value);
						rB.FontWeight = FontWeights.Bold;
						rB.FontSize = 14;
						inlines.Add(rB);
						text = text.Substring(match.Index + match.Length);
						break;
					case "i":  // Italics
						if (match.Index > 0)
							inlines.Add(new Run(text.Substring(0, match.Index)));
						Run rI = new Run(match.Groups["text"].Value);
						rI.FontStyle = FontStyles.Italic;
						rI.FontSize = 11;
						inlines.Add(rI);
						text = text.Substring(match.Index + match.Length);
						break;
					case "u":  // Underline
						if (match.Index > 0)
							inlines.Add(new Run(text.Substring(0, match.Index)));
						Run rU = new Run(match.Groups["text"].Value);
						rU.TextDecorations = TextDecorations.Underline;
						rU.FontSize = 11;
						inlines.Add(rU);
						text = text.Substring(match.Index + match.Length);
						break;
					case "h":  // Header
						if (match.Index > 0)
							inlines.Add(new Run(text.Substring(0, match.Index)));
						Run rH = new Run(match.Groups["text"].Value);
						switch (renderSize)
						{
							case RenderSize.ExtraLarge:
								rH.FontSize = 72;
								break;
							default:
								rH.FontSize = 36;
								break;
						}
						rH.FontWeight = FontWeights.Bold;
						inlines.Add(rH);
						text = text.Substring(match.Index + match.Length);
						break;
					case "br":  // Line break
						if (match.Index > 0)
							inlines.Add(new Run(text.Substring(0, match.Index)));
						inlines.Add(new LineBreak());
						text = text.Substring(match.Index + match.Length);
						break;
					case "sk":  // Shortcut Key
						if (match.Index > 0)
							inlines.Add(new Run(text.Substring(0, match.Index)));
						Run rSK = new Run(match.Groups["text"].Value);
						rSK.TextDecorations = TextDecorations.Underline;
						rSK.Foreground = Brushes.Crimson;
						inlines.Add(rSK);
						text = text.Substring(match.Index + match.Length);
						break;
					case "sm":
						if (match.Index > 0)
							inlines.Add(new Run(text.Substring(0, match.Index)));
						Run rS = new Run(match.Groups["text"].Value);
						rS.FontSize = 11;
						inlines.Add(rS);
						text = text.Substring(match.Index + match.Length);
						break;
					case "nbsp":  // Non-breaking space
						text = String.Format("{0}{1}{2}", text.Substring(0, match.Index), Convert.ToChar(160), text.Substring(match.Index + match.Length));
						break;
					case "vp":
						if (textOnly)
							text = String.Format("{0}{1}‡{2}", text.Substring(0, match.Index), match.Groups["text"].Value, text.Substring(match.Index + match.Length));
						else
						{
							if (!String.IsNullOrEmpty(text.Substring(0, match.Index)))
								inlines.Add(new Run(text.Substring(0, match.Index)));
							inlines.Add(new Run(match.Groups["text"].Value));
							text = text.Substring(match.Index + match.Length);

							InlineUIContainer vpUIContainer = new InlineUIContainer();
							vpUIContainer.BaselineAlignment = BaselineAlignment.Center;
							Image vp = new Image();
							Caching.ImageRepository ir = Caching.ImageRepository.Acquire();
							vp.Source = ir.GetBitmapImage("vp", String.Empty);
							switch (renderSize)
							{
								case RenderSize.Tiny:
									vp.Height = 14;
									vp.Width = 14;
									break;
								default:
									vp.Height = 18;
									vp.Width = 18;
									break;
							}
							Caching.ImageRepository.Release();
							vpUIContainer.Child = vp;
							inlines.Add(vpUIContainer);
						}
						break;
					case "vplg":
						if (textOnly)
							text = String.Format("<h>{0}{1}‡{2}</h>", text.Substring(0, match.Index), match.Groups["text"].Value, text.Substring(match.Index + match.Length));
						else
						{
							if (!String.IsNullOrEmpty(text.Substring(0, match.Index)))
								inlines.Add(new Run(text.Substring(0, match.Index)));
							Run rVpLg = new Run(match.Groups["text"].Value);
							switch (renderSize)
							{
								case RenderSize.ExtraLarge:
									rVpLg.FontSize = 72;
									break;
								default:
									rVpLg.FontSize = 36;
									break;
							}
							rVpLg.FontWeight = FontWeights.Bold;
							inlines.Add(rVpLg);
							text = text.Substring(match.Index + match.Length);

							InlineUIContainer vpUIContainer = new InlineUIContainer();
							vpUIContainer.BaselineAlignment = BaselineAlignment.Center;
							Image vp = new Image();
							Caching.ImageRepository ir = Caching.ImageRepository.Acquire();
							vp.Source = ir.GetBitmapImage("vplg", String.Empty);
							Caching.ImageRepository.Release();
							switch (renderSize)
							{
								case RenderSize.ExtraLarge:
									vp.Height = 96;
									vp.Width = 96;
									break;
								default:
									vp.Height = 48;
									vp.Width = 48;
									break;
							}
							vpUIContainer.Child = vp;
							inlines.Add(vpUIContainer);
						}
						break;
					case "coin":
						if (textOnly)
							text = String.Format("{0}{1}¢{2}", text.Substring(0, match.Index), match.Groups["text"].Value, text.Substring(match.Index + match.Length));
						else
						{
							if (!String.IsNullOrEmpty(text.Substring(0, match.Index)))
								inlines.Add(new Run(text.Substring(0, match.Index)));
							text = text.Substring(match.Index + match.Length);

							InlineUIContainer coinUIContainer = new InlineUIContainer();
							coinUIContainer.BaselineAlignment = BaselineAlignment.Center;
							Canvas coinCanvas = new Canvas();
							Image coin = new Image();
							Caching.ImageRepository ir = Caching.ImageRepository.Acquire();
							coin.Source = ir.GetBitmapImage("coin", String.Empty);
							switch (renderSize)
							{
								case RenderSize.Tiny:
									coinCanvas.Height = coin.Height = 14;
									coinCanvas.Width = coin.Width = 14;
									break;
								default:
									coinCanvas.Height = coin.Height = 18;
									coinCanvas.Width = coin.Width = 18;
									break;
							}
							Caching.ImageRepository.Release();
							coinCanvas.Children.Add(coin);
							TextBlock tbNum = new TextBlock();
							Run r = new Run(match.Groups["text"].Value);
							r.FontWeight = FontWeights.Bold;
							tbNum.Inlines.Add(r);
							Canvas.SetLeft(tbNum, 10 - 4 * match.Groups["text"].Value.Length);
							Canvas.SetTop(tbNum, 2);
							coinCanvas.Children.Add(tbNum);
							coinUIContainer.Child = coinCanvas;
							inlines.Add(coinUIContainer);
						}
						break;
					case "coinlg":
						if (textOnly)
							text = String.Format("<h>{0}{1}¢{2}</h>", text.Substring(0, match.Index), match.Groups["text"].Value, text.Substring(match.Index + match.Length));
						else
						{
							if (!String.IsNullOrEmpty(text.Substring(0, match.Index)))
								inlines.Add(new Run(text.Substring(0, match.Index)));
							text = text.Substring(match.Index + match.Length);

							InlineUIContainer coinUIContainer = new InlineUIContainer();
							coinUIContainer.BaselineAlignment = BaselineAlignment.Center;
							Canvas coinCanvas = new Canvas();
							Image coin = new Image();
							Caching.ImageRepository ir = Caching.ImageRepository.Acquire();
							coin.Source = ir.GetBitmapImage("coinlg", String.Empty);
							Caching.ImageRepository.Release();

							coinCanvas.Children.Add(coin);
							TextBlock tbNum = new TextBlock();

							Run r = new Run(match.Groups["text"].Value);
							r.FontWeight = FontWeights.Bold;

							int blurRadius = 0;
							switch (renderSize)
							{
								case RenderSize.ExtraLarge:
									r.FontSize = 72;
									coinCanvas.Height = coin.Height = 96;
									coinCanvas.Width = coin.Width = 96;
									Canvas.SetLeft(tbNum, 28);
									blurRadius = 50;
									break;
								default:
									r.FontSize = 36;
									coinCanvas.Height = coin.Height = 48;
									coinCanvas.Width = coin.Width = 48;
									Canvas.SetLeft(tbNum, 14);
									blurRadius = 30;
									break;
							}

							tbNum.Inlines.Add(r);
							tbNum.Effect = Caching.DropShadowRepository.GetDSE(blurRadius, Color.FromRgb(192, 192, 192), 1d);

							coinCanvas.Children.Add(tbNum);
							coinUIContainer.Child = coinCanvas;
							inlines.Add(coinUIContainer);
						}
						break;
					case "potion":
						if (textOnly)
							text = String.Format("{0}{1}¤{2}", text.Substring(0, match.Index), match.Groups["text"].Value, text.Substring(match.Index + match.Length));
						else
						{
							if (!String.IsNullOrEmpty(text.Substring(0, match.Index)))
								inlines.Add(new Run(text.Substring(0, match.Index)));
							if (match.Groups["text"].Value != "1")
								inlines.Add(new Run(match.Groups["text"].Value));
							text = text.Substring(match.Index + match.Length);

							InlineUIContainer potionUIContainer = new InlineUIContainer();
							potionUIContainer.BaselineAlignment = BaselineAlignment.Center;
							Image potion = new Image();
							Caching.ImageRepository ir = Caching.ImageRepository.Acquire();
							potion.Source = ir.GetBitmapImage("potion", String.Empty);
							switch (renderSize)
							{
								case RenderSize.Tiny:
									potion.Height = 14;
									potion.Width = 14;
									break;
								default:
									potion.Height = 18;
									potion.Width = 18;
									break;
							}
							Caching.ImageRepository.Release();
							potionUIContainer.Child = potion;
							inlines.Add(potionUIContainer);
						}
						break;
					case "potionlg":
						if (textOnly)
							text = String.Format("<h>{0}{1}¤{2}</h>", text.Substring(0, match.Index), match.Groups["text"].Value, text.Substring(match.Index + match.Length));
						else
						{
							if (!String.IsNullOrEmpty(text.Substring(0, match.Index)))
								inlines.Add(new Run(text.Substring(0, match.Index)));
							Run rPotionLg = new Run(match.Groups["text"].Value);
							switch (renderSize)
							{
								case RenderSize.ExtraLarge:
									rPotionLg.FontSize = 72;
									break;
								default:
									rPotionLg.FontSize = 36;
									break;
							}
							rPotionLg.FontWeight = FontWeights.Bold;
							inlines.Add(rPotionLg);
							text = text.Substring(match.Index + match.Length);

							InlineUIContainer potionUIContainer = new InlineUIContainer();
							potionUIContainer.BaselineAlignment = BaselineAlignment.Center;
							Image potion = new Image();
							Caching.ImageRepository ir = Caching.ImageRepository.Acquire();
							potion.Source = ir.GetBitmapImage("potionlg", String.Empty);
							Caching.ImageRepository.Release();
							switch (renderSize)
							{
								case RenderSize.ExtraLarge:
									potion.Height = 96;
									potion.Width = 96;
									break;
								default:
									potion.Height = 48;
									potion.Width = 48;
									break;
							}
							potionUIContainer.Child = potion;
							inlines.Add(potionUIContainer);
						}
						break;

					default:
						text = String.Empty;
						break;
				}
				match = tagMatch.Match(text);
			}
			if (!String.IsNullOrEmpty(text))
				inlines.Add(new Run(text));
			if (textBlockTemp != null)
				elements.Add(textBlockTemp);
			return elements;
		}

		public static String Ordinal(int number)
		{
			switch (number % 100)
			{
				case 11:
				case 12:
				case 13:
					return number.ToString() + "th";
			}

			switch (number % 10)
			{
				case 1:
					return number.ToString() + "st";
				case 2:
					return number.ToString() + "nd";
				case 3:
					return number.ToString() + "rd";
				default:
					return number.ToString() + "th";
			}
		}

		public static void Log(String filename, String line)
		{
			try
			{
				if (!Directory.Exists(Path.GetDirectoryName(filename)))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(filename));
				}
				using (StreamWriter sw = new StreamWriter(filename, true))
				{
					sw.WriteLine(line);
				}
			}
			catch (IOException ioe) { }
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		internal static void LogClear(String filename)
		{
			try
			{
				if (!Directory.Exists(Path.GetDirectoryName(filename)))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(filename));
				}
				if (File.Exists(filename))
					File.Delete(filename);
			}
			catch (IOException ioe) { }
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
	}

	public class VersionInfo
	{
		public Boolean IsVersionValid = false;
		public Version Version = new Version();
		public Uri Url = null;
		public Uri FileUrl = null;
		public Boolean IsNewerThan(Version currentVersion)
		{
			if (Version.Major == 0 && Version.Minor == 0 && Version.Build == 0 && Version.Revision == 0)
				return false;

			Version curVersion = new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build, Version.Revision);
			if (curVersion < Version)
				return true;

			return false;
		}
	}

	public class VersionChecker
	{
		public static VersionInfo GetLatestVersion()
		{
			VersionInfo latestVersion = new VersionInfo();
			String xmlURL = "http://dominion.technowall.net/files/version.xml";
			try
			{
				XmlDocument xDoc = new XmlDocument();
				xDoc.Load(xmlURL);

				XmlNode xnHead = xDoc.SelectSingleNode("Dominion.NET");
				if (xnHead != null)
				{
					XmlNode xnVersion = xnHead.SelectSingleNode("version");
					if (xnVersion != null)
					{
						latestVersion.Version = new Version(xnVersion.InnerText);
						latestVersion.IsVersionValid = true;
					}

					XmlNode xnUrl = xnHead.SelectSingleNode("url");
					if (xnUrl != null)
						latestVersion.Url = new Uri(xnUrl.InnerText);

					XmlNode xnFileUrl = xnHead.SelectSingleNode("fileurl");
					if (xnFileUrl != null)
						latestVersion.FileUrl = new Uri(xnFileUrl.InnerText);
				}
			}
			catch (System.Net.WebException wex)
			{
				// We'll just silently ignore these errors
			}
			catch (Exception ex)
			{
				DominionBase.Utilities.Logging.LogError(ex);
			}
			return latestVersion;
		}
	}

	public struct ColorHls
	{
		public double H;
		public double L;
		public double S;
		public double A;
	}

	public class HLSColor
	{
		public static ColorHls RgbToHls(Color rgbColor)
		{
			// Initialize result
			var hlsColor = new ColorHls();

			// Convert RGB values to percentages
			double r = (double)rgbColor.R / 255;
			var g = (double)rgbColor.G / 255;
			var b = (double)rgbColor.B / 255;
			var a = (double)rgbColor.A / 255;

			// Find min and max RGB values
			var min = Math.Min(r, Math.Min(g, b));
			var max = Math.Max(r, Math.Max(g, b));
			var delta = max - min;

			/* If max and min are equal, that means we are dealing with 
			 * a shade of gray. So we set H and S to zero, and L to either
			 * max or min (it doesn't matter which), and  then we exit. */

			//Special case: Gray
			if (max == min)
			{
				hlsColor.H = 0;
				hlsColor.S = 0;
				hlsColor.L = max;
				return hlsColor;
			}

			/* If we get to this point, we know we don't have a shade of gray. */

			// Set L
			hlsColor.L = (min + max) / 2;

			// Set S
			if (hlsColor.L < 0.5)
			{
				hlsColor.S = delta / (max + min);
			}
			else
			{
				hlsColor.S = delta / (2.0 - max - min);
			}

			// Set H
			if (r == max) hlsColor.H = (g - b) / delta;
			if (g == max) hlsColor.H = 2.0 + (b - r) / delta;
			if (b == max) hlsColor.H = 4.0 + (r - g) / delta;
			hlsColor.H *= 60;
			if (hlsColor.H < 0) hlsColor.H += 360;

			// Set A
			hlsColor.A = a;

			// Set return value
			return hlsColor;

		}

		/// <summary>
		/// Converts a WPF HSL color to an RGB color
		/// </summary>
		/// <param name="hslColor">The HSL color to convert.</param>
		/// <returns>An RGB color object equivalent to the HSL color object passed in.</returns>
		public static Color HlsToRgb(ColorHls hlsColor)
		{
			return HlsToRgb(hlsColor.H, hlsColor.L, hlsColor.S, hlsColor.A);
		}

		/// <summary>
		/// Converts a WPF HSL color to an RGB color
		/// </summary>
		/// <param name="H">The Hue to convert</param>
		/// <param name="L">The Luminosity to convert</param>
		/// <param name="S">The Saturation to convert</param>
		/// <param name="A">The Alpha value to convert</param>
		/// <returns>An RGB color object equivalent to the HSL color object passed in.</returns>
		public static Color HlsToRgb(double H, double L, double S, double A)
		{
			// Initialize result
			var rgbColor = new Color();

			Debug.Assert(H >= 0);
			Debug.Assert(H <= 360);

			/* If S = 0, that means we are dealing with a shade 
			 * of gray. So, we set R, G, and B to L and exit. */

			// Special case: Gray
			if (S == 0)
			{
				rgbColor.R = (byte)(L * 255);
				rgbColor.G = (byte)(L * 255);
				rgbColor.B = (byte)(L * 255);
				rgbColor.A = (byte)(A * 255);
				return rgbColor;
			}

			double t1;
			if (L < 0.5)
			{
				t1 = L * (1.0 + S);
			}
			else
			{
				t1 = L + S - (L * S);
			}

			var t2 = 2.0 * L - t1;

			// Convert H from degrees to a percentage
			var h = H / 360;

			// Set colors as percentage values
			var tR = h + (1.0 / 3.0);
			var r = SetColor(t1, t2, tR);

			var tG = h;
			var g = SetColor(t1, t2, tG);

			var tB = h - (1.0 / 3.0);
			var b = SetColor(t1, t2, tB);

			// Assign colors to Color object
			rgbColor.R = (byte)(r * 255);
			rgbColor.G = (byte)(g * 255);
			rgbColor.B = (byte)(b * 255);
			rgbColor.A = (byte)(A * 255);

			// Set return value
			return rgbColor;
		}

		#region Utility Methods

		/// <summary>
		/// Used by the HSL-to-RGB converter.
		/// </summary>
		/// <param name="t1">A temporary variable.</param>
		/// <param name="t2">A temporary variable.</param>
		/// <param name="t3">A temporary variable.</param>
		/// <returns>An RGB color value, in decimal format.</returns>
		private static double SetColor(double t1, double t2, double t3)
		{
			if (t3 < 0) t3 += 1.0;
			if (t3 > 1) t3 -= 1.0;

			double color;
			if (6.0 * t3 < 1)
			{
				color = t2 + (t1 - t2) * 6.0 * t3;
			}
			else if (2.0 * t3 < 1)
			{
				color = t1;
			}
			else if (3.0 * t3 < 2)
			{
				color = t2 + (t1 - t2) * ((2.0 / 3.0) - t3) * 6.0;
			}
			else
			{
				color = t2;
			}

			// Set return value
			return color;
		}

		#endregion
	}

	//public sealed class TreeViewBehavior
	//{
	//    public static DependencyProperty IsTransparentProperty = DependencyProperty.Register("IsTransparent",
	//        typeof(Boolean), typeof(TreeViewBehavior), 
	//        new FrameworkPropertyMetadata(false, new PropertyChangedCallback(IsTransparent_PropertyChanged)));
	//    public Boolean GetIsTransparent(TreeViewItem element)
	//    {
	//        if (element == null)
	//            throw new ArgumentNullException("element");
	//        return (Boolean)element.GetValue(IsTransparentProperty);
	//    }
	//    public void SetIsTransparent(TreeViewItem element, Boolean value)
	//    {
	//        if (element == null)
	//            throw new ArgumentNullException("element");
	//        element.SetValue(IsTransparentProperty, value);
	//    }
	//    public static void IsTransparent_PropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
	//    {
	//        TreeViewItem tvi = sender as TreeViewItem;
	//        if ((Boolean)e.NewValue)
	//        {
	//            tvi.Selected += tvi_Selected;
	//        }
	//        else
	//        {
	//            tvi.Selected -= tvi_Selected;
	//        }
	//    }

	//    public static void tvi_Selected(object sender, RoutedEventArgs e)
	//    {
	//        TreeViewItem tvi = sender as TreeViewItem;
	//        if (tvi == null || !tvi.IsSelected)
	//            return;
	//        tvi.Dispatcher.Invoke((Action)delegate() { tvi.IsSelected = false; }, System.Windows.Threading.DispatcherPriority.Send, tvi);
	//    }
	//}
}
