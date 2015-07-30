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
	/// Interaction logic for ucCardConstraints.xaml
	/// </summary>
	public partial class ucCardConstraints : UserControl
	{
		public static readonly DependencyProperty ConstraintCollectionProperty =
			DependencyProperty.Register("ConstraintCollection", typeof(DominionBase.Cards.ConstraintCollection), typeof(ucCardConstraints),
			new PropertyMetadata(new DominionBase.Cards.ConstraintCollection()));
		public DominionBase.Cards.ConstraintCollection ConstraintCollection
		{
			get { return (DominionBase.Cards.ConstraintCollection)this.GetValue(ConstraintCollectionProperty); }
			set { 
				this.SetValue(ConstraintCollectionProperty, value);
				foreach (DominionBase.Cards.Constraint constraint in value)
					icConstraints.Items.Add(new ucCardConstraint { Constraint = constraint });
			}
		}

		public ucCardConstraints()
		{
			InitializeComponent();
		}

		private void spConstraints_RemoveClick(object sender, RoutedEventArgs e)
		{
			this.ConstraintCollection.Remove((e.OriginalSource as ucCardConstraint).Constraint);
			icConstraints.Items.Remove(e.OriginalSource as ucCardConstraint);
			//this.Settings.Constraints.Remove((e.Source as Controls.CardConstraintControl).Constraint);
			//spConstraints.Children.Remove(e.Source as Controls.CardConstraintControl);
		}

		private void bAddNew_Click(object sender, RoutedEventArgs e)
		{
			DominionBase.Cards.Constraint constraint = new DominionBase.Cards.Constraint();
			this.ConstraintCollection.Add(constraint);
			icConstraints.Items.Add(new ucCardConstraint { Constraint = constraint });
		}
	}
}
