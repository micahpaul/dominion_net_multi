using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using DominionBase;
using DominionBase.Cards;

namespace Dominion.NET_WPF
{
	/// <summary>
	/// Interaction logic for ToolTipCard.xaml
	/// </summary>
	public partial class ToolTipCard : UserControl
	{
		private Boolean _IsSetup = false;

		public ToolTipCard()
		{
			InitializeComponent();
		}

		private void SetupDisplay()
		{
			if (this.ICard == null)
				return;

			Boolean fullCardFound = false;
			if (wMain.Settings.UseCustomToolTips)
			{
				iFullCard.Visibility = System.Windows.Visibility.Visible;
				dpCardFace.Visibility = System.Windows.Visibility.Hidden;
				Caching.ImageRepository repo = Caching.ImageRepository.Acquire();
				BitmapImage im = repo.GetBitmapImage(this.ICard.Name.Replace(" ", "").Replace("'", ""), "full");

				if (im != null)
				{
					iFullCard.Source = im;
					fullCardFound = true;
				}
				Caching.ImageRepository.Release();
			}

			if (!fullCardFound)
			{
				iFullCard.Visibility = System.Windows.Visibility.Hidden;
				dpCardFace.Visibility = System.Windows.Visibility.Visible;

				tbCardName.Text = String.Empty;
				Run rName = null;
				foreach (char letter in this.ICard.Name)
				{
					if (rName == null || Char.IsUpper(rName.Text[0]) != Char.IsUpper(letter))
					{
						if (rName != null)
							tbCardName.Inlines.Add(rName);
						rName = new Run();
						if (Char.IsUpper(letter))
							rName.FontSize = 14;
						else
							rName.FontSize = 11;
					}
					rName.Text += Char.ToUpper(letter);
				}
				if (rName != null)
					tbCardName.Inlines.Add(rName);

				Caching.ImageRepository repo = Caching.ImageRepository.Acquire();

				BitmapImage im = repo.GetBitmapImage(this.ICard.Name.Replace(" ", "").Replace("'", ""), "medium");
				imCardLarge.Source = im;

				String iconName;
				if (this.ICard.Source != Source.Promotional && this.ICard.Source != Source.Custom)
					iconName = this.ICard.Source.ToString();
				else
					iconName = this.ICard.Name.Replace(" ", "").Replace("'", "");
				im = repo.GetBitmapImage(iconName, String.Empty);
				imSource.Source = im;

				Caching.ImageRepository.Release();

				List<String> cardTypes = new List<String>();
				if ((this.ICard.Category & Category.Action) == Category.Action)
					cardTypes.Add("Action");

				if ((this.ICard.Category & Category.Curse) == Category.Curse)
					cardTypes.Add("Curse");

				if ((this.ICard.Category & Category.Duration) == Category.Duration)
					cardTypes.Add("Duration");

				if ((this.ICard.Category & Category.Treasure) == Category.Treasure)
					cardTypes.Add("Treasure");

				if ((this.ICard.Category & Category.Attack) == Category.Attack)
					cardTypes.Add("Attack");

				if ((this.ICard.Category & Category.Knight) == Category.Knight)
					cardTypes.Add("Knight");

				if ((this.ICard.Category & Category.Victory) == Category.Victory)
					cardTypes.Add("Victory");

				if ((this.ICard.Category & Category.Reaction) == Category.Reaction)
					cardTypes.Add("Reaction");

				if ((this.ICard.Category & Category.Prize) == Category.Prize)
					cardTypes.Add("Prize");

				if ((this.ICard.Category & Category.Shelter) == Category.Shelter)
					cardTypes.Add("Shelter");

				if ((this.ICard.Category & Category.Looter) == Category.Looter)
					cardTypes.Add("Looter");

				if ((this.ICard.Category & Category.Ruins) == Category.Ruins)
					cardTypes.Add("Ruins");

				tbTreasureValueLeft.Inlines.Clear();
				tbTreasureValueRight.Inlines.Clear();
				tbCardCost.Inlines.Clear();
				spCardText.Children.Clear();

				if ((this.ICard.Category & Category.Treasure) == Category.Treasure)
				{
					lblTreasureValueLeft.Visibility = lblTreasureValueRight.Visibility = System.Windows.Visibility.Visible;
					TextBlock tbTreasureValue = (TextBlock)Utilities.RenderText(this.ICard.Benefit.Currency.ToStringInline(), NET_WPF.RenderSize.Small, false)[0];
					while (tbTreasureValue.Inlines.Count > 0)
						tbTreasureValueLeft.Inlines.Add(tbTreasureValue.Inlines.ElementAt(0));
					tbTreasureValue = (TextBlock)Utilities.RenderText(this.ICard.Benefit.Currency.ToStringInline(), NET_WPF.RenderSize.Small, false)[0];
					while (tbTreasureValue.Inlines.Count > 0)
						tbTreasureValueRight.Inlines.Add(tbTreasureValue.Inlines.ElementAt(0));
				}
				else
				{
					lblTreasureValueLeft.Visibility = lblTreasureValueRight.Visibility = System.Windows.Visibility.Hidden;
				}

				TextBlock tbTemp = (TextBlock)Utilities.RenderText(this.ICard.BaseCost.ToString(), NET_WPF.RenderSize.Small, false)[0];
				while (tbTemp.Inlines.Count > 0)
					tbCardCost.Inlines.Add(tbTemp.Inlines.ElementAt(0));
				if (this.ICard.BaseCost.Special)
				{
					Run special = new Run("*");
					special.FontWeight = FontWeights.Bold;
					tbCardCost.Inlines.Add(special);
				}
				if (this.ICard.BaseCost.CanOverpay)
				{
					Run canOverpay = new Run("+");
					canOverpay.FontWeight = FontWeights.Bold;
					tbCardCost.Inlines.Add(canOverpay);
				}

				tbCardType.Text = String.Empty;
				Run rCardType = null;
				foreach (char letter in String.Join(" - ", cardTypes.ToArray()))
				{
					if (rCardType == null || Char.IsUpper(rCardType.Text[0]) != Char.IsUpper(letter))
					{
						if (rCardType != null)
							tbCardType.Inlines.Add(rCardType);
						rCardType = new Run();
						if (Char.IsUpper(letter))
							rCardType.FontSize = 12;
						else
							rCardType.FontSize = 10;
					}
					rCardType.Text += Char.ToUpper(letter);
				}
				if (rCardType != null)
					tbCardType.Inlines.Add(rCardType);

				DominionBase.Cards.Category category = this.ICard.Category;
				if (this.ICard is DominionBase.Cards.Card)
					category = ((DominionBase.Cards.Card)this.ICard).PhysicalCategory;

				lblCardName.Background = bBottomArea.Background = rBackground.Fill = Caching.BrushRepository.GetBackgroundBrush(category);

				lblCardName.Foreground = lblCardType.Foreground = Caching.BrushRepository.GetForegroundBrush(category);
				if ((category & Category.Reaction) == Category.Reaction)
					tbCardName.Effect = tbCardType.Effect = Caching.DropShadowRepository.GetDSE(8, Colors.White, 1d);

				if (lblCardName.Foreground.CanFreeze)
					lblCardName.Foreground.Freeze();
				if (lblCardType.Foreground.CanFreeze)
					lblCardType.Foreground.Freeze();
				if (lblCardCost.Foreground.CanFreeze)
					lblCardCost.Foreground.Freeze();

				String[] text = this.ICard.Text.Split(new string[] { "<br/>" }, StringSplitOptions.RemoveEmptyEntries);
				for (int index = 0; index < text.Length; index++)
				{
					if (index > 0)
					{
						Line newLine = new Line();
						newLine.Stretch = Stretch.Fill;
						newLine.Stroke = Brushes.Black;
						newLine.StrokeThickness = 2;
						newLine.X2 = 1;
						newLine.Margin = new Thickness(20, 2, 20, 2);
						spCardText.Children.Add(newLine);
					}

					String t = text[index];
					TextBlock tbBody = new TextBlock();
					tbBody.HorizontalAlignment = HorizontalAlignment.Center;
					tbBody.Margin = new Thickness(10, 0, 10, 0);
					tbBody.Padding = new Thickness(0);
					tbBody.TextWrapping = TextWrapping.Wrap;
					tbBody.TextAlignment = TextAlignment.Center;

					List<UIElement> elements = Utilities.RenderText(t, (this.ICard.Location == Location.General ? NET_WPF.RenderSize.ExtraLarge : NET_WPF.RenderSize.Small), false);
					foreach (TextBlock tb in elements.OfType<TextBlock>())
					{
						tb.HorizontalAlignment = HorizontalAlignment.Center;
						tb.VerticalAlignment = System.Windows.VerticalAlignment.Center;
						tb.Margin = new Thickness(10, 0, 10, 0);
						tb.Padding = new Thickness(0);
						tb.TextWrapping = TextWrapping.Wrap;
						tb.TextAlignment = TextAlignment.Center;
					}
					elements.ForEach(e => spCardText.Children.Add(e));
				}
			}

			_IsSetup = true;
		}

		public static readonly DependencyProperty ICardProperty =
			DependencyProperty.Register("ICard", typeof(ICard), typeof(ToolTipCard),
			new PropertyMetadata(null));
		public ICard ICard
		{
			get { return (ICard)this.GetValue(ICardProperty); }
			set 
			{
				if (((this.ICard == null || value == null) && this.ICard != value) || 
					(this.ICard != null && value != null && this.ICard.Name != value.Name))
					_IsSetup = false;
				this.SetValue(ICardProperty, value);
				if (this.IsVisible)
					SetupDisplay();
			}
		}

		private delegate void IsVisibleChanged_Delegate(object sender, DependencyPropertyChangedEventArgs e);

		private void ToolTipCard_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				if (((Boolean)e.NewValue) && !_IsSetup)
					SetupDisplay();
			}
			else
			{
				this.Dispatcher.BeginInvoke(new IsVisibleChanged_Delegate(ToolTipCard_IsVisibleChanged), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}
	}
}
