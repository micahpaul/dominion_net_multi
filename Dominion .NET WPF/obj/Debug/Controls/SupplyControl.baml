   M S B A M L   `   `   `  ����L  HDominion .NET WPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null3clr-namespace:Dominion.NET_WPFDominion.NET_WPF  5      $    Dominion.NET_WPF.SupplyControl   5      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      0localclr-namespace:Dominion.NET_WPF     5      -   5        $��42��5      $��42��6   $��0q�6    $	��130��64   $��Stretch=�6C     ^� ShowOnDisabled  .  ^� InitialShowDelay$
 1000��5   *    ^� HasDropShadow .  ^� BetweenShowDelay$	 500��5   )   .��5      �� -   5        $��stackPanel1��5      $��LeftToRight=�6$    �� Orientation$ 
Horizontal=�6@   $��Right=�6Y   .+�5	   
   �� $
��Left=�6   $	��Top=�60   $��12,0,0,0q�6H   .+�5
      �� -   5        $��lName��5
      $��Right=�6!   $
��Left=�6=   $��Center=�6_   $��Trebuchet MS)�6�   $��14(�6�   $
��Bold"�6�   $��RightToLeft=�6�   $
��Auto��6�   $
��Auto��6�   
���OO/�$��1q�6  $��0,3,0,0q�6"  $��6,0,4,0q�63  .��5      �� -   5        $��tbName��5      Name60   5      �� 5      -   5        $��spExtraStuff��5      $��RightToLeft=�6-   $ 
Horizontal=�6I   5      � 5   
   $
��Auto��6   $��41.25��6   .+�5      �� -   5        $��lCost��5      $��Trebuchet MS)�6!   $��14(�6;   $
��Bold"�6I   $��Center=�6[   $��23.25��6   $	��Top=�6�   $��Right=�6�   $��42��6�   $��0q�6�   $��Center=�6�   .��6�   Cost6  �� 5      -   5        $��lCount��5      $��Trebuchet MS)�6"   $��12(�6<   $��Center=�6J   $��1q�6n   $��18.981��6�   $��Bottom=�6�   $��-0.75,0,0,0q�6�   $��0q�6�   $��Center=�6�   ��5      �� 5       �� Color 5      �� 5       �� A$	 255��6$    �� R$	 127��6,    �� G$	 127��64   	 �� B$		 255��6<   5      .��5   &   Cnt5      5      � 5   
   $
��Auto��6   $��61��6   .+�5      �� -   5        $��
imCardIcon��5      $
��Fill=�6&   $��4q�65   �� 5      -	   5        $��
imCardGone��5      $
��Fill=�6&   $y�Hidden=�65   $��4q�6I   �� 5      -
   5        $��imBuyOverlay��5      $
��Fill=�6(   $y�Hidden=�67   $��4q�6K   �� 5       -   5        $��imSelectOverlay��5       $
��Fill=�6+   $y�Hidden=�6:   $��4q�6N   �� 5!      -   5        $��imDisableOverlay��5!      $
��Fill=�6,   $y�Hidden=�6;   $��4q�6O   �� 5"      -   5        $
��bBuy��5"   3   $��0q�6(   
 J� Opacity$
 0Q�6?   $��0q�6K   $��Stretch=�6W   $��Stretch=�6|   $
��Left=�6�   $��61��6�   5#      5$       � ToolTip 5%      _� 5&   
   -   5        $��ttCard��5&      $��0q�6    $��0q�6+   .��5'      "   Dominion.NET_WPF.ToolTipCard  -   5          Name$ ttcCard��5'       5(      5*      