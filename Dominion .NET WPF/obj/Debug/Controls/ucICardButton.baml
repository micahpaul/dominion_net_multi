   M S B A M L   `   `   `  ����L  HDominion .NET WPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null3clr-namespace:Dominion.NET_WPFDominion.NET_WPF  5      -    'Dominion.NET_WPF.Controls.ucICardButton   5      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      0localclr-namespace:Dominion.NET_WPF     5      -   5        $	��3,0q�5        ^� ShowOnDisabled  .  ^� InitialShowDelay$
 1000��5   3    ^� HasDropShadow .  ^� BetweenShowDelay$	 250��5	   2   .��5
      �� -   5        $��bControl��5
      $��2q�6    �� Padding$ 1q�61    �� CornerRadius$ 2��6=   ._�5   
   � .+�5      �� -   5        $��bHover��5      $��2q�6#   $ 1q�67   $ 2��6C    J� Opacity$ 0Q�6T   
���<��6`   �� 5       �� Orientation$ 
Horizontal=�6   $��2q�62   .+�5      �� -   5        $��lOrdinal��5      $��Trebuchet MS)�6(   $��Center=�6B   $��Center=�6_   $	��0,2q�6�   $��Center=�6�   .��5      �� -   5        $��	tbOrdinal��5        �� TextAlignment$ Center=�61   	 �� Padding$		 4,0q�6H   0th6[   5      �� 5      -   5        $��lName��5      $��Trebuchet MS)�6%   $
��Bold"�6?   $��Center=�6Q   $��Center=�6n   
���OO/�$��1q�6�   $��0q�6�   $y�Hidden=�6�   .��5      � .+�5      �� -   5        $��tbName��5   $   
 K� ZIndex$
 0��62   $ Center=�6C   $	��4,2q�6Z   6i   d� 5      -   5        $��bName��5   '   $
 1��64   $ 0Q�6E   6Q   5      5      5      5      5       � ToolTip 5      _� 5   
   -	   5        $��ttCard��5      $��0q�6    $��0q�6+   .��5      "   Dominion.NET_WPF.ToolTipCard  -
   5          Name$ ttcCard��5       5       � Triggers 5      :� 5    
    :� RoutedEvent$ UserControl.MouseEnter��6   .[�5!      �� .��5"      �� .��5#      W�  �� 
TargetName$ bHover��6&    �� TargetProperty$ Opacity�6E    W� From$ 0X�6i   	 W� To$ 1X�6r    g� Duration$ 	0:0:0.375D�6y   5$      5%      5&      :� 5'   
   $ UserControl.MouseLeave��6   .[�5(      �� .��5)      �� .��5*      W� $ bHover��6&   $ Opacity�6E   $ 1X�6i   $ 0X�6r   $ 	0:0:0.375D�6y   5+      5,      5-      5/      