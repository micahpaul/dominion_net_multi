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
	/// Interaction logic for ucICardIcon.xaml
	/// </summary>
	public partial class ucICardIcon : UserControl, IDisposable
	{
		public static readonly DependencyProperty ICardProperty =
			DependencyProperty.Register("ICard", typeof(DominionBase.ICard), typeof(ucICardIcon),
			new FrameworkPropertyMetadata(null,
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
				new PropertyChangedCallback(ucICardIcon.OnICardChanged),
				new CoerceValueCallback(ucICardIcon.CoerceICard)));

		private static void OnICardChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ucICardIcon ctrl = d as ucICardIcon;
			ctrl.ICard = (DominionBase.ICard)e.NewValue;
		}

		private static object CoerceICard(DependencyObject d, object value)
		{
			return value;
		}

		public static readonly DependencyProperty CardVisibilityProperty =
			DependencyProperty.Register("CardVisibility", typeof(DominionBase.Piles.Visibility), typeof(ucICardIcon),
				new FrameworkPropertyMetadata(DominionBase.Piles.Visibility.All,
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
				new PropertyChangedCallback(ucICardIcon.OnCardVisibilityChanged),
				new CoerceValueCallback(ucICardIcon.CoerceCardVisibility)));

		private static void OnCardVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ucICardIcon ctrl = d as ucICardIcon;
			ctrl.CardVisibility = (DominionBase.Piles.Visibility)e.NewValue;
		}

		private static object CoerceCardVisibility(DependencyObject d, object value)
		{
			return value;
		}

		public ucICardIcon()
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

		public DominionBase.ICard ICard
		{
			get { return (DominionBase.ICard)GetValue(ICardProperty); }
			set
			{
				SetValue(ICardProperty, value);

				UpdateCardDisplay();
			}
		}

		public DominionBase.Piles.Visibility CardVisibility
		{
			get { return (DominionBase.Piles.Visibility)GetValue(CardVisibilityProperty); }
			set
			{
				SetValue(CardVisibilityProperty, value);

				switch (value)
				{
					case DominionBase.Piles.Visibility.All:
						tbName.Visibility = System.Windows.Visibility.Visible;
						imName.Visibility = System.Windows.Visibility.Collapsed;
						break;

					case DominionBase.Piles.Visibility.None:
						lName.BorderThickness = new Thickness(0);
						tbName.Visibility = System.Windows.Visibility.Collapsed;
						imName.Visibility = System.Windows.Visibility.Visible;
						break;
				}
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

		private void UpdateCardDisplay()
		{
			if (this.ICard != null)
			{
				if (this.CardVisibility == DominionBase.Piles.Visibility.All)
				{
					DominionBase.Cards.Category category = this.ICard.Category;
					if (this.ICard is DominionBase.Cards.Card)
						category = ((DominionBase.Cards.Card)this.ICard).PhysicalCategory;

					lName.Background = Caching.BrushRepository.GetBackgroundBrush(category);
					lName.Foreground = Caching.BrushRepository.GetForegroundBrush(category);

					if ((category & DominionBase.Cards.Category.Reaction) == DominionBase.Cards.Category.Reaction)
						tbName.Effect = Caching.DropShadowRepository.GetDSE(8, Colors.White, 1d);

					this.Title = this.ICard.Name;

					ttcCard.ICard = this.ICard;
				}
				else
				{
					lName.Background = Brushes.Transparent;
					Caching.ImageRepository repo = Caching.ImageRepository.Acquire();
					if (repo != null && imName != null)
					{
						switch (this.ICard.CardBack)
						{
							case DominionBase.Cards.CardBack.Standard:
								imName.Source = repo.GetBitmapImage("back", "small");
								break;
							case DominionBase.Cards.CardBack.Red:
								imName.Source = repo.GetBitmapImage("back_red", "small");
								break;
						}
					}
					Caching.ImageRepository.Release();
					imName.Width = lName.MinWidth;
					imName.Height = 12;
				}
			}
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
