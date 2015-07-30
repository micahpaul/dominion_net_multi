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

namespace Dominion.NET_WPF.Controls.GameLog
{
	public class LogSection : UserControl, IDisposable
	{
		public String LogFile = String.Empty;
		private StackPanel _spContainer = null;

		public StackPanel spContainer { protected get { return _spContainer; } set { _spContainer = value; } }

		public LogSection()
		{
		}

		public virtual void TearDown()
		{
		}

		#region IDisposable variables, properties, & methods
		// Track whether Dispose has been called.
		protected bool disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			// Check to see if Dispose has already been called.
			if (!this.disposed)
			{
				// If disposing equals true, dispose all managed
				// and unmanaged resources.
				if (disposing)
				{
					// Dispose managed resources.
				}

				// Call the appropriate methods to clean up
				// unmanaged resources here.
				// If disposing is false,
				// only the following code is executed.

				// Note disposing has been done.
				disposed = true;
			}
		}

		~LogSection()
		{
			Dispose(false);
		}
		#endregion

		public virtual Boolean IsExpanded { get { return false; } set { } }

		public virtual void New(String title) { }
		public virtual void New(DominionBase.Players.Player player, List<Brush> playerBrushes, DominionBase.Cards.Card grantedBy) { }

		public virtual void Push() { }
		public virtual void Pop() { }

		public virtual void End()
		{
			this.BorderThickness = new Thickness(1, 2, 1, 2);
		}

		public virtual void Log(params Object[] items) { }
		public virtual void Log(DominionBase.Players.Player player, List<Brush> playerBrushes, params Object[] items) { }
		public virtual void Log(DominionBase.Visual.VisualPlayer player, List<Brush> playerBrushes, params Object[] items) { }
	}
}
