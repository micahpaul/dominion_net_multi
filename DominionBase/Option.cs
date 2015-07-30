using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DominionBase
{
	public class Option : IComparable<Option>
	{
		private String _Text;
		private String _Value;
		private Boolean _IsRequired;

		public String Text 
		{ 
			get { return _Text; } 
			private set 
			{ 
				_Text = value; 
				if (String.IsNullOrEmpty(this.Value))
					this.Value = value;
			} 
		}
		public String Value { get { return _Value; } private set { _Value = value; } }
		public Boolean IsRequired { get { return _IsRequired; } private set { _IsRequired = value; } }

		public Option(String text) : this(text, text, false) { }
		public Option(String text, String value) : this(text, value, false) { }
		public Option(String text, Boolean isRequired) : this(text, text, isRequired) { }
		public Option(String text, String value, Boolean isRequired)
		{
			this.Text = text;
			this.IsRequired = isRequired;
			this.Value = value;
		}

		public int CompareTo(Option option)
		{
			if (ReferenceEquals(this, option))
				return 0;
			int v = this.Text.CompareTo(option.Text);
			if (v != 0)
				return v;
			v = this.Value.CompareTo(option.Value);
			if (v != 0)
				return v;
			return this.IsRequired.CompareTo(option.IsRequired);
		}
	}

	public class OptionCollection : List<Option>
	{
		public Boolean IsAnyRequired { get { return this.Any(o => o.IsRequired); } }
		public Boolean IsAllRequired { get { return this.All(o => o.IsRequired); } }

		public Boolean Contains(String item) { return this.Any(o => o.Text == item); }
		public void Add(String item) { this.Add(new Option(item)); }
		public void Add(String item, String value) { this.Add(new Option(item, value)); }
		public void Add(String item, Boolean isRequired) { this.Add(new Option(item, isRequired)); }
		public int IndexOf(String item) { return this.FindIndex(o => o.Text == item); }
		public Boolean Remove(String item)
		{
			if (!this.Contains(item))
				return false;
			this.Remove(this.Find(o => o.Text == item));
			return true;
		}

		public OptionCollection() : base() { }
		public OptionCollection(IEnumerable<Option> collection) : base(collection) { }
		public OptionCollection(IEnumerable<String> collection)
		{
			foreach (String item in collection)
				this.Add(new Option(item));
		}
	}
}
