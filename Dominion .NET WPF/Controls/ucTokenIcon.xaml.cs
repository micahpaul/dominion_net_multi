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

namespace Dominion.NET_WPF.Controls
{
	/// <summary>
	/// Interaction logic for ucTokenIcon.xaml
	/// </summary>
	public partial class ucTokenIcon : UserControl
	{
		public static readonly DependencyProperty TokenProperty =
			DependencyProperty.Register("Token", typeof(DominionBase.Token), typeof(ucTokenIcon), new PropertyMetadata(null));
		public DominionBase.Token Token
		{
			get { return (DominionBase.Token)this.GetValue(TokenProperty); }
			set
			{
				this.SetValue(TokenProperty, value);

				if (value == null)
				{
					lToken.ToolTip = null;
					tbToken.Text = String.Empty;
					lToken.Background = lToken.Foreground = Brushes.Transparent;
					return;
				}

				lToken.ToolTip = Utilities.RenderText(value.Title);
				tbToken.Text = Utilities.RenderText(value.DisplayString);

				Type tokenType = value.GetType();
				if (tokenType == DominionBase.Cards.Seaside.TypeClass.EmbargoToken)
				{
					lToken.Background = Caching.BrushRepository.GetBackgroundBrush(DominionBase.Cards.Category.Curse);
					lToken.Foreground = Brushes.Snow;
				}
				else if (tokenType == DominionBase.Cards.Prosperity.TypeClass.VictoryToken)
				{
					lToken.Background = Caching.BrushRepository.GetBackgroundBrush(DominionBase.Cards.Category.Victory);
				}
				else if (tokenType == DominionBase.Cards.Prosperity.TypeClass.ContrabandToken)
				{
					lToken.Background = Brushes.Black;
					lToken.Foreground = Brushes.Red;
				}
				else if (tokenType == DominionBase.Cards.Prosperity.TypeClass.TradeRouteToken)
				{
					lToken.Background = Caching.BrushRepository.GetBackgroundBrush(DominionBase.Cards.Category.Treasure);
				}
				else if (tokenType == DominionBase.Cards.Cornucopia.TypeClass.BaneToken)
				{
					lToken.Background = Caching.BrushRepository.GetBackgroundBrush(DominionBase.Cards.Category.Reaction);
					tbToken.Effect = Caching.DropShadowRepository.GetDSE(8, Colors.White, 1d);
				}

			}
		}

		public static readonly DependencyProperty SizeProperty =
			DependencyProperty.Register("Size", typeof(CardSize), typeof(ucTokenIcon), new PropertyMetadata(CardSize.Text));
		public CardSize Size
		{
			get { return (CardSize)this.GetValue(SizeProperty); }
			set
			{
				this.SetValue(SizeProperty, value);

				switch (value)
				{
					case CardSize.SmallText:
						dpName.Height = 16;
						lToken.Margin = new Thickness(0, 0, 2, 0);
						break;
					case CardSize.Text:
						dpName.Height = Double.NaN;
						lToken.Margin = new Thickness(0);
						break;
				}
			}
		}

		public ucTokenIcon()
		{
			InitializeComponent();
		}
	}
}
