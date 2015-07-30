using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DominionBase.Visual
{
	public class VisualPlayer
	{
		private String _Name = String.Empty;
		private Guid _UniqueId;
		private Players.PhaseEnum _Phase = Players.PhaseEnum.Action;
		private Players.PlayerMode _PlayerMode = Players.PlayerMode.Waiting;
		private int _VictoryPoints = 0;
		private Piles.Deck _Revealed = new Piles.Deck(Players.DeckLocation.Revealed, Piles.Visibility.All, Piles.VisibilityTo.All);
		private Piles.Deck _DiscardPile = new Piles.Deck(Players.DeckLocation.Discard, Piles.Visibility.Top, Piles.VisibilityTo.All);
		private Piles.Deck _Private = new Piles.Deck(Players.DeckLocation.Private, Piles.Visibility.All, Piles.VisibilityTo.Owner);

		public String Name { get { return _Name; } private set { _Name = value; } }
		public Guid PlayerUniqueId { get { return _UniqueId; } private set { _UniqueId = value; } }
		public Players.PhaseEnum Phase { get { return _Phase; } private set { _Phase = value; } }
		public Players.PlayerMode PlayerMode { get { return _PlayerMode; } private set { _PlayerMode = value; } }
		public int VictoryPoints { get { return _VictoryPoints; } private set { _VictoryPoints = value; } }
		public Piles.Deck Revealed { get { return _Revealed; } private set { _Revealed = value; } }
		public Piles.Deck DiscardPile { get { return _DiscardPile; } private set { _DiscardPile = value; } }
		public Piles.Deck Private { get { return _Private; } private set { _Private = value; } }

		public VisualPlayer(Players.Player player)
		{
			if (player == null)
				return;

			lock (player)
			{
				this.Name = player.Name;
				this.PlayerUniqueId = player.UniqueId;
				this.Phase = player.Phase;
				this.PlayerMode = player.PlayerMode;

				// This sometimes has an Enumeration exception -- can't figure out how to Lock it properly
				try { this.VictoryPoints = player.VictoryPoints; }
				catch { this.VictoryPoints = 0; }

				this.Revealed = player.Revealed;

				Cards.CardCollection discardCards = new Cards.CardCollection();
				for (int i = 0; i < player.DiscardPile.Count - 1; i++)
					discardCards.Add(new Cards.Universal.Dummy());
				if (player.DiscardPile.Count - 1 > 0)
					discardCards.Add(player.DiscardPile.First());
				this.DiscardPile.AddRange(player, discardCards);

				Cards.CardCollection privateCards = new Cards.CardCollection();
				for (int i = 0; i < player.DiscardPile.Count; i++)
					privateCards.Add(new Cards.Universal.Dummy());
				this.DiscardPile.AddRange(player, privateCards);
			}
		}
	}
}
