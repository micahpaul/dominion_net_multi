using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace DominionBase.Players
{
	public class PlayerCollection : List<Player>
	{
		public Player Next(Player currentPlayer)
		{
			if (!this.Contains(currentPlayer))
				throw new ArgumentOutOfRangeException(currentPlayer.ToString(), "Could not find Player!");
			return this[(this.IndexOf(currentPlayer) + 1) % this.Count];
		}

		public IEnumerator<Player> GetPlayersStartingWithEnumerator(Player player)
		{
			int offset = player == null || !this.Contains(player) ? 0 : this.IndexOf(player);
			for (int i = 0; i < this.Count; i++)
			{
				yield return this[(i + offset) % this.Count];
			}
		}

		public static IEnumerable<Type> GetAllAIs()
		{
			return Assembly.GetExecutingAssembly().GetTypes().Where(x => x.Namespace.StartsWith("DominionBase.Players.AI") && x.IsSubclassOf(typeof(DominionBase.Players.Player)));
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (Player player in this)
			{
				if (sb.Length > 0)
					sb.Append(", ");
				sb.Append(player);
			}
			return sb.ToString();
		}

		internal virtual void Setup(Game game)
		{
			foreach (Player player in this)
				player.Setup(game);
		}

		internal void TearDown()
		{
			foreach (Player player in this)
				player.TearDown();
		}

		internal new void Clear()
		{
			foreach (Player player in this)
				player.Clear();
			base.Clear();
		}

		internal void InitializeDecks()
		{
			foreach (Player player in this)
				player.InitializeDeck();
		}

		internal void FinalizeDecks()
		{
			foreach (Player player in this)
				player.FinalizeDeck();
		}

		internal XmlNode GenerateXml(XmlDocument doc)
		{
			XmlElement xePlayers = doc.CreateElement("players");
			foreach (Player player in this)
				xePlayers.AppendChild(player.GenerateXml(doc));

			return xePlayers;
		}
	}
}
