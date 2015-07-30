using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
	/// Interaction logic for wCardViewer.xaml
	/// </summary>
	public partial class wCardViewer : Window
	{
		private DominionBase.Cards.CardCollection AllCards = null;
		public wCardViewer()
		{
			InitializeComponent();

			this.AllCards = DominionBase.Cards.CardCollection.GetAllCards(c => c.Location != DominionBase.Cards.Location.Invisible);
			this.AllCards.Sort(delegate(DominionBase.Cards.Card c1, DominionBase.Cards.Card c2) { return c1.Name.CompareTo(c2.Name); });
			cbCards.ItemsSource = this.AllCards;
			SourceContainerList sources = new SourceContainerList(this.AllCards.Select(c => c.Source).Distinct().OrderBy(s => s.ToString()));
			cbSets.ItemsSource = sources;
		}

		private void Window_Close_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void cbCards_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ttcCard.ICard = (sender as ComboBox).SelectedItem as DominionBase.ICard;
			if (ttcCard.ICard == null)
				ttcCard.Visibility = System.Windows.Visibility.Hidden;
			else
				ttcCard.Visibility = System.Windows.Visibility.Visible;
			ttcCard.InvalidateVisual();
		}

		private void cbSets_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (((ComboBox)sender).SelectedItem == null)
			{
				cbCards.ItemsSource = null;
				return;
			}
			SourceContainer selectedSource = (SourceContainer)((ComboBox)sender).SelectedItem;
			cbCards.ItemsSource = this.AllCards.Where(c => selectedSource.Source == DominionBase.Cards.Source.All || c.Source == selectedSource.Source);
			cbCards_SelectionChanged(cbCards, null);
		}
	}

	public class SourceContainer
	{
		public BitmapImage Image { get; set; }
		public DominionBase.Cards.Source Source { get; set; }
		public SourceContainer(DominionBase.Cards.Source source)
		{
			this.Source = source;
			Caching.ImageRepository repo = Caching.ImageRepository.Acquire();

			this.Image = repo.GetBitmapImage(this.Source.ToString(), String.Empty);

			Caching.ImageRepository.Release();
		}
	}

	public class SourceContainerList : List<SourceContainer>
	{
		public SourceContainerList(IEnumerable<DominionBase.Cards.Source> elements)
		{
			foreach (DominionBase.Cards.Source element in elements)
				this.Add(new SourceContainer(element));
		}
	}
}
