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
using System.Windows.Shapes;

namespace Dominion.NET_WPF
{
	/// <summary>
	/// Interaction logic for wCardSelection.xaml
	/// </summary>
	public partial class wCardSelection : Window
	{
		public wCardSelection()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (this.Owner is wMain)
			{
				wpGeneralCards.Visibility = System.Windows.Visibility.Collapsed;

				IEnumerable<IGrouping<Boolean, DominionBase.Piles.Supply>> supplyGroups = ((wMain)this.Owner).game.Table.Supplies.Values.Where(s => s.Randomizer.Location == DominionBase.Cards.Location.Kingdom || s.Randomizer.Source != DominionBase.Cards.Source.All).GroupBy(s => s.Randomizer.Location == DominionBase.Cards.Location.General || s.Randomizer.Location == DominionBase.Cards.Location.Invisible);
				foreach (IGrouping<Boolean, DominionBase.Piles.Supply> supplyGroup in supplyGroups)
				{
					if (supplyGroup.Key)
					{
						cccGeneralCards.Pile = supplyGroup.Select(s => s.Randomizer);
						wpGeneralCards.Visibility = System.Windows.Visibility.Visible;
					}
					else
					{
						cccKingdomCards.TokenDict = supplyGroup.ToDictionary(s => s.Randomizer, s => s.Tokens);
						cccKingdomCards.Pile = supplyGroup.Select(s => s.Randomizer);
					}
				}
			}
		}

		private void bReshuffle_Click(object sender, RoutedEventArgs e)
		{
			if (this.Owner is wMain)
			{
				((wMain)this.Owner).game.SelectCards();
				Window_Loaded(this, null);
			}
		}

		private void bAccept_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
			this.Close();
		}

		private void svSetCategoryGroupDisplay_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			ScrollViewer sv = sender as ScrollViewer;
			bSetCategoryGroupDisplayHorizontal.Width = sv.ViewportWidth * sv.ViewportWidth / sv.ExtentWidth;
			bSetCategoryGroupDisplayVertical.Height = sv.ViewportHeight * sv.ViewportHeight / sv.ExtentHeight;

			bSetCategoryGroupDisplayHorizontal.Margin = new Thickness(sv.ViewportWidth * sv.HorizontalOffset / sv.ExtentWidth, 0, 0, 0);
			bSetCategoryGroupDisplayVertical.Margin = new Thickness(0, sv.ViewportHeight * sv.VerticalOffset / sv.ExtentHeight, 0, 0);

			bOpacityLayerLeft.Visibility = bOpacityLayerRight.Visibility = System.Windows.Visibility.Visible;
			if (bSetCategoryGroupDisplayHorizontal.Width >= sv.ViewportWidth)
				bOpacityLayerLeft.Visibility = bOpacityLayerRight.Visibility = System.Windows.Visibility.Collapsed;
			else if (bSetCategoryGroupDisplayHorizontal.Margin.Left <= 0)
				bOpacityLayerLeft.Visibility = System.Windows.Visibility.Collapsed;
			else if (bSetCategoryGroupDisplayHorizontal.Margin.Left + bSetCategoryGroupDisplayHorizontal.Width >= sv.ViewportWidth)
				bOpacityLayerRight.Visibility = System.Windows.Visibility.Collapsed;

			bOpacityLayerTop.Visibility = bOpacityLayerBottom.Visibility = System.Windows.Visibility.Visible;
			if (bSetCategoryGroupDisplayVertical.Height >= sv.ViewportHeight)
				bOpacityLayerTop.Visibility = bOpacityLayerBottom.Visibility = System.Windows.Visibility.Collapsed;
			else if (bSetCategoryGroupDisplayVertical.Margin.Top <= 0)
				bOpacityLayerTop.Visibility = System.Windows.Visibility.Collapsed;
			else if (bSetCategoryGroupDisplayVertical.Margin.Top + bSetCategoryGroupDisplayVertical.Height >= sv.ViewportHeight)
				bOpacityLayerBottom.Visibility = System.Windows.Visibility.Collapsed;
		}

		private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Grid g = (Grid)sender;
			g.CaptureMouse();
			g.Cursor = Cursors.ScrollNS;
			svSetCategoryGroupDisplay.ScrollToVerticalOffset(e.GetPosition(g).Y / g.ActualHeight * svSetCategoryGroupDisplay.ExtentHeight);
		}

		private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			Grid g = (Grid)sender;
			g.ReleaseMouseCapture();
			g.Cursor = Cursors.Arrow;
		}

		private void Grid_MouseMove(object sender, MouseEventArgs e)
		{
			Grid g = (Grid)sender;
			if (g.IsMouseCaptured)
				svSetCategoryGroupDisplay.ScrollToVerticalOffset(e.GetPosition(g).Y / g.ActualHeight * svSetCategoryGroupDisplay.ExtentHeight);
		}

		private void Grid_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
		{
			Grid g = (Grid)sender;
			g.CaptureMouse();
			g.Cursor = Cursors.ScrollWE;
			svSetCategoryGroupDisplay.ScrollToHorizontalOffset(e.GetPosition(g).X / g.ActualWidth * svSetCategoryGroupDisplay.ExtentWidth);
		}

		private void Grid_MouseMove_1(object sender, MouseEventArgs e)
		{
			Grid g = (Grid)sender;
			if (g.IsMouseCaptured)
				svSetCategoryGroupDisplay.ScrollToHorizontalOffset(e.GetPosition(g).X / g.ActualWidth * svSetCategoryGroupDisplay.ExtentWidth);
		}
	}
}
