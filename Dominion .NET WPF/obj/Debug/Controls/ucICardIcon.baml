   M S B A M L   `   `   `  ����L  HDominion .NET WPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null3clr-namespace:Dominion.NET_WPFDominion.NET_WPF  5      +    %Dominion.NET_WPF.Controls.ucICardIcon   5      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      0localclr-namespace:Dominion.NET_WPF     5      -   5        $	��3,0q�5        ^� ShowOnDisabled  .  ^� InitialShowDelay$
 1000��5   3    ^� HasDropShadow .  ^� BetweenShowDelay$	 250��5	   2   .��5
      ��  �� Orientation$ 
Horizontal=�6   $��2q�6*   .+�5   
   �� -   5        $��lName��5      $��Trebuchet MS)�6   $
��Bold"�67   $��Center=�6I   $��Center=�6f   
���OO/�$��1q�6�   $��0q�6�   $y�Hidden=�6�   .��5      � .+�5      �� -   5        $��tbName��5       �� TextAlignment$ Center=�6*   $	��2,1q�6A   $y�Visible=�6N   �� 5      -   5        $��imName��5      $y�	Collapsed=�6&   $
��Fill=�6=   5      5      5       � ToolTip 5      _� 5   
   -   5        $��ttCard��5      $��0q�6    $��0q�6+   .��5      "   Dominion.NET_WPF.ToolTipCard  -   5          Name$ ttcCard��5       5       � Triggers 5      :� 5   
   	 :� RoutedEvent$	 UserControl.MouseEnter��6   .[�5      �� .��5      �� .��5      W� 
 �� 
TargetName$
 bHover��6&    �� TargetProperty$ Opacity�6E    W� From$ 0X�6i   	 W� To$ 1X�6r    g� Duration$ 	0:0:0.375D�6y   5      5      5      :� 5   
   $	 UserControl.MouseLeave��6   .[�5       �� .��5!      �� .��5"      W� $
 bHover��6&   $ Opacity�6E   $ 1X�6i   $ 0X�6r   $ 	0:0:0.375D�6y   5#      5$      5%      5'      