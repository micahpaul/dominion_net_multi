using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using DominionBase.Cards;

namespace DominionBase.Cards
{
	public class Preset
	{
		private String _Name = String.Empty;
		private CardCollection _Cards = new CardCollection();
		private SerializableDictionary<Card, CardCollection> _CardCards = new SerializableDictionary<Card, CardCollection>();

		public String Name { get { return _Name; } set { _Name = value; } }
		public CardCollection Cards { get { return _Cards; } }
		public SerializableDictionary<Card, CardCollection> CardCards { get { return _CardCards; } }

		public Preset() { }

		public Preset(String name)
		{
			this.Name = name;
		}

		public override string ToString()
		{
			List<Source> sources = new List<Source>(this.Cards.Select(c => c.Source));
			foreach (CardCollection cards in this.CardCards.Values)
				sources.AddRange(cards.Select(c => c.Source));

			return String.Format("{0} - {1}", this.Name, String.Join(", ", sources.Distinct().OrderBy(s => s)));
		}
	}

	public class PresetCollection : List<Preset>
	{
		private static String Filename
		{
			get
			{
				return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "presets.txt");
			}
		}

		public static PresetCollection Parse()
		{
			PresetCollection presets = new PresetCollection();

			CardCollection allCards = CardCollection.GetAllCards(c => c.Location == Location.Kingdom);

			try
			{
				using (System.IO.StreamReader sr = new StreamReader(PresetCollection.Filename))
				{
					Preset currentPreset = null;
					while (!sr.EndOfStream)
					{
						String line = sr.ReadLine().Trim();

						if (String.IsNullOrWhiteSpace(line))
							continue;

						if (line.EndsWith(":"))
						{
							currentPreset = new Preset(line.Substring(0, line.LastIndexOf(':')));
							presets.Add(currentPreset);
							continue;
						}

						if (line.Contains(":"))
						{
							String[] special = line.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
							Card specialKeyCard = allCards.SingleOrDefault(c => c.SpecialPresetKey == special[0].Trim());
							if (specialKeyCard != null)
								specialKeyCard.CheckSetup(currentPreset, allCards.SingleOrDefault(c => c.Name == special[1].Trim()));
						}

						Card foundCard = allCards.SingleOrDefault(c => c.Name == line);
						if (foundCard != null)
							currentPreset.Cards.Add(foundCard);
					}
					sr.Close();
				}
			}
			catch { }
			return presets;
		}
	}
}
