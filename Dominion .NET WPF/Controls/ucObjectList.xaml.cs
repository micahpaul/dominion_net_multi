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
	/// Interaction logic for ucObjectList.xaml
	/// </summary>
	public partial class ucObjectList : UserControl
	{
		public static readonly DependencyProperty ObjectsProperty =
			DependencyProperty.Register("Objects", typeof(IEnumerable<Object>), typeof(ucObjectList), new PropertyMetadata(null));
		public IEnumerable<Object> Objects
		{
			get { return (IEnumerable<Object>)this.GetValue(ObjectsProperty); }
			set
			{
				this.SetValue(ObjectsProperty, value);

				wpObjects.Children.Clear();
				if (value == null)
					return;

				foreach (Object obj in value)
				{
					if (obj == null)
					{
						// Let's throw in a spacer
						wpObjects.Children.Add(new Label { Margin = new Thickness(5, 0, 0, 0) });
					}
					else if ((obj as DominionBase.ICard) != null)
					{
						wpObjects.Children.Add(new Controls.ucCardIcon { Size = CardSize.Text, Card = (obj as DominionBase.ICard) });
					}
					else if ((obj as DominionBase.Token) != null)
					{
						wpObjects.Children.Add(new Controls.ucTokenIcon { Token = (obj as DominionBase.Token), Margin = new Thickness(2, 5, 2, 5) });
					}
				}
			}
		}

		public ucObjectList()
		{
			InitializeComponent();
		}
	}
}
