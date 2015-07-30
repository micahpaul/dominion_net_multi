using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace DominionBase.Players.AI
{
	public class RandomAI : Player, IComputerAI
	{
		public static String AIName { get { return "Random"; } }
		public static String AIDescription { get { return "Randomly chooses one of the other AIs to play as."; } }

		private Basic ActualPlayer = null;

		public String AIType
		{
			get 
			{
				Type myType = null;
				if (this.Phase == PhaseEnum.Endgame)
					myType = this.ActualPlayer.GetType();
				else
					myType = this.GetType();

				return (String)myType.GetProperty("AIName", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy).GetValue(null, null); 
			}
		}

		public RandomAI(Game game, String name)
			: base(game, name)
		{
			_PlayerType = PlayerType.Computer;

			IEnumerable<Type> aiTypes = Assembly.GetExecutingAssembly().GetTypes().Where(x => 
				(x == typeof(Basic) || x.IsSubclassOf(typeof(Basic))) && 
				game.Settings.RandomAI_AllowedAIs.Contains(x.FullName) &&
				(!game.Settings.RandomAI_Unique || !game.Players.Any(p => 
					p.GetType() == x || (p.GetType() == typeof(RandomAI) && ((RandomAI)p).ActualPlayer.GetType() == x))));

			if (aiTypes.Count() == 0)
				throw new GameCreationException(String.Format("Cannot find a fitting AI to choose for Random!{0}Please check your game settings and verify that there are enough AIs to choose from", System.Environment.NewLine));

			Type aiType = Utilities.Shuffler.Choose(aiTypes.ToList());
			this.ActualPlayer = (Basic)aiType.GetConstructor(new Type[] { typeof(Game), typeof(String), typeof(Player) }).Invoke(new object[] { game, name + " (R)", this });
		}

		public AIState State
		{
			get
			{
				return ((Basic)this.ActualPlayer).State;
			}
		}

		public override void StartAsync()
		{
			this.ActualPlayer.StartAsync();
			base.StartAsync();
		}

		internal override void Setup(Game game)
		{
			base.Setup(game);

			this.ActualPlayer.Setup(game, this);
		}

		internal override void Clear()
		{
			this.ActualPlayer.Clear();
			base.Clear();
		}

		internal override void TearDown()
		{
			this.ActualPlayer.TearDown();
			base.TearDown();
		}

		internal override XmlNode GenerateXml(XmlDocument doc)
		{
			XmlNode xnBase = base.GenerateXml(doc);

			XmlElement xe = doc.CreateElement("random_type");
			xe.InnerText = this.ActualPlayer.GetType().ToString();
			xnBase.AppendChild(xe);

			return xnBase;
		}

		internal override void Load(XmlNode xnPlayer)
		{
			base.Load(xnPlayer);

			XmlNode xnRandomType = xnPlayer.SelectSingleNode("random_type");
			if (xnRandomType == null)
				return;

			Type randomType = Type.GetType(xnRandomType.InnerText);
			this.ActualPlayer = (Basic)randomType.GetConstructor(new Type[] { typeof(Game), typeof(String), typeof(Player) }).Invoke(new object[] { this._Game, this.Name + " (R)", this });
		}
	}
}
