﻿#pragma checksum "..\..\..\Controls\ucCardSettings.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "0F038C5C232D178DE959459F28A55194"
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
    /// ucCardSettings
    /// </summary>
    public partial class ucCardSettings : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 9 "..\..\..\Controls\ucCardSettings.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.GroupBox gbCardName;
        
        #line default
        #line hidden
        
        
        #line 11 "..\..\..\Controls\ucCardSettings.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label lName;
        
        #line default
        #line hidden
        
        
        #line 13 "..\..\..\Controls\ucCardSettings.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ToolTip ttCard;
        
        #line default
        #line hidden
        
        
        #line 14 "..\..\..\Controls\ucCardSettings.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Dominion.NET_WPF.ToolTipCard ttcCard;
        
        #line default
        #line hidden
        
        
        #line 20 "..\..\..\Controls\ucCardSettings.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ItemsControl icCardSetting;
        
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
            System.Uri resourceLocater = new System.Uri("/Dominion .NET WPF;component/controls/uccardsettings.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\Controls\ucCardSettings.xaml"
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
            
            #line 8 "..\..\..\Controls\ucCardSettings.xaml"
            ((Dominion.NET_WPF.Controls.ucCardSettings)(target)).IsVisibleChanged += new System.Windows.DependencyPropertyChangedEventHandler(this.UserControl_IsVisibleChanged);
            
            #line default
            #line hidden
            return;
            case 2:
            this.gbCardName = ((System.Windows.Controls.GroupBox)(target));
            return;
            case 3:
            this.lName = ((System.Windows.Controls.Label)(target));
            
            #line 11 "..\..\..\Controls\ucCardSettings.xaml"
            this.lName.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.lName_MouseDown);
            
            #line default
            #line hidden
            
            #line 11 "..\..\..\Controls\ucCardSettings.xaml"
            this.lName.MouseUp += new System.Windows.Input.MouseButtonEventHandler(this.lName_MouseUp);
            
            #line default
            #line hidden
            return;
            case 4:
            this.ttCard = ((System.Windows.Controls.ToolTip)(target));
            return;
            case 5:
            this.ttcCard = ((Dominion.NET_WPF.ToolTipCard)(target));
            return;
            case 6:
            this.icCardSetting = ((System.Windows.Controls.ItemsControl)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

