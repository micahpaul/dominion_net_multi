using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Dominion.NET_WPF.Controls.GameLog
{
	/// <summary>
	/// Interaction logic for ucGameMessage.xaml
	/// </summary>
	public partial class ucGameMessage : LogSection
	{
		public ucGameMessage()
		{
			InitializeComponent();
			this.spContainer = spArea;
		}

		public override Boolean IsExpanded { get { return expTurn.IsExpanded; } set { expTurn.IsExpanded = value; } }

		public override void New(String title)
		{
			DockPanel dp = new DockPanel();
			TextBlock tbTitle = new TextBlock();
			tbTitle.Text = title;
			DockPanel.SetDock(tbTitle, Dock.Left);
			dp.Children.Add(tbTitle);
			expTurn.Header = dp;
		}

		public override void TearDown()
		{
			base.TearDown();

			spArea.Children.Clear();
		}

		public override void Log(params Object[] items)
		{
			StringBuilder sbFullLine = new StringBuilder();

			DockPanel dp = new DockPanel();

			SolidColorBrush background = Brushes.Transparent;
			SolidColorBrush foreground = Brushes.Transparent;
			foreach (Object item in items)
			{
				if (item is Color)
				{
					foreground = new SolidColorBrush((Color)item);
				}
				else if (item is String)
				{
					if (!String.IsNullOrEmpty((String)item))
					{
						Tuple<List<UIElement>, String> itemTuple = GenerateElements((String)item, background, foreground);
						foreach (UIElement elem in itemTuple.Item1)
							dp.Children.Add(elem);

						sbFullLine.Append(itemTuple.Item2);
					}
				}
				else if (item is int)
				{
					Tuple<List<UIElement>, String> itemTuple = GenerateElements((int)item, background, foreground);
					foreach (UIElement elem in itemTuple.Item1)
						dp.Children.Add(elem);

					sbFullLine.Append(itemTuple.Item2);
				}
				else if (item is DominionBase.Players.Player)
				{
					Tuple<List<UIElement>, String> itemTuple = GenerateElements((DominionBase.Players.Player)item, background, foreground);
					foreach (UIElement elem in itemTuple.Item1)
						dp.Children.Add(elem);

					sbFullLine.Append(itemTuple.Item2);
				}
				else if (item is DominionBase.Players.PlayerCollection)
				{
					Boolean isFirstItem = true;
					foreach (DominionBase.Players.Player player in (DominionBase.Players.PlayerCollection)item)
					{
						Tuple<List<UIElement>, String> itemTuple;
						if (!isFirstItem)
						{
							itemTuple = GenerateElements(", ", background, foreground);
							foreach (UIElement elem in itemTuple.Item1)
								dp.Children.Add(elem);

							sbFullLine.Append(itemTuple.Item2);
						}

						itemTuple = GenerateElements(player, background, foreground);
						foreach (UIElement elem in itemTuple.Item1)
							dp.Children.Add(elem);

						sbFullLine.Append(itemTuple.Item2);

						isFirstItem = false;
					}

				}
				else if (item is DominionBase.Visual.VisualPlayer)
				{
					Tuple<List<UIElement>, String> itemTuple = GenerateElements((DominionBase.Visual.VisualPlayer)item, background, foreground);
					foreach (UIElement elem in itemTuple.Item1)
						dp.Children.Add(elem);

					sbFullLine.Append(itemTuple.Item2);
				}
				else if (item is DominionBase.ICard)
				{
					Tuple<List<UIElement>, String> itemTuple = GenerateElements((DominionBase.ICard)item, background, foreground);
					foreach (UIElement elem in itemTuple.Item1)
						dp.Children.Add(elem);

					sbFullLine.Append(itemTuple.Item2);
				}
				else if (item is IEnumerable<DominionBase.ICard>)
				{
					Tuple<List<UIElement>, String> itemTuple = GenerateElements((IEnumerable<DominionBase.ICard>)item, background, foreground);
					foreach (UIElement elem in itemTuple.Item1)
						dp.Children.Add(elem);

					sbFullLine.Append(itemTuple.Item2);
				}
			}

			TextBlock g = new TextBlock();
			g.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
			g.Background = background;
			DockPanel.SetDock(g, Dock.Left);
			dp.Children.Add(g);

			Utilities.Log(this.LogFile, sbFullLine.ToString());
			spArea.Children.Add(dp);
			dp.BringIntoView();
		}

		private Tuple<List<UIElement>, String> GenerateElements(String item, SolidColorBrush background, SolidColorBrush foreground)
		{
			List<UIElement> elements = new List<UIElement>();
			foreach (UIElement elem in Utilities.RenderText(item as String, NET_WPF.RenderSize.Tiny, true))
			{
				if (elem is TextBlock)
				{
					((TextBlock)elem).Background = background;
					((TextBlock)elem).Foreground = foreground.Color.A == 0 ? Brushes.Black : foreground;
				}

				DockPanel.SetDock(elem, Dock.Left);
				elements.Add(elem);
			}

			return new Tuple<List<UIElement>, String>(elements, item.Replace("<u>", "*").Replace("</u>", "*"));
		}

		private Tuple<List<UIElement>, String> GenerateElements(int item, SolidColorBrush background, SolidColorBrush foreground)
		{
			List<UIElement> elements = new List<UIElement>();
			TextBlock tbLine = new TextBlock();
			tbLine.Text = ((int)item).ToString();
			tbLine.Background = background;
			tbLine.FontWeight = FontWeights.Bold;
			tbLine.Foreground = foreground.Color.A == 0 ? Brushes.Crimson : foreground;

			DockPanel.SetDock(tbLine, Dock.Left);

			elements.Add(tbLine);

			return new Tuple<List<UIElement>, String>(elements, item.ToString());
		}

		private Tuple<List<UIElement>, String> GenerateElements(DominionBase.Players.Player item, SolidColorBrush background, SolidColorBrush foreground)
		{
			List<UIElement> elements = new List<UIElement>();
			TextBlock tbLine = new TextBlock();
			tbLine.Text = ((DominionBase.Players.Player)item).Name;
			tbLine.Background = background;
			tbLine.Foreground = foreground.Color.A == 0 ? Brushes.Black : foreground;
			tbLine.FontWeight = FontWeights.Bold;

			DockPanel.SetDock(tbLine, Dock.Left);

			elements.Add(tbLine);

			return new Tuple<List<UIElement>, String>(elements, item.Name);
		}

		private Tuple<List<UIElement>, String> GenerateElements(DominionBase.Visual.VisualPlayer item, SolidColorBrush background, SolidColorBrush foreground)
		{
			List<UIElement> elements = new List<UIElement>();
			TextBlock tbLine = new TextBlock();
			tbLine.Text = ((DominionBase.Visual.VisualPlayer)item).Name;
			tbLine.Background = background;
			tbLine.Foreground = foreground.Color.A == 0 ? Brushes.Black : foreground;
			tbLine.FontWeight = FontWeights.Bold;

			DockPanel.SetDock(tbLine, Dock.Left);

			elements.Add(tbLine);

			return new Tuple<List<UIElement>, String>(elements, item.Name);
		}

		private Tuple<List<UIElement>, String> GenerateElements(DominionBase.ICard item, SolidColorBrush background, SolidColorBrush foreground)
		{
			List<UIElement> elements = new List<UIElement>();
			ucCardIcon icon = CardIconUtilities.CreateCardIcon((DominionBase.ICard)item);
			icon.Background = background;
			DockPanel.SetDock(icon, Dock.Left);

			elements.Add(icon);

			return new Tuple<List<UIElement>, String>(elements, item.Name);
		}

		private Tuple<List<UIElement>, String> GenerateElements(IEnumerable<DominionBase.ICard> item, SolidColorBrush background, SolidColorBrush foreground)
		{
			List<UIElement> elements = new List<UIElement>();
			StringBuilder sbItem = new StringBuilder();

			foreach (UserControl uc in CardIconUtilities.CreateCardIcons(item))
			{
				uc.Background = background;
				DockPanel.SetDock(uc, Dock.Left);
				elements.Add(uc);

				if (uc is ucCardIcon)
				{
					if (((ucCardIcon)uc).Count == 1)
						sbItem.Append(((ucCardIcon)uc).Card.Name);
					else
						sbItem.AppendFormat("{0}x {1}", ((ucCardIcon)uc).Count, ((ucCardIcon)uc).Card.Name);

					TextBlock tbLine = new TextBlock();
					tbLine.Text = ", ";
					tbLine.Background = background;
					tbLine.Foreground = foreground.Color.A == 0 ? Brushes.Black : foreground;
					DockPanel.SetDock(tbLine, Dock.Left);
					elements.Add(tbLine);

					sbItem.Append(", ");
				}
				else if (uc is ucTokenIcon)
				{
					sbItem.AppendFormat("({0})", ((ucTokenIcon)uc).Token.LongDisplayString);
				}
			}

			if (item.Count() > 0)
			{
				elements.RemoveAt(elements.Count - 1);
				sbItem = sbItem.Remove(sbItem.Length - 2, 2);
			}

			return new Tuple<List<UIElement>, String>(elements, sbItem.ToString());
		}
	}
}
