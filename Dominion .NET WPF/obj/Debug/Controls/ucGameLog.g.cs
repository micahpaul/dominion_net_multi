﻿#pragma checksum "..\..\..\Controls\ucGameLog.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "B424E166920658A0948475FA84E1C741"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.0
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Dominion.NET_WPF.Controls.GameLog;
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
    /// ucGameLog
    /// </summary>
    public partial class ucGameLog : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 7 "..\..\..\Controls\ucGameLog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Dominion.NET_WPF.Controls.ucGameLog ucGameLogName;
        
        #line default
        #line hidden
        
        
        #line 8 "..\..\..\Controls\ucGameLog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ScrollViewer svArea;
        
        #line default
        #line hidden
        
        
        #line 11 "..\..\..\Controls\ucGameLog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel spArea;
        
        #line default
        #line hidden
        
        
        #line 17 "..\..\..\Controls\ucGameLog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.MenuItem miViewGameLog;
        
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
            System.Uri resourceLocater = new System.Uri("/Dominion .NET WPF;component/controls/ucgamelog.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\Controls\ucGameLog.xaml"
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
            this.ucGameLogName = ((Dominion.NET_WPF.Controls.ucGameLog)(target));
            return;
            case 2:
            this.svArea = ((System.Windows.Controls.ScrollViewer)(target));
            return;
            case 3:
            this.spArea = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 4:
            
            #line 16 "..\..\..\Controls\ucGameLog.xaml"
            ((System.Windows.Controls.ContextMenu)(target)).Opened += new System.Windows.RoutedEventHandler(this.ContextMenu_Opened);
            
            #line default
            #line hidden
            return;
            case 5:
            this.miViewGameLog = ((System.Windows.Controls.MenuItem)(target));
            
            #line 17 "..\..\..\Controls\ucGameLog.xaml"
            this.miViewGameLog.Click += new System.Windows.RoutedEventHandler(this.CurrentGame_ViewGameLog_Click);
            
            #line default
            #line hidden
            return;
            case 6:
            
            #line 18 "..\..\..\Controls\ucGameLog.xaml"
            ((System.Windows.Controls.MenuItem)(target)).Click += new System.Windows.RoutedEventHandler(this.miCollapseAll_Click);
            
            #line default
            #line hidden
            return;
            case 7:
            
            #line 19 "..\..\..\Controls\ucGameLog.xaml"
            ((System.Windows.Controls.MenuItem)(target)).Click += new System.Windows.RoutedEventHandler(this.miExpandAll_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

