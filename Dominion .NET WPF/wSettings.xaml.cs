using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
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

using Dominion.NET_WPF.Controls;

namespace Dominion.NET_WPF
{
	/// <summary>
	/// Interaction logic for wSettings.xaml
	/// </summary>
	public partial class wSettings : Window
	{
		private Settings _MasterSettings = null;
		private Settings _LocalSettings = null;
		public Settings Settings { get { return _LocalSettings; } private set { _LocalSettings = value; } }
		public DominionBase.Cards.CardCollection Cards = new DominionBase.Cards.CardCollection();
		public Dictionary<DominionBase.Cards.Group, int> Groups = new Dictionary<DominionBase.Cards.Group, int>();
		public Dictionary<DominionBase.Cards.Cost, int> Costs = new Dictionary<DominionBase.Cards.Cost, int>();

		public wSettings(ref Settings settings)
		{
			this._MasterSettings = settings;
			this.Settings = new NET_WPF.Settings(settings);

			this.DataContext = this.Settings;

			#region Card & Group generation
			this.Cards = DominionBase.Cards.CardCollection.GetAllCards(c => c.Location == DominionBase.Cards.Location.Kingdom);
			this.Cards.Sort(delegate(DominionBase.Cards.Card c1, DominionBase.Cards.Card c2) { return c1.Name.CompareTo(c2.Name); });

			foreach (DominionBase.Cards.Card card in this.Cards)
			{
				if (!this.Costs.ContainsKey(card.BaseCost))
					this.Costs[card.BaseCost] = 0;
				this.Costs[card.BaseCost]++;

				foreach (DominionBase.Cards.Group group in Enum.GetValues(typeof(DominionBase.Cards.Group)))
				{
					if (group == DominionBase.Cards.Group.Basic || group == DominionBase.Cards.Group.None)
						continue;
					if ((card.GroupMembership & group) == group)
					{
						if (!Groups.ContainsKey(group))
							Groups[group] = 0;
						Groups[group]++;
					}
				}
			}
			#endregion

			InitializeComponent();

			#region Tab #1 -- Players
			slidNumPlayers.Value = this.Settings.NumberOfPlayers;
			slidHumanPlayers.Value = this.Settings.NumberOfHumanPlayers;

			IEnumerable<Type> aiTypes = DominionBase.Players.PlayerCollection.GetAllAIs();
			IEnumerable<ucPlayerSettings> playerSettings = spPlayers.Children.OfType<ucPlayerSettings>();
			for (int count = 0; count < this.Settings.PlayerSettings.Count; count++)
			{
				playerSettings.ElementAt(count).AITypes = aiTypes;
				playerSettings.ElementAt(count).PlayerSettings = this.Settings.PlayerSettings[count];
			}

            // human players used to be limited to 1
            slidHumanPlayers.Maximum = slidNumPlayers.Value;

            slidNumPlayers_ValueChanged(slidNumPlayers, null);
			slidHumanPlayers_ValueChanged(slidHumanPlayers, null);

			lbAISelection.DataContext = new ViewModel.AIListViewModel(aiTypes.Where(t => t != typeof(DominionBase.Players.AI.RandomAI)), settings.RandomAI_AllowedAIs);
			lbAISelection.SetBinding(ListBox.ItemsSourceProperty, new Binding("AIs"));
			#endregion

			#region Tab #2 -- Automation
			if (this.Settings.AutoPlayTreasures_LoanFirst)
				rbChooser_AutomaticallyPlayTreasuresLoanBeforeVenture.IsChecked = true;
			else
				rbChooser_AutomaticallyPlayTreasuresVentureBeforeLoan.IsChecked = true;
			if (this.Settings.AutoPlayTreasures_HornOfPlentyFirst)
				rbChooser_AutomaticallyPlayTreasuresHornOfPlentyBeforeBank.IsChecked = true;
			else
				rbChooser_AutomaticallyPlayTreasuresBankBeforeHornOfPlenty.IsChecked = true;
			#endregion

			#region Tab #3 -- Interface
			cbLayoutStyle.SelectedValue = this.Settings.LayoutStyle;
			cbGameLogLocation.SelectedValue = this.Settings.GameLogLocation;

			slidToolTipDuration_ValueChanged(slidToolTipDuration, null);
			#endregion

			#region Tab #4 -- Kingdom Card Setup
			cbUsePreset.IsChecked = this.Settings.UsePreset;
			cbShowPresetCards.IsChecked = this.Settings.Settings_ShowPresetCards;

			cbUsePreset_Checked(cbUsePreset, null);
			cbShowPresetCards_Checked(cbShowPresetCards, null);

			ucccConstraints.ConstraintCollection = this.Settings.Constraints;

			cbPresets.ItemsSource = this.Settings.Presets;
			cbPresets.SelectedItem = this.Settings.Presets.SingleOrDefault(p => p.Name == this.Settings.PresetName);
			#endregion

			#region Tab #5 -- Card Settings
			foreach (DominionBase.Cards.CardsSettings cardsSettings in this.Settings.CardSettings)
				icCardSettings.Items.Add(new ucCardSettings { CardsSettings = cardsSettings });
			#endregion

			#region Tab #6 -- Set & Group Information
			IEnumerable<DominionBase.Cards.Source> sets = Enum.GetValues(typeof(DominionBase.Cards.Source)).Cast<DominionBase.Cards.Source>();
			cbSet.ItemsSource = sets.Where(s => s != DominionBase.Cards.Source.All);

			IEnumerable<DominionBase.Cards.Category> categories = Enum.GetValues(typeof(DominionBase.Cards.Category)).Cast<DominionBase.Cards.Category>();
			cbCategory.ItemsSource = categories.Where(c => c != DominionBase.Cards.Category.Unknown && this.Cards.Any(card => (card.Category & c) == c));

			cbGroup.ItemsSource = this.Groups.OrderBy(kvp => (int)kvp.Key);
			#endregion
		}

		private void slidNumPlayers_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Slider slider = sender as Slider;
			if (tbPlayersNum != null)
				tbPlayersNum.Text = slider.Value.ToString();
			if (this.Settings != null)
			{
				if (slidHumanPlayers != null)
				{
					this.Settings.NumberOfPlayers = (int)slider.Value;

					if (textBox1 != null)
					{
						if (slider.Value == 1 && textBox1.Text.EndsWith("s"))
							textBox1.Text = textBox1.Text.Remove(textBox1.Text.Length - 1);
						if (slider.Value != 1 && !textBox1.Text.EndsWith("s"))
							textBox1.Text += "s";
					}

					slidHumanPlayers.Value = this.Settings.NumberOfHumanPlayers;
					slidHumanPlayers.Maximum = this.Settings.NumberOfPlayers;

                    // human players used to be limited to 1
                    slidHumanPlayers.Maximum = slidNumPlayers.Value;
                }
			}
		}

		private void slidHumanPlayers_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Slider slider = sender as Slider;
			if (tbPlayersHuman != null)
				tbPlayersHuman.Text = slider.Value.ToString();
			if (this.Settings != null)
			{
				if (textBox2 != null)
				{
					this.Settings.NumberOfHumanPlayers = (int)slider.Value;
					if (slider.Value == 1 && textBox2.Text.EndsWith("s"))
						textBox2.Text = textBox2.Text.Remove(textBox2.Text.Length - 1);
					if (slider.Value != 1 && !textBox2.Text.EndsWith("s"))
						textBox2.Text += "s";
				}
				if (spPlayers != null)
				{
					for (int count = 0; count < this.Settings.PlayerSettings.Count; count++)
					{
						DominionBase.Players.PlayerType pt = DominionBase.Players.PlayerType.Computer;
						if (this.Settings.NumberOfHumanPlayers > count)
							pt = DominionBase.Players.PlayerType.Human;
						spPlayers.Children.OfType<ucPlayerSettings>().ElementAt(count).PlayerType = pt;
					}
				}
			}
		}

		private void bOk_Click(object sender, RoutedEventArgs e)
		{
			IEnumerable<ucPlayerSettings> ucPSs = spPlayers.Children.OfType<ucPlayerSettings>();
			for (int count = 0; count < this.Settings.PlayerSettings.Count; count++)
			{
				this.Settings.PlayerSettings[count] = ucPSs.ElementAt(count).PlayerSettings;
			}
			this.Settings.RandomAI_AllowedAIs.Clear();
			this.Settings.RandomAI_AllowedAIs.AddRange((lbAISelection.DataContext as ViewModel.AIListViewModel).AIs.Where(avm => avm.IsChecked).Select(avm => avm.AI.FullName));

			this.Settings.LayoutStyle = (LayoutStyle)cbLayoutStyle.SelectedValue;
			this.Settings.GameLogLocation = (GameLogLocation)cbGameLogLocation.SelectedValue;
			this._MasterSettings.CopyFrom(this.Settings);
			this.DialogResult = true;
			this.Close();
		}

		private void cbUsePreset_Checked(object sender, RoutedEventArgs e)
		{
			this.Settings.UsePreset = (cbUsePreset.IsChecked == true);
			if (this.Settings.UsePreset)
			{
				gbCardConstraints.IsEnabled = false;
				gbCardConstraints.ToolTip = "This section is disabled if you're using a preset";
			}
			else
			{
				gbCardConstraints.IsEnabled = true;
				gbCardConstraints.ToolTip = null;
			}
		}

		private void cbShowPresetCards_Checked(object sender, RoutedEventArgs e)
		{
			this.Settings.Settings_ShowPresetCards = (cbShowPresetCards.IsChecked == true);
			cbPresets_SelectionChanged(cbPresets, null);
		}

		private void slidToolTipDuration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Slider slider = sender as Slider;
			if (tbToolTipDuration != null)
			{
				tbToolTipDuration.Text = ((int)this.Settings.ToolTipShowDuration / 1000).ToString();
				if (this.Settings.ToolTipShowDuration == ToolTipShowDuration.Off)
					tbToolTipExtra.Text = "(Off)";
				else
					tbToolTipExtra.Text = String.Empty;
			}
		}

		private void cbToolTipClick_Checked(object sender, RoutedEventArgs e)
		{
			if ((sender as CheckBox).IsChecked == true)
				slidToolTipDuration.Value = 1;
		}

		private void cbPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			DominionBase.Cards.Preset selectedPreset = (sender as ComboBox).SelectedItem as DominionBase.Cards.Preset;
			if (selectedPreset != null)
				this.Settings.PresetName = selectedPreset.Name;

			if (selectedPreset == null || !this.Settings.Settings_ShowPresetCards)
			{
				olCardsUsed.Objects = null;
				olCardsUsed.Visibility = System.Windows.Visibility.Collapsed;
			}
			else
			{

				List<Object> cardTokenObjects = new List<Object>(selectedPreset.Cards.OrderBy(card => card.Name));
				foreach (DominionBase.Cards.Card specialCard in selectedPreset.CardCards.Keys)
				{
					if (specialCard.CardType == DominionBase.Cards.Cornucopia.TypeClass.YoungWitch)
					{
						cardTokenObjects.Add(null);
						cardTokenObjects.Add(new DominionBase.Cards.Cornucopia.BaneToken());
						cardTokenObjects.AddRange(selectedPreset.CardCards[specialCard]);
					}
				}

				olCardsUsed.Objects = cardTokenObjects;
				olCardsUsed.Visibility = System.Windows.Visibility.Visible;
			}
		}

		private void rbChooser_AutomaticallyPlayTreasuresLoanBeforeVenture_Checked(object sender, RoutedEventArgs e)
		{
			this.Settings.AutoPlayTreasures_LoanFirst = true;
		}

		private void rbChooser_AutomaticallyPlayTreasuresVentureBeforeLoan_Checked(object sender, RoutedEventArgs e)
		{
			this.Settings.AutoPlayTreasures_LoanFirst = false;
		}

		private void rbChooser_AutomaticallyPlayTreasuresHornOfPlentyBeforeBank_Checked(object sender, RoutedEventArgs e)
		{
			this.Settings.AutoPlayTreasures_HornOfPlentyFirst = true;
		}

		private void rbChooser_AutomaticallyPlayTreasuresBankBeforeHornOfPlenty_Checked(object sender, RoutedEventArgs e)
		{
			this.Settings.AutoPlayTreasures_HornOfPlentyFirst = false;
		}

		private void cbSet_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox cb = sender as ComboBox;
			if (cb.SelectedItem != null)
			{
				cbCategory.SelectedItem = null;
				cbGroup.SelectedItem = null;

				DominionBase.Cards.Constraint constraint = new DominionBase.Cards.Constraint(DominionBase.Cards.ConstraintType.SetIs, cb.SelectedItem, 0, 10);
				IEnumerable<DominionBase.Cards.Card> cards = constraint.GetMatchingCards(this.Cards);
				cccSetCategoryGroupDisplay.Pile = cards;

				gbSetCategoryGroupDisplay.Header = String.Format("Cards where Set is {0}", cb.SelectedItem);
				tbMatchingCount.Text = cards.Count().ToString();
			}
		}

		private void cbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox cb = sender as ComboBox;
			if (cb.SelectedItem != null)
			{
				cbSet.SelectedItem = null;
				cbGroup.SelectedItem = null;

				DominionBase.Cards.Constraint constraint = new DominionBase.Cards.Constraint(DominionBase.Cards.ConstraintType.CategoryContains, cb.SelectedItem, 0, 10);
				IEnumerable<DominionBase.Cards.Card> cards = constraint.GetMatchingCards(this.Cards);
				cccSetCategoryGroupDisplay.Pile = cards;

				gbSetCategoryGroupDisplay.Header = String.Format("Cards where Category has {0} in it", cb.SelectedItem);
				tbMatchingCount.Text = cards.Count().ToString();
			}
		}

		private void cbGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox cb = sender as ComboBox;
			if (cb.SelectedItem != null)
			{
				cbSet.SelectedItem = null;
				cbCategory.SelectedItem = null;

				DominionBase.Cards.Group group = ((KeyValuePair<DominionBase.Cards.Group, int>)cb.SelectedItem).Key;
				DominionBase.Cards.Constraint constraint = new DominionBase.Cards.Constraint(DominionBase.Cards.ConstraintType.MemberOfGroup, group, 0, 10);
				IEnumerable<DominionBase.Cards.Card> cards = constraint.GetMatchingCards(this.Cards);
				cccSetCategoryGroupDisplay.Pile = cards;

				gbSetCategoryGroupDisplay.Header = String.Format("Cards that are a member of Group: {0}", group.ToDescription());
				tbMatchingCount.Text = cards.Count().ToString();
			}
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

		private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
		}

		private void smallCardIconBrowse_Click(object sender, RoutedEventArgs e)
		{
			Ookii.Dialogs.Wpf.VistaFolderBrowserDialog dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
			dialog.RootFolder = Environment.SpecialFolder.MyPictures;
			dialog.SelectedPath = this.Settings.CustomImagesPathSmall;
			if (!System.IO.Path.IsPathRooted(dialog.SelectedPath))
				dialog.SelectedPath = System.IO.Path.Combine(Caching.ImageRepository.ImageRoot, dialog.SelectedPath);
			dialog.ShowNewFolderButton = false;
			if (dialog.ShowDialog() == true)
			{
				// I don't like doing it this way, but I'm stumped as to how to get the TextBox to update
				// its Text property property using the commented-out line here.  The Binding doesn't seem
				// to work the way I thought it should
				//this.Settings.CustomImagesPathSmall = dialog.SelectedPath;
				tbCustomImagesPathSmall.Text = dialog.SelectedPath;
			}
		}

		private void mediumCardIconBrowse_Click(object sender, RoutedEventArgs e)
		{
			Ookii.Dialogs.Wpf.VistaFolderBrowserDialog dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
			dialog.RootFolder = Environment.SpecialFolder.MyPictures;
			dialog.SelectedPath = this.Settings.CustomImagesPathMedium;
			if (!System.IO.Path.IsPathRooted(dialog.SelectedPath))
				dialog.SelectedPath = System.IO.Path.Combine(Caching.ImageRepository.ImageRoot, dialog.SelectedPath);
			dialog.ShowNewFolderButton = false;
			if (dialog.ShowDialog() == true)
			{
				// I don't like doing it this way, but I'm stumped as to how to get the TextBox to update
				// its Text property property using the commented-out line here.  The Binding doesn't seem
				// to work the way I thought it should
				//this.Settings.CustomImagesPathMedium = dialog.SelectedPath;
				tbCustomImagesPathMedium.Text = dialog.SelectedPath;
			}
		}

		private void cardToolTipBrowse_Click(object sender, RoutedEventArgs e)
		{
			Ookii.Dialogs.Wpf.VistaFolderBrowserDialog dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
			dialog.RootFolder = Environment.SpecialFolder.MyPictures;
			dialog.SelectedPath = this.Settings.CustomToolTipsPath;
			if (!System.IO.Path.IsPathRooted(dialog.SelectedPath))
				dialog.SelectedPath = System.IO.Path.Combine(Caching.ImageRepository.ImageRoot, dialog.SelectedPath);
			dialog.ShowNewFolderButton = false;
			if (dialog.ShowDialog() == true)
			{
				// I don't like doing it this way, but I'm stumped as to how to get the TextBox to update
				// its Text property property using the commented-out line here.  The Binding doesn't seem
				// to work the way I thought it should
				//this.Settings.CustomToolTipsPath = dialog.SelectedPath;
				tbCustomToolTipsPath.Text = dialog.SelectedPath;
			}
		}

		private void cbFullCardView_Checked(object sender, RoutedEventArgs e)
		{
			switch (((CheckBox)sender).IsChecked)
			{
				case true:
					cccSetCategoryGroupDisplay.CardSize = CardSize.Full;
					break;

				default:
					cccSetCategoryGroupDisplay.CardSize = CardSize.Text;
					break;
			}
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
	}

	[ValueConversion(typeof(double?), typeof(bool))]
	public class PlayerDisplayConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			if ((double)value >= int.Parse((string)parameter))
				return true;
			return false;
		}
		public object ConvertBack(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class AITypeConverterName : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			if (value is Type && ((Type)value == typeof(DominionBase.Players.AI.Basic) || ((Type)value).IsSubclassOf(typeof(DominionBase.Players.AI.Basic))))
			{
				return (String)((Type)value).GetProperty("AIName", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy).GetValue(null, null);
			}
			else if (value is Type && ((Type)value == typeof(DominionBase.Players.AI.RandomAI) || ((Type)value).IsSubclassOf(typeof(DominionBase.Players.AI.RandomAI))))
			{
				return (String)((Type)value).GetProperty("AIName", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy).GetValue(null, null);
			}
			return value.ToString();
		}
		public object ConvertBack(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			return null;
		}
	}

	public class AITypeConverterDescription : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			if (value is Type && ((Type)value == typeof(DominionBase.Players.AI.Basic) || ((Type)value).IsSubclassOf(typeof(DominionBase.Players.AI.Basic))))
			{
				return (String)((Type)value).GetProperty("AIDescription", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy).GetValue(null, null);
			}
			else if (value is Type && ((Type)value == typeof(DominionBase.Players.AI.RandomAI) || ((Type)value).IsSubclassOf(typeof(DominionBase.Players.AI.RandomAI))))
			{
				return (String)((Type)value).GetProperty("AIDescription", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy).GetValue(null, null);
			}
			return value.ToString();
		}
		public object ConvertBack(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			return null;
		}
	}

	public class ToolTipShowDurationConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			switch ((ToolTipShowDuration)value)
			{
				case ToolTipShowDuration.Off: return 1;
				case ToolTipShowDuration.Short: return 2;
				case ToolTipShowDuration.Normal: return 3;
				case ToolTipShowDuration.Long: return 4;
				case ToolTipShowDuration.SuperLong: return 5;
			}
			return 3;
		}
		public object ConvertBack(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			switch (System.Convert.ToInt32(value))
			{
				case 1: return ToolTipShowDuration.Off;
				case 2: return ToolTipShowDuration.Short;
				case 3: return ToolTipShowDuration.Normal;
				case 4: return ToolTipShowDuration.Long;
				case 5: return ToolTipShowDuration.SuperLong;
			}
			return ToolTipShowDuration.Normal;
		}
	}
}
