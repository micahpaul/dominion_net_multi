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
	/// Interaction logic for ucCardSettings.xaml
	/// </summary>
	public partial class ucCardSettings : UserControl, IDisposable
	{
		public static readonly DependencyProperty CardsSettingsProperty =
			DependencyProperty.Register("CardsSettings", typeof(DominionBase.Cards.CardsSettings), typeof(ucCardSettings), new PropertyMetadata(null));
		public DominionBase.Cards.CardsSettings CardsSettings
		{
			get { return (DominionBase.Cards.CardsSettings)this.GetValue(CardsSettingsProperty); }
			set { 
				this.SetValue(CardsSettingsProperty, value);

				lName.Content = (value as DominionBase.Cards.CardsSettings).Name;
				ttcCard.ICard = DisplayObjects.Cards.FirstOrDefault(c => c.Name == (value as DominionBase.Cards.CardsSettings).Name);

				foreach (DominionBase.Cards.CardSetting cardSetting in value.CardSettingOrdered)
				{
					if (cardSetting.Type == typeof(Boolean))
					{
						StackPanel spBoolean = new StackPanel();
						spBoolean.Orientation = Orientation.Horizontal;
						spBoolean.ToolTip = cardSetting.Hint;
						icCardSetting.Items.Add(spBoolean);

						CheckBox cbBoolean = new CheckBox();
						cbBoolean.Tag = cardSetting;
						cbBoolean.VerticalAlignment = System.Windows.VerticalAlignment.Center;
						cbBoolean.IsChecked = (Boolean)cardSetting.Value;
						cbBoolean.Margin = new Thickness(3);
						cbBoolean.Checked += new RoutedEventHandler(cbBoolean_Checked);
						cbBoolean.Unchecked += new RoutedEventHandler(cbBoolean_Checked);
						spBoolean.Children.Add(cbBoolean);

						Label lBoolean = new Label();
						lBoolean.VerticalAlignment = System.Windows.VerticalAlignment.Center;
						lBoolean.Content = cardSetting.Text;
						lBoolean.Padding = new Thickness(0);
						cbBoolean.Content = lBoolean;
					}
					else if (cardSetting.Type == typeof(int))
					{
						StackPanel spInt = new StackPanel();
						spInt.Orientation = Orientation.Horizontal;
						spInt.ToolTip = cardSetting.Hint;
						icCardSetting.Items.Add(spInt);

						Label lInt = new Label();
						lInt.Padding = new Thickness(0);
						lInt.VerticalAlignment = System.Windows.VerticalAlignment.Center;
						lInt.Content = String.Format("{0}: ", cardSetting.Text);
						spInt.Children.Add(lInt);

						TextBox tbInt = new TextBox();
						tbInt.Tag = cardSetting;
						tbInt.VerticalAlignment = System.Windows.VerticalAlignment.Center;
						tbInt.Width = 50;
						tbInt.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Right;
						tbInt.Text = cardSetting.Value.ToString();
						tbInt.TextChanged += new TextChangedEventHandler(tbInt_TextChanged);
						spInt.Children.Add(tbInt);
					}
					else if (cardSetting.Type == typeof(DominionBase.Cards.ConstraintCollection))
					{
						GroupBox gbConstraint = new GroupBox();
						gbConstraint.Header = cardSetting.Text;
						gbConstraint.ToolTip = cardSetting.Hint;
						gbConstraint.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
						icCardSetting.Items.Add(gbConstraint);

						ScrollViewer svConstraint = new ScrollViewer();
						svConstraint.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
						gbConstraint.Content = svConstraint;

						ucCardConstraints ucccConstraint = new ucCardConstraints();
						ucccConstraint.ConstraintCollection = (DominionBase.Cards.ConstraintCollection)cardSetting.Value;
						svConstraint.Content = ucccConstraint;
					}
					else
					{
						StackPanel spString = new StackPanel();
						spString.Orientation = Orientation.Horizontal;
						spString.ToolTip = cardSetting.Hint;
						icCardSetting.Items.Add(spString);

						Label lString = new Label();
						lString.Padding = new Thickness(0);
						lString.VerticalAlignment = System.Windows.VerticalAlignment.Center;
						lString.Content = String.Format("{0}: ", cardSetting.Text);
						spString.Children.Add(lString);

						TextBox tbString = new TextBox();
						tbString.Tag = cardSetting;
						tbString.VerticalAlignment = System.Windows.VerticalAlignment.Center;
						tbString.Text = (String)cardSetting.Value;
						tbString.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
						tbString.TextChanged += new TextChangedEventHandler(tbString_TextChanged);
						spString.Children.Add(tbString);
					}
				}
			}
		}

		void tbString_TextChanged(object sender, TextChangedEventArgs e)
		{
			((sender as TextBox).Tag as DominionBase.Cards.CardSetting).Value = (sender as TextBox).Text;
		}

		void tbInt_TextChanged(object sender, TextChangedEventArgs e)
		{
			int value;
			int.TryParse((sender as TextBox).Text, out value);
			((sender as TextBox).Tag as DominionBase.Cards.CardSetting).Value = value;
		}

		void cbBoolean_Checked(object sender, RoutedEventArgs e)
		{
			((sender as CheckBox).Tag as DominionBase.Cards.CardSetting).Value = (sender as CheckBox).IsChecked;
		}

		public ucCardSettings()
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
					ToolTipService.SetIsEnabled(lName, false);
				else
				{
					ToolTipService.SetIsEnabled(lName, true);
					ToolTipService.SetShowDuration(lName, (int)settings.ToolTipShowDuration);
				}
			}
		}

		private void lName_MouseDown(object sender, MouseButtonEventArgs e)
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

		private void lName_MouseUp(object sender, MouseButtonEventArgs e)
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

		private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((lName.ToolTip as ToolTip).IsOpen)
				(lName.ToolTip as ToolTip).IsOpen = false;
		}
	}
}
