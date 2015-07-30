using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Dominion.NET_WPF.Controls
{
	/// <summary>
	/// Interaction logic for ucCardConstraint.xaml
	/// </summary>
	public partial class ucCardConstraint : UserControl, IDisposable
	{
		DominionBase.Cards.Constraint _Constraint = null;

		public DominionBase.Cards.Constraint Constraint
		{
			get { return _Constraint; }
			set { 
				_Constraint = value;

				cbMinimum.ItemsSource = this.Counts;
				cbMinimum.SelectedItem = _Constraint.Minimum;

				cbMaximum.ItemsSource = this.Counts;
				cbMaximum.SelectedItem = _Constraint.Maximum;

				cbCriteria.SelectedItem = _Constraint.ConstraintType;
				cbValue.SelectedItem = _Constraint.ConstraintValue;
			}
		}

		public static readonly RoutedEvent RemoveClickEvent = EventManager.RegisterRoutedEvent(
			"RemoveClick",
			RoutingStrategy.Bubble,
			typeof(RoutedEventHandler),
			typeof(ucCardConstraint));

		public event RoutedEventHandler RemoveClick
		{
			add { AddHandler(RemoveClickEvent, value); }
			remove { RemoveHandler(RemoveClickEvent, value); }
		}


		private List<int> counts = new List<int>();
		public ObservableCollection<int> Counts
		{
			get
			{
				if (counts.Count == 0)
				{
					for (int i = this.Constraint.RangeMin; i <= this.Constraint.RangeMax; i++)
						counts.Add(i);
				}
				return new ObservableCollection<int>(counts);
			}
		}

		public ucCardConstraint()
		{
			InitializeComponent();
			if (wMain.Settings != null)
			{
				wMain.Settings.SettingsChanged += new NET_WPF.Settings.SettingsChangedEventHandler(Settings_SettingsChanged);
				Settings_SettingsChanged(wMain.Settings, null);
			}
		}

		public void Dispose()
		{
			if (wMain.Settings != null)
			{
				wMain.Settings.SettingsChanged -= new NET_WPF.Settings.SettingsChangedEventHandler(Settings_SettingsChanged);
			}
		}

		void Settings_SettingsChanged(object sender, SettingsChangedEventArgs e)
		{
			Settings settings = sender as Settings;
			if (settings != null)
			{
				if (settings.ToolTipShowDuration == ToolTipShowDuration.Off)
					ToolTipService.SetIsEnabled(cbValue, false);
				else
				{
					ToolTipService.SetIsEnabled(cbValue, true);
					ToolTipService.SetShowDuration(cbValue, (int)settings.ToolTipShowDuration);
				}
			}
		}

		private void cbMinimum_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (cbMinimum == null || cbMinimum.SelectedItem == null)
				return;
			this.Constraint.Minimum = (int)cbMinimum.SelectedItem;
			cbMaximum.SelectedItem = this.Constraint.Maximum;
		}

		private void cbMaximum_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (cbMaximum == null || cbMaximum.SelectedItem == null)
				return;
			this.Constraint.Maximum = (int)cbMaximum.SelectedItem;
			cbMinimum.SelectedItem = this.Constraint.Minimum;
		}

		private void cbCriteria_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (cbValue == null)
				return;

			if (e.AddedItems.Count > 0)
			{
				this.Constraint.ConstraintType = (DominionBase.Cards.ConstraintType)e.AddedItems[0];

				switch (this.Constraint.ConstraintType)
				{
					case DominionBase.Cards.ConstraintType.Unknown:
						break;

					case DominionBase.Cards.ConstraintType.SetIs:
						cbValue.ItemsSource = DisplayObjects.Sources;
						if (this.Constraint.ConstraintValue is DominionBase.Cards.Source)
						{
							foreach (KeyValuePair<DominionBase.Cards.Source, int> kvpSource in cbValue.Items.OfType<KeyValuePair<DominionBase.Cards.Source, int>>())
							{
								if (kvpSource.Key == (DominionBase.Cards.Source)this.Constraint.ConstraintValue)
								{
									cbValue.SelectedItem = kvpSource;
									break;
								}
							}
						}
						cbValue.Focus();
						break;

					case DominionBase.Cards.ConstraintType.CategoryIs:
						cbValue.ItemsSource = DisplayObjects.CategoriesExact;
						if (this.Constraint.ConstraintValue is DominionBase.Cards.Category)
						{
							foreach (KeyValuePair<DominionBase.Cards.Category, int> kvpCategory in cbValue.Items.OfType<KeyValuePair<DominionBase.Cards.Category, int>>())
							{
								if (kvpCategory.Key == (DominionBase.Cards.Category)this.Constraint.ConstraintValue)
								{
									cbValue.SelectedItem = kvpCategory;
									break;
								}
							}
						}
						cbValue.Focus();
						break;

					case DominionBase.Cards.ConstraintType.CategoryContains:
						cbValue.ItemsSource = DisplayObjects.CategoriesContains;
						if (this.Constraint.ConstraintValue is DominionBase.Cards.Category)
						{
							foreach (KeyValuePair<DominionBase.Cards.Category, int> kvpCategory in cbValue.Items.OfType<KeyValuePair<DominionBase.Cards.Category, int>>())
							{
								if (kvpCategory.Key == (DominionBase.Cards.Category)this.Constraint.ConstraintValue)
								{
									cbValue.SelectedItem = kvpCategory;
									break;
								}
							}
						}
						cbValue.Focus();
						break;

					case DominionBase.Cards.ConstraintType.CardCosts:
						cbValue.ItemsSource = DisplayObjects.Costs;
						if (this.Constraint.ConstraintValue is DominionBase.Cards.Cost)
						{
							foreach (KeyValuePair<DominionBase.Cards.Cost, int> kvpCost in cbValue.Items.OfType<KeyValuePair<DominionBase.Cards.Cost, int>>())
							{
								if (kvpCost.Key == (DominionBase.Cards.Cost)this.Constraint.ConstraintValue)
								{
									cbValue.SelectedItem = kvpCost;
									break;
								}
							}
						}
						cbValue.Focus();
						break;

					case DominionBase.Cards.ConstraintType.CardDontUse:
						cbValue.ItemsSource = DisplayObjects.Cards;
						if (this.Constraint.ConstraintValue is String)
						{
							foreach (DominionBase.Cards.Card card in cbValue.Items.OfType<DominionBase.Cards.Card>())
							{
								if (card.Name == (String)this.Constraint.ConstraintValue)
								{
									cbValue.SelectedItem = card;
									break;
								}
							}
						}
						cbValue.Focus();
						break;

					case DominionBase.Cards.ConstraintType.CardMustUse:
						cbValue.ItemsSource = DisplayObjects.Cards;
						if (this.Constraint.ConstraintValue is String)
						{
							foreach (DominionBase.Cards.Card card in cbValue.Items.OfType<DominionBase.Cards.Card>())
							{
								if (card.Name == (String)this.Constraint.ConstraintValue)
								{
									cbValue.SelectedItem = card;
									break;
								}
							}
						}
						cbValue.Focus();
						break;

					case DominionBase.Cards.ConstraintType.MemberOfGroup:
						cbValue.ItemsSource = DisplayObjects.Groups;
						if (this.Constraint.ConstraintValue is DominionBase.Cards.Group)
						{
							foreach (KeyValuePair<DominionBase.Cards.Group, int> kvpGroup in cbValue.Items.OfType<KeyValuePair<DominionBase.Cards.Group, int>>())
							{
								if (kvpGroup.Key == (DominionBase.Cards.Group)this.Constraint.ConstraintValue)
								{
									cbValue.SelectedItem = kvpGroup;
									break;
								}
							}
						}
						cbValue.Focus();
						break;

					default:
						break;
				}

				this.counts.Clear();

				cbMinimum.ItemsSource = this.Counts;
				cbMinimum.SelectedItem = _Constraint.Minimum;

				cbMaximum.ItemsSource = this.Counts;
				cbMaximum.SelectedItem = _Constraint.Maximum;
			}
		}

		private void cbValue_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count > 0)
			{
				if (this.Constraint.ConstraintType == DominionBase.Cards.ConstraintType.CardDontUse || this.Constraint.ConstraintType == DominionBase.Cards.ConstraintType.CardMustUse)
				{
					ttCard.Visibility = System.Windows.Visibility.Visible;
					ttcCard.ICard = (DominionBase.Cards.Card)e.AddedItems[0];
					this.Constraint.ConstraintValue = ((DominionBase.Cards.Card)e.AddedItems[0]).Name;
				}
				else
				{
					ttCard.Visibility = System.Windows.Visibility.Collapsed;
					ttcCard.ICard = null;
					this.Constraint.ConstraintValue = e.AddedItems[0];
				}
			}
		}

		private void cbValue_MouseDown(object sender, MouseButtonEventArgs e)
		{
			FrameworkElement fe = sender as FrameworkElement;
			if (wMain.Settings == null || fe.ToolTip == null || !(fe.ToolTip is ToolTip) || (fe.ToolTip as ToolTip).Content == null ||
				!((fe.ToolTip as ToolTip).Content is ToolTipCard) || ((fe.ToolTip as ToolTip).Content as ToolTipCard).ICard == null ||
				((fe.ToolTip as ToolTip).Content as ToolTipCard).ICard.CardType == DominionBase.Cards.Universal.TypeClass.Dummy)
				return;

			if (wMain.Settings.ShowToolTipOnRightClick && e.ChangedButton == MouseButton.Right && e.ButtonState == MouseButtonState.Pressed)
			{
				fe.CaptureMouse();
				(fe.ToolTip as ToolTip).IsOpen = true;
			}
		}

		private void cbValue_MouseUp(object sender, MouseButtonEventArgs e)
		{
			FrameworkElement fe = sender as FrameworkElement;
			if (wMain.Settings == null || fe.ToolTip == null)
				return;

			if (wMain.Settings.ShowToolTipOnRightClick && e.ChangedButton == MouseButton.Right && e.ButtonState == MouseButtonState.Released)
			{
				fe.ReleaseMouseCapture();
				(fe.ToolTip as ToolTip).IsOpen = false;
			}
		}

		private void bRemove_Click(object sender, RoutedEventArgs e)
		{
			(sender as Button).IsEnabled = false;
			RaiseEvent(new RoutedEventArgs(RemoveClickEvent));
			(sender as Button).IsEnabled = true;
		}

		private void This_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((cbValue.ToolTip as ToolTip).IsOpen)
				(cbValue.ToolTip as ToolTip).IsOpen = false;
		}

	}
}
