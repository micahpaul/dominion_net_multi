using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DominionBase
{
	public enum ColonyPlatinumUsage
	{
		Standard,
		Always,
		Never,
		Used,
		NotUsed
	}

	public enum ShelterUsage
	{
		Standard,
		Always,
		Never,
		Used,
		NotUsed
	}

	[Serializable]
	public class GameSettings
	{
		private Boolean _IdenticalStartingHands = false;
		private Cards.ConstraintCollection _Constraints = new Cards.ConstraintCollection();
		private Cards.Preset _Preset = null;
		private ColonyPlatinumUsage _ColonyPlatinumUsage = ColonyPlatinumUsage.Standard;
		private ShelterUsage _ShelterUsage = ShelterUsage.Standard;
		private Cards.CardsSettingsCollection _CardSettings = new Cards.CardsSettingsCollection();
		private Boolean _RandomAI_Unique = false;
		private List<String> _RandomAI_AllowedAIs = new List<String>();

		public Boolean IdenticalStartingHands
		{
			get { return _IdenticalStartingHands; }
			set { _IdenticalStartingHands = value; }
		}
		public Cards.ConstraintCollection Constraints
		{
			get { return _Constraints; }
			set { _Constraints = value; }
		}
		public Cards.Preset Preset
		{
			get { return _Preset; }
			set { _Preset = value; }
		}
		public ColonyPlatinumUsage ColonyPlatinumUsage
		{
			get { return _ColonyPlatinumUsage; }
			set { _ColonyPlatinumUsage = value; }
		}

		public ShelterUsage ShelterUsage
		{
			get { return _ShelterUsage; }
			set { _ShelterUsage = value; }
		}

		public Cards.CardsSettingsCollection CardSettings
		{
			get { return _CardSettings; }
			set { _CardSettings = value; }
		}

		public Boolean RandomAI_Unique
		{
			get { return _RandomAI_Unique; }
			set { _RandomAI_Unique = value; }
		}

		public List<String> RandomAI_AllowedAIs
		{
			get { return _RandomAI_AllowedAIs; }
			set { _RandomAI_AllowedAIs = value; }
		}

		public GameSettings()
		{

		}
	}
}
