﻿#pragma checksum "..\..\..\Controls\ucICardCheckBox.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "392F16BB559AAD22178AEEF87FD18D44"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.0
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Dominion.NET_WPF;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace Dominion.NET_WPF.Controls {
    
    
    /// <summary>
    /// ucICardCheckBox
    /// </summary>
    public partial class ucICardCheckBox : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 10 "..\..\..\Controls\ucICardCheckBox.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel spMain;
        
        #line default
        #line hidden
        
        
        #line 11 "..\..\..\Controls\ucICardCheckBox.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox cbSelected;
        
        #line default
        #line hidden
        
        
        #line 12 "..\..\..\Controls\ucICardCheckBox.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label lName;
        
        #line default
        #line hidden
        
        
        #line 17 "..\..\..\Controls\ucICardCheckBox.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock tbName;
        
        #line default
        #line hidden
        
        
        #line 18 "..\..\..\Controls\ucICardCheckBox.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Image imName;
        
        #line default
        #line hidden
        
        
        #line 23 "..\..\..\Controls\ucICardCheckBox.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ToolTip ttCard;
        
        #line default
        #line hidden
        
        
        #line 24 "..\..\..\Controls\ucICardCheckBox.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Dominion.NET_WPF.ToolTipCard ttcCard;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/Dominion .NET WPF;component/controls/ucicardcheckbox.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\Controls\ucICardCheckBox.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 7 "..\..\..\Controls\ucICardCheckBox.xaml"
            ((Dominion.NET_WPF.Controls.ucICardCheckBox)(target)).MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.UserControl_MouseDown);
            
            #line default
            #line hidden
            
            #line 7 "..\..\..\Controls\ucICardCheckBox.xaml"
            ((Dominion.NET_WPF.Controls.ucICardCheckBox)(target)).MouseUp += new System.Windows.Input.MouseButtonEventHandler(this.UserControl_MouseUp);
            
            #line default
            #line hidden
            
            #line 9 "..\..\..\Controls\ucICardCheckBox.xaml"
            ((Dominion.NET_WPF.Controls.ucICardCheckBox)(target)).IsVisibleChanged += new System.Windows.DependencyPropertyChangedEventHandler(this.UserControl_IsVisibleChanged);
            
            #line default
            #line hidden
            return;
            case 2:
            this.spMain = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 3:
            this.cbSelected = ((System.Windows.Controls.CheckBox)(target));
            
            #line 11 "..\..\..\Controls\ucICardCheckBox.xaml"
            this.cbSelected.Checked += new System.Windows.RoutedEventHandler(this.cbSelected_Checked);
            
            #line default
            #line hidden
            return;
            case 4:
            this.lName = ((System.Windows.Controls.Label)(target));
            return;
            case 5:
            this.tbName = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 6:
            this.imName = ((System.Windows.Controls.Image)(target));
            return;
            case 7:
            this.ttCard = ((System.Windows.Controls.ToolTip)(target));
            return;
            case 8:
            this.ttcCard = ((Dominion.NET_WPF.ToolTipCard)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

