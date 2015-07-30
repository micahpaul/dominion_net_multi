using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ScrollableArea
{
	public class KineticBehavior
	{
		#region Friction

		/// <summary>
		/// Friction Attached Dependency Property
		/// </summary>
		public static readonly DependencyProperty FrictionProperty =
			DependencyProperty.RegisterAttached("Friction", typeof(double), typeof(KineticBehavior),
				new FrameworkPropertyMetadata((double)0.95));

		/// <summary>
		/// Gets the Friction property.  This dependency property 
		/// indicates ....
		/// </summary>
		public static double GetFriction(DependencyObject d)
		{
			return (double)d.GetValue(FrictionProperty);
		}

		/// <summary>
		/// Sets the Friction property.  This dependency property 
		/// indicates ....
		/// </summary>
		public static void SetFriction(DependencyObject d, double value)
		{
			d.SetValue(FrictionProperty, value);
		}

		#endregion

		#region ScrollStartPoint

		/// <summary>
		/// ScrollStartPoint Attached Dependency Property
		/// </summary>
		private static readonly DependencyProperty ScrollStartPointProperty =
			DependencyProperty.RegisterAttached("ScrollStartPoint", typeof(Point), typeof(KineticBehavior),
				new FrameworkPropertyMetadata((Point)new Point()));

		/// <summary>
		/// Gets the ScrollStartPoint property.  This dependency property 
		/// indicates ....
		/// </summary>
		private static Point GetScrollStartPoint(DependencyObject d)
		{
			return (Point)d.GetValue(ScrollStartPointProperty);
		}

		/// <summary>
		/// Sets the ScrollStartPoint property.  This dependency property 
		/// indicates ....
		/// </summary>
		private static void SetScrollStartPoint(DependencyObject d, Point value)
		{
			d.SetValue(ScrollStartPointProperty, value);
		}

		#endregion

		#region ScrollStartOffset

		/// <summary>
		/// ScrollStartOffset Attached Dependency Property
		/// </summary>
		private static readonly DependencyProperty ScrollStartOffsetProperty =
			DependencyProperty.RegisterAttached("ScrollStartOffset", typeof(Point), typeof(KineticBehavior),
				new FrameworkPropertyMetadata((Point)new Point()));

		/// <summary>
		/// Gets the ScrollStartOffset property.  This dependency property 
		/// indicates ....
		/// </summary>
		private static Point GetScrollStartOffset(DependencyObject d)
		{
			return (Point)d.GetValue(ScrollStartOffsetProperty);
		}

		/// <summary>
		/// Sets the ScrollStartOffset property.  This dependency property 
		/// indicates ....
		/// </summary>
		private static void SetScrollStartOffset(DependencyObject d, Point value)
		{
			d.SetValue(ScrollStartOffsetProperty, value);
		}

		#endregion

		#region InertiaProcessor

		/// <summary>
		/// InertiaProcessor Attached Dependency Property
		/// </summary>
		private static readonly DependencyProperty InertiaProcessorProperty =
			DependencyProperty.RegisterAttached("InertiaProcessor", typeof(InertiaHandler), typeof(KineticBehavior),
				new FrameworkPropertyMetadata((InertiaHandler)null));

		/// <summary>
		/// Gets the InertiaProcessor property.  This dependency property 
		/// indicates ....
		/// </summary>
		private static InertiaHandler GetInertiaProcessor(DependencyObject d)
		{
			return (InertiaHandler)d.GetValue(InertiaProcessorProperty);
		}

		/// <summary>
		/// Sets the InertiaProcessor property.  This dependency property 
		/// indicates ....
		/// </summary>
		private static void SetInertiaProcessor(DependencyObject d, InertiaHandler value)
		{
			d.SetValue(InertiaProcessorProperty, value);
		}

		#endregion

		#region HandleKineticScrolling

		/// <summary>
		/// HandleKineticScrolling Attached Dependency Property
		/// </summary>
		public static readonly DependencyProperty HandleKineticScrollingProperty =
			DependencyProperty.RegisterAttached("HandleKineticScrolling", typeof(bool), 
			typeof(KineticBehavior),
				new FrameworkPropertyMetadata((bool)false,
					new PropertyChangedCallback(OnHandleKineticScrollingChanged)));

		/// <summary>
		/// Gets the HandleKineticScrolling property.  This dependency property 
		/// indicates ....
		/// </summary>
		public static bool GetHandleKineticScrolling(DependencyObject d)
		{
			return (bool)d.GetValue(HandleKineticScrollingProperty);
		}

		/// <summary>
		/// Sets the HandleKineticScrolling property.  This dependency property 
		/// indicates ....
		/// </summary>
		public static void SetHandleKineticScrolling(DependencyObject d, bool value)
		{
			d.SetValue(HandleKineticScrollingProperty, value);
		}

		/// <summary>
		/// Handles changes to the HandleKineticScrolling property.
		/// </summary>
		private static void OnHandleKineticScrollingChanged(DependencyObject d, 
			DependencyPropertyChangedEventArgs e)
		{
			ScrollViewer scroller = d as ScrollViewer;
			if ((bool)e.NewValue)
			{
				scroller.PreviewMouseDown += OnPreviewMouseDown;
				scroller.PreviewMouseMove += OnPreviewMouseMove;
				scroller.PreviewMouseUp += OnPreviewMouseUp;
				SetInertiaProcessor(scroller, new InertiaHandler(scroller));
			}
			else
			{
				scroller.PreviewMouseDown -= OnPreviewMouseDown;
				scroller.PreviewMouseMove -= OnPreviewMouseMove;
				scroller.PreviewMouseUp -= OnPreviewMouseUp;
				var inertia = GetInertiaProcessor(scroller);
				if (inertia != null)
					inertia.Dispose();
			}
			
		}

		#endregion

		#region Mouse Events
		private static void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			ScrollViewer scrollViewer = (ScrollViewer)sender;

			if (e.ChangedButton == MouseButton.Left &&
				scrollViewer.IsMouseOver &&
				!(e.Source is Dominion.NET_WPF.Controls.CardStackControl) &&
				(e.OriginalSource is ScrollViewer || e.OriginalSource is TextBlock ||
				e.OriginalSource is DockPanel || e.OriginalSource is Image || e.OriginalSource is Label))
			{
				// Save starting point, used later when determining how much to scroll.
				SetScrollStartPoint(scrollViewer, e.GetPosition(scrollViewer));
				SetScrollStartOffset(scrollViewer, new 
					Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset));
				scrollViewer.Cursor = Cursors.ScrollAll;
				scrollViewer.CaptureMouse();
			}
		}


		private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
		{
			ScrollViewer scrollViewer = (ScrollViewer)sender;
			if (scrollViewer.IsMouseCaptured)
			{
				Point currentPoint = e.GetPosition(scrollViewer);

				Point scrollStartPoint = GetScrollStartPoint(scrollViewer);
				// Determine the new amount to scroll.
				Point delta = new Point(scrollStartPoint.X - currentPoint.X, 
					scrollStartPoint.Y - currentPoint.Y);

				Point scrollStartOffset = GetScrollStartOffset(scrollViewer);
				Point scrollTarget = new Point(scrollStartOffset.X + delta.X, 
					scrollStartOffset.Y + delta.Y);

				InertiaHandler inertiaProcessor = GetInertiaProcessor(scrollViewer);
				if (inertiaProcessor != null)
					inertiaProcessor.ScrollTarget = scrollTarget;
				
				// Scroll to the new position.
				scrollViewer.ScrollToHorizontalOffset(scrollTarget.X);
				scrollViewer.ScrollToVerticalOffset(scrollTarget.Y);
			}
		}

		private static void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			ScrollViewer scrollViewer = (ScrollViewer)sender;
			if (scrollViewer.IsMouseCaptured)
			{
				scrollViewer.Cursor = null;
				scrollViewer.ReleaseMouseCapture();
			}
		}
		#endregion

		#region Inertia Stuff

		/// <summary>
		/// Handles the inertia 
		/// </summary>
		class InertiaHandler : IDisposable
		{
			private Point previousPoint;
			private Vector velocity;
			ScrollViewer scroller;
			DispatcherTimer animationTimer;

			private Point scrollTarget;
			public Point ScrollTarget { 
				get { return scrollTarget; } 
				set { scrollTarget = value; } }

			public InertiaHandler(ScrollViewer scroller)
			{
				this.scroller = scroller;
				animationTimer = new DispatcherTimer();
				animationTimer.Interval = new TimeSpan(0, 0, 0, 0, 20);
				animationTimer.Tick += new EventHandler(HandleWorldTimerTick);
				animationTimer.Start();
			}

			private void HandleWorldTimerTick(object sender, EventArgs e)
			{
				if (scroller.IsMouseCaptured)
				{
					Point currentPoint = Mouse.GetPosition(scroller);
					velocity = previousPoint - currentPoint;
					previousPoint = currentPoint;
				}
				else
				{
					if (velocity.Length > 1)
					{
						scroller.ScrollToHorizontalOffset(ScrollTarget.X);
						scroller.ScrollToVerticalOffset(ScrollTarget.Y);
						scrollTarget.X += velocity.X;
						scrollTarget.Y += velocity.Y;
						velocity *= KineticBehavior.GetFriction(scroller);
					}
				}
			}

			#region IDisposable Members

			public void Dispose()
			{
				animationTimer.Stop();
			}

			#endregion
		}

		#endregion
	}
}
