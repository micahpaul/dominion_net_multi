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
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using DominionBase.Utilities;

namespace Dominion.NET_WPF
{
	/// <summary>
	/// Interaction logic for CardCollectionControl.xaml
	/// </summary>
	public partial class CardCollectionControl : UserControl
	{
		public static readonly DependencyProperty SplitAtProperty =
			DependencyProperty.Register("SplitAt", typeof(DominionBase.ICard), typeof(CardCollectionControl), new PropertyMetadata(null));
		public DominionBase.ICard SplitAt
		{
			get { return (DominionBase.ICard)this.GetValue(SplitAtProperty); }
			set
			{
				this.SetValue(SplitAtProperty, value);
				UpdateCardDisplay();
			}
		}

		public static readonly DependencyProperty MinStackWidthProperty =
			DependencyProperty.Register("MinStackWidth", typeof(double), typeof(CardCollectionControl), new PropertyMetadata(0d));
		public double MinStackWidth
		{
			get { return (double)this.GetValue(MinStackWidthProperty); }
			set
			{
				this.SetValue(MinStackWidthProperty, value);
				UpdateCardDisplay();
			}
		}

		private CardSize _CardSize = CardSize.Text;
		private IEnumerable<DominionBase.Cards.Card> _CardPile = null;
		private Dictionary<DominionBase.Cards.Card, DominionBase.TokenCollection> _TokenDict = null;
		private int _GlowSize = 20;
		private String _PileName = "Pile Name";

		private DominionBase.Players.PhaseEnum _PlayerPhase = DominionBase.Players.PhaseEnum.Action;
		private DominionBase.Players.PlayerMode _PlayerMode = DominionBase.Players.PlayerMode.Waiting;
		private DominionBase.Cards.Card _ClickedCard = null;
		private Boolean _IsClickable = false;

		public CardCollectionControl()
		{
			InitializeComponent();
			this.CardSize = NET_WPF.CardSize.Medium;
			this.IsClickable = false;
			wpCardCollections.Margin = new Thickness(_GlowSize / 2, _GlowSize / 2, _GlowSize / 2, 0);
			wpCardCollections2.Margin = new Thickness(_GlowSize / 2, _GlowSize / 2, _GlowSize / 2, 0);
		}

		public DominionBase.Cards.Card ClickedCard
		{
			get { return _ClickedCard; }
		}

		public Boolean ExactCount { get; set; }
		public Boolean IsCardsVisible { get; set; }
		public Boolean IsClickable 
		{
			get
			{
				return _IsClickable;
			}
			set
			{
				_IsClickable = value;
				foreach (Controls.CardStackControl csc in wpCardCollections.Children.OfType<Controls.CardStackControl>())
					csc.IsClickable = value;
				foreach (Controls.CardStackControl csc in wpCardCollections2.Children.OfType<Controls.CardStackControl>())
					csc.IsClickable = value;
			}
		}
		public Boolean IsDisplaySorted { get; set;}
		public Boolean IsVPsVisible { get; set; }
		public CardSize CardSize 
		{
			get { return _CardSize; }
			set
			{
				_CardSize = value;
				UpdateCardDisplay();
			}
		}

		public IEnumerable<DominionBase.Cards.Card> Pile
		{
			private get { return _CardPile; }
			set
			{
				_CardPile = value;
				UpdateCardDisplay();
			}
		}

		public Dictionary<DominionBase.Cards.Card, DominionBase.TokenCollection> TokenDict
		{
			private get { return _TokenDict; }
			set
			{
				_TokenDict = value;
				UpdateCardDisplay();
			}
		}
		public DominionBase.Piles.Visibility PileVisibility
		{
			get
			{
				if (this.Pile is DominionBase.Piles.Pile)
					return (this.Pile as DominionBase.Piles.Pile).Visibility;
				else
					return DominionBase.Piles.Visibility.All;
			}
		}
		private int PileCount
		{
			get
			{
				if (this.Pile is DominionBase.Piles.Pile)
					return (this.Pile as DominionBase.Piles.Pile).Count;
				else
					return this.Pile.Count();
			}
		}
		private DominionBase.Cards.Card PileFirst
		{
			get
			{
				if (this.Pile is DominionBase.Piles.Pile)
					return (this.Pile as DominionBase.Piles.Pile).First();
				else
					return this.Pile.First();
			}
		}

		public String PileName 
		{
			get { return _PileName; }
			set
			{
				_PileName = value;
				UpdateCardDisplay();
			}
		}

		public void UpdateCardDisplay()
		{
			wpCardCollections.Children.Clear();
			wpCardCollections2.Children.Clear();
			wpToolTipCards.Children.Clear();
			nPileName.Content = PileName;

			if (this.CardSize == NET_WPF.CardSize.Full && this.Pile != null && this.PileCount > 0)
			{
				foreach (DominionBase.Cards.Card card in this.Pile)
				{
					ToolTipCard ttc = new ToolTipCard();
					ttc.ICard = card;
					ttc.Margin = new Thickness(3);
					wpToolTipCards.Children.Add(ttc);
				}
			}
			else
			{
				if (String.IsNullOrEmpty(this.PileName))
					nPileName.Visibility = System.Windows.Visibility.Collapsed;
				else
					nPileName.Visibility = System.Windows.Visibility.Visible;

				if (ExactCount && this.Pile != null && this.PileCount > 0)
				{
					lCount.Visibility = System.Windows.Visibility.Visible;
					lCount.Content = String.Format("({0})", StringUtility.Plural("Card", this.PileCount, true));
				}
				else
				{
					lCount.Visibility = System.Windows.Visibility.Collapsed;
				}

				WrapPanel currentWrapPanel = wpCardCollections;
				foreach (DominionBase.Cards.CardCollection cardStack in GenerateCardStacks())
				{
					Controls.CardStackControl csc = new Controls.CardStackControl() { MinWidth = this.MinStackWidth };
					csc.CardSize = this.CardSize;
					csc.ExactCount = this.ExactCount;
					csc.IsCardsVisible = this.IsCardsVisible;
					csc.Phase = this._PlayerPhase;
					csc.PlayerMode = this._PlayerMode;
					csc.PileVisibility = this.PileVisibility;
					if (this.TokenDict != null && this.TokenDict.ContainsKey(cardStack[0]))
						csc.Tokens = this.TokenDict[cardStack[0]];
					csc.CardCollection = cardStack;
					if (this.IsVPsVisible && (
						(cardStack[0].Category & DominionBase.Cards.Category.Victory) == DominionBase.Cards.Category.Victory ||
						(cardStack[0].Category & DominionBase.Cards.Category.Curse) == DominionBase.Cards.Category.Curse))
						csc.CountVPs(this.Pile);
					else
						csc.HideVPs();

					if (this.SplitAt != null && csc.CardCollection.Count() > 0 && csc.CardCollection.First().CardType == this.SplitAt.CardType)
						currentWrapPanel = wpCardCollections2;

					currentWrapPanel.Children.Add(csc);
				}
			}

			InvalidateVisual();
		}

		public DominionBase.Players.PhaseEnum Phase
		{
			set
			{
				_PlayerPhase = value;

				foreach (Controls.CardStackControl csc in wpCardCollections.Children.OfType<Controls.CardStackControl>())
					csc.Phase = value;
			}
		}
		public DominionBase.Players.PlayerMode PlayerMode
		{
			set
			{
				_PlayerMode = value;

				foreach (Controls.CardStackControl csc in wpCardCollections.Children.OfType<Controls.CardStackControl>())
					csc.PlayerMode = value;
			}
		}

		private List<DominionBase.Cards.CardCollection> GenerateCardStacks()
		{
			List<DominionBase.Cards.CardCollection> cardStacks = new List<DominionBase.Cards.CardCollection>();

			if (this.Pile == null || this.PileCount == 0)
				return cardStacks;

			int count;

			switch (this.PileVisibility)
			{
				case DominionBase.Piles.Visibility.All:

					IEnumerable<DominionBase.Cards.Card> cards = this.Pile;
					if (this.IsDisplaySorted)
						cards = this.Pile.OrderBy(c => c.Name);

					foreach (DominionBase.Cards.Card card in cards)
					{
						Type cardT = IsCardsVisible ? card.CardType : typeof(object);
						if (cardStacks.Count == 0 || !card.IsStackable || (IsCardsVisible && cardStacks[cardStacks.Count - 1][0].CardType != cardT))
							cardStacks.Add(new DominionBase.Cards.CardCollection());
						cardStacks[cardStacks.Count - 1].AddRange(card.CardStack());
					}
					break;

				case DominionBase.Piles.Visibility.Top:
					if (this.PileCount > 0)
					{
						cardStacks.Add(new DominionBase.Cards.CardCollection());
						count = this.PileCount - 1;
						if (!this.ExactCount && count > 1)
						{
							Double variance = DominionBase.Utilities.Gaussian.NextGaussian();
							count = count + (int)(variance * (count / 4.0));
						}
						for (int index = 0; index < count; index++)
							cardStacks[0].Add(new DominionBase.Cards.Universal.Dummy());
						cardStacks[0].Add(this.PileFirst);
					}
					break;

				case DominionBase.Piles.Visibility.None:
					if (this.PileCount > 0)
					{
						cardStacks.Add(new DominionBase.Cards.CardCollection());
						count = this.PileCount;
						if (!this.ExactCount && count > 1)
						{
							Double variance = DominionBase.Utilities.Gaussian.NextGaussian();
							count = count + (int)(variance * (count / 4.0));
						}
						for (int index = 0; index < count; index++)
							cardStacks[0].Add(new DominionBase.Cards.Universal.Dummy());
					}
					break;
			}

			return cardStacks;
		}
	}
}
