using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;

namespace DominionBase.Cards
{
	[Serializable]
	public abstract class CardSetting
	{
		private Object _Value = null;

		public virtual String Name { get { return String.Empty; } }
		public virtual String Text { get { return this.Name; } }
		public virtual String Hint { get { return this.Name; } }
		public virtual Type Type { get { return typeof(Object); } }
		public virtual int DisplayOrder { get { return -1; } }
		public Object Value 
		{ 
			get { return _Value; } 
			set 
			{
				if (value.GetType() != this.Type)
					throw new ArgumentException(String.Format("Type of object is not correct -- expected {0}", this.Type));

				MethodInfo miCompareTo = null;
				if (this.UseLowerBounds)
				{
					Boolean success = false;
					miCompareTo = this.Type.GetMethod("CompareTo", new Type[] { this.Type });
					if (miCompareTo != null)
					{
						Object returnObject = miCompareTo.Invoke(value, new Object[1] { this.LowerBounds });
						if (returnObject.GetType() == typeof(int))
							success = (int)returnObject == 1 || (this.IsLowerBoundsInclusive && (int)returnObject == 0);
					}
					if (!success)
						throw new ArgumentOutOfRangeException("Value is too small for bounds");
				}

				if (this.UseUpperBounds)
				{
					Boolean success = false;
					if (miCompareTo == null)
						miCompareTo = this.Type.GetMethod("CompareTo", new Type[] { this.Type });
					if (miCompareTo != null)
					{
						Object returnObject = miCompareTo.Invoke(value, new Object[1] { this.UpperBounds });
						if (returnObject.GetType() == typeof(int))
							success = (int)returnObject == -1 || (this.IsUpperBoundsInclusive && (int)returnObject == 0);
					}
					if (!success)
						throw new ArgumentOutOfRangeException("Value is too large for bounds");
				}

				_Value = value; 
			} 
		}

		public virtual Object LowerBounds { get { return null; } }
		public virtual Boolean UseLowerBounds { get { return false; } }
		public virtual Boolean IsLowerBoundsInclusive { get { return true; } }
		public virtual Object UpperBounds { get { return null; } }
		public virtual Boolean UseUpperBounds { get { return false; } }
		public virtual Boolean IsUpperBoundsInclusive { get { return true; } }

		public CardSetting() { }
	}

	[Serializable]
	public class CardSettingCollection : List<CardSetting>
	{

		public Boolean ContainsKey(Type tSetting)
		{
			return this.Exists(cs => cs.GetType() == tSetting);
		}

		public CardSetting this[Type tSetting]
		{
			get
			{
				return this.Find(cs => cs.GetType() == tSetting);
			}
			set
			{
				CardSetting foundCS = this.FirstOrDefault(cs => cs.GetType() == tSetting);
				if (foundCS != null)
					this.Remove(foundCS);
				this.Add(value);
			}
		}

		public bool Remove(Type tSetting)
		{
			if (!this.ContainsKey(tSetting))
				return false;
			this.Remove(this.First(cs => cs.GetType() == tSetting));
			return true;
		}
	}

	[Serializable]
	public class CardsSettings
	{
		private String _CardName = null;
		private CardSettingCollection _CardSettingCollection = new CardSettingCollection();

		public String Name { get { return _CardName; } set { _CardName = value; } }
		public CardSettingCollection CardSettingCollection { get { return _CardSettingCollection; } set { _CardSettingCollection = value; } }
		public IOrderedEnumerable<CardSetting> CardSettingOrdered { get { return _CardSettingCollection.OrderBy(cs => cs.DisplayOrder); } }

		private CardsSettings() { }
		public CardsSettings(String cardName)
		{
			this.Name = cardName;
		}
	}

	[Serializable]
	public class CardsSettingsCollection : List<CardsSettings>
	{
		public Boolean ContainsKey(String cardName)
		{
			return this.Exists(cs => cs.Name == cardName);
		}

		public CardsSettings this[String cardName]
		{
			get
			{
				return this.Find(cs => cs.Name == cardName);
			}
			set
			{
				CardsSettings foundCS = this.FirstOrDefault(cs => cs.Name == cardName);
				if (foundCS != null)
					this.Remove(foundCS);
				this.Add(value);
			}
		}

		public bool Remove(String cardName)
		{
			if (!this.ContainsKey(cardName))
				return false;
			this.Remove(this.First(cs => cs.Name == cardName));
			return true;
		}

		public CardsSettingsCollection DeepClone()
		{
			using (MemoryStream ms = new MemoryStream())
			{
				BinaryFormatter formatter = new BinaryFormatter();
				formatter.Serialize(ms, this);
				ms.Position = 0;
				return (CardsSettingsCollection)formatter.Deserialize(ms);
			}
		}
	}

}
