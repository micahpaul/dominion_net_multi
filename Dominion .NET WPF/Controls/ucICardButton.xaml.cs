using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Dominion.NET_WPF.Controls
{
	/// <summary>
	/// Interaction logic for ucICardButton.xaml
	/// </summary>
	public partial class ucICardButton : UserControl, IDisposable
	{
		public static readonly DependencyProperty ICardProperty =
			DependencyProperty.Register("ICard", typeof(DominionBase.ICard), typeof(ucICardButton),
			new FrameworkPropertyMetadata(null,
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
				new PropertyChangedCallback(ucICardButton.OnICardChanged),
				new CoerceValueCallback(ucICardButton.CoerceICard)));

		private static void OnICardChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ucICardButton ctrl = d as ucICardButton;
			ctrl.ICard = (DominionBase.ICard)e.NewValue;
		}

		private static object CoerceICard(DependencyObject d, object value)
		{
			return value;
		}

		public static readonly RoutedEvent ICardButtonClickEvent = EventManager.RegisterRoutedEvent(
			"ICardButtonClick",
			RoutingStrategy.Bubble,
			typeof(RoutedEventHandler),
			typeof(ucICardButton));

		public event RoutedEventHandler ICardButtonClick
		{
			add { AddHandler(ICardButtonClickEvent, value); }
			remove { RemoveHandler(ICardButtonClickEvent, value); }
		}

		private Boolean _IsOrdered = false;
		private int _Order = 0;

		public ucICardButton()
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
			if (settings.ToolTipShowDuration == ToolTipShowDuration.Off)
				ToolTipService.SetIsEnabled(this, false);
			else
			{
				ToolTipService.SetIsEnabled(this, true);
				ToolTipService.SetShowDuration(this, (int)settings.ToolTipShowDuration);
			}
		}

		public bool? IsChecked
		{
			get { return bName.IsChecked; }
			set { bName.IsChecked = value; }
		}

		public DominionBase.ICard ICard
		{
			get { return (DominionBase.ICard)GetValue(ICardProperty); }
			set
			{
				SetValue(ICardProperty, value);

				UpdateCardDisplay();
			}
		}

		public String Title
		{
			get { return tbName.Text; }
			private set
			{
				tbName.Text = value;
				if (String.IsNullOrEmpty(value))
					lName.Visibility = System.Windows.Visibility.Hidden;
				else
					lName.Visibility = System.Windows.Visibility.Visible;
			}
		}

		public Boolean IsOrdered
		{
			get { return _IsOrdered; }
			set { _IsOrdered = value; UpdateCardDisplay(); }
		}
		public int Order
		{
			get { return _Order; }
			set { _Order = value; UpdateCardDisplay(); }
		}

		private void UpdateCardDisplay()
		{
			if (this.ICard != null)
			{
				DominionBase.Cards.Category category = this.ICard.Category;
				if (this.ICard is DominionBase.Cards.Card)
					category = ((DominionBase.Cards.Card)this.ICard).PhysicalCategory;

				lName.Background = Caching.BrushRepository.GetBackgroundBrush(category);
				lName.Foreground = Caching.BrushRepository.GetForegroundBrush(category);

				if ((category & DominionBase.Cards.Category.Reaction) == DominionBase.Cards.Category.Reaction)
					tbName.Effect = Caching.DropShadowRepository.GetDSE(8, Colors.White, 1d);

				this.Title = this.ICard.Name;
			}

			ttcCard.ICard = this.ICard;

			if (this.IsOrdered)
			{
				if (this.Order > 0)
					lOrdinal.Visibility = System.Windows.Visibility.Visible;
				else
					lOrdinal.Visibility = System.Windows.Visibility.Hidden;
				tbOrdinal.Text = Utilities.Ordinal(this.Order);
			}
			else
				lOrdinal.Visibility = System.Windows.Visibility.Collapsed;
		}

		private void b_Click(object sender, RoutedEventArgs e)
		{
			(sender as ToggleButton).IsEnabled = false;

			if ((sender as ToggleButton).IsChecked == true)
			{
				bControl.BorderBrush = Brushes.SaddleBrown;
			}
			else
			{
				bControl.BorderBrush = Brushes.Transparent;
			}
			RaiseEvent(new RoutedEventArgs(ICardButtonClickEvent));

			(sender as ToggleButton).IsEnabled = true;
		}

		private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (wMain.Settings != null && this.ToolTip != null && (this.ToolTip as ToolTip).Content != null &&
				((this.ToolTip as ToolTip).Content as ToolTipCard).ICard != null &&
				((this.ToolTip as ToolTip).Content as ToolTipCard).ICard.CardType != DominionBase.Cards.Universal.TypeClass.Dummy &&
				wMain.Settings.ShowToolTipOnRightClick && e.ChangedButton == MouseButton.Right && e.ButtonState == MouseButtonState.Pressed)
			{
				this.CaptureMouse();
				(this.ToolTip as ToolTip).IsOpen = true;
			}
		}

		private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (wMain.Settings != null && this.ToolTip != null &&
				wMain.Settings.ShowToolTipOnRightClick && e.ChangedButton == MouseButton.Right && e.ButtonState == MouseButtonState.Released)
			{
				this.ReleaseMouseCapture();
				(this.ToolTip as ToolTip).IsOpen = false;
			}
		}

		private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((this.ToolTip as ToolTip).IsOpen)
				(this.ToolTip as ToolTip).IsOpen = false;
		}
	}
}
