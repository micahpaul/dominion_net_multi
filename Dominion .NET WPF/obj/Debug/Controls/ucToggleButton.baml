   M S B A M L   `   `   `  ����  Dominion .NET WPF.    (Dominion.NET_WPF.Controls.ucToggleButton   5      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      -   5          � 	Resources  5       �� Source�� 5   
   $ ../Resources/Styles.xaml?�6   %    .��5
      � }�. .+�5   
   �� -   5        $��bHover��5      $��2q�6    �� Padding$ 1q�63    �� CornerRadius$ 6��6?    J� Opacity$ 0Q�6P   
���<��6\   d� 5   
   -   5        $��bButton��5         ButtonStyle#���   66   $
��Bold"�6\   $��2q�6n   $��1q�6y   $��Center=�6�   {�. .��5      � $��Center=�6   .+�5      �� -   5        $��tbPre��5      $��Center=�6)   $
��Wrap=�6D   �� 5      -   5        $��	tbDisplay��5      $��Center=�6-   $
��Wrap=�6H   5      5      5       � Triggers 5      :� 5   
    :� RoutedEvent$ UserControl.MouseEnter��6   .[�5      �� .��5      �� .��5      W�  �� 
TargetName$ bHover��6&    �� TargetProperty$ Opacity�6E   	 W� From$	 0X�6i   	
 W� To$
 1X�6r    g� Duration$ 	0:0:0.375D�6y   5      5      5      :� 5   
   $ UserControl.MouseLeave��6   .[�5      �� .��5      �� .��5      W� $ bHover��6&   $ Opacity�6E   $	 1X�6i   $
 0X�6r   $ 	0:0:0.375D�6y   5      5       5!      5#      