   M S B A M L   `   `   `  ����L  HDominion .NET WPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null3clr-namespace:Dominion.NET_WPFDominion.NET_WPF  5   	   E'clr-namespace:Dominion.NET_WPF.ControlsDominion.NET_WPF.Controls  5   	   /clr-namespace:ScrollableAreaScrollableArea  5   	   %    Dominion.NET_WPF.wCardSelection   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   0localclr-namespace:Dominion.NET_WPF     5   	   8ctrl'clr-namespace:Dominion.NET_WPF.Controls     5   	   +saclr-namespace:ScrollableArea     5   	   -   5          � Title$  Kingdom Card Randomizer��5   	   $	��425��6)   $	��550��66    � WindowStyle$ 
ToolWindow=�6Y    � ShowInTaskbar .  � WindowStartupLocation$ CenterOwner=�6�    � 
ResizeMode$ 	CanResize=�6�   .��5      �� $��1q�6   
���   �._�5	   
   ]� .+�5
      �� $��Bottom=�6    �� Orientation$ 
Horizontal=�61   $��Right=�6J   .+�5      �� -   5        $��cbAutoAccept��5      $��3q�6/   $��Don't ask in the future��6:   $��Center=�6\   $��Center=�6w    � ToolTip$V PThis setting can be changed on the Kingdom Card Setup tab in the Settings dialog��6�   (   "Dominion.NET_WPF.Controls.ucButton  5      -   5          Name$ bAccept��5       (   "Dominion.NET_WPF.Controls.ucButton   Click$ bAccept_Click��6o   	   Text$	 _Accept��61   $��3q�6@   
   TextPadding$

 10,5q�6K      	IsDefault . $3 -Accept these Kingdom cards and play with them��6�     5      -   5        $ 
bReshuffle��5       $ bReshuffle_Click��6d   $	 
_Reshuffle��64   $��3q�6F   $

 10,5q�6Q   $$ Re-randomize the Kingdom cards��6}     5      -   5        $ bCancel��5       $	 _Cancel��61   $��3q�6@   $

 10,5q�6K      IsCancel . $# Cancel Kingdom card selection��6n   5      �� 5      $	��Top=�6   $��1q�6*   
���   �._�5      ]� .+�5      ]� $	��Top=�6    .+�5      � $��6��6   $��6��6)   
�������$
��Left=�6K   .+�5      �� $��4��6%   $��4��60   
����� �$��1q�6T   
����  �$��1,0,0,0q�6~   $
��Left=�6�   5      � 5      -   5        $��6��5      
�������.+�5      �� -   5        $(��"bSetCategoryGroupDisplayHorizontal��5   %   $��4��6O   $��55��6Z   
����� �$	��0,1q�6   
����  �$��0,0,0,0q�6�   $
��Left=�6�   5      5      ]� 5      $
��Left=�6    .+�5      � -   5        $��6��5      
�������.+�5      �� -	   5        $&�� bSetCategoryGroupDisplayVertical��5   %   $��4��6M   $��44��6W   
����� �$	��1,0q�6}   
����  �$��0,0,0,0q�6�   $	��top=�6�   5      5      � 5      .+�5       �� -
   5        $��svSetCategoryGroupDisplay��5    '   $��Disabled=�6H   $��Hidden=�6r   $��Stretch=�6�   $��1q�6�   
���   �$��Trebuchet MS)�6�   $   ScrollableArea.KineticBehavior   HandleKineticScrolling .    Friction$
 0.90Q�6&  .��5!      ]� -   5        $��dpCards��5!   (   ��.  .+�5"   "   � -   5        $��wpKingdomCards��5"   ,    � Orientation$ 
Horizontal=�6B   $	��Top=�6[   .+�5#   &   ,   &Dominion.NET_WPF.CardCollectionControl  -   5          Name$ cccKingdomCards��5#   B      
PlayerMode$ Waiting=�6[      PileName$ Kingdom cards��6q      IsCardsVisible .    IsDisplaySorted .    CardSize$ Small=�6�      
ExactCount .  $	��Top=�6�   ��. $��1��6     MinStackWidth$ 85Q�6  5$   #   � 5%   "   -   5        $��wpGeneralCards��5%   ,   $ 
Horizontal=�6B   $	��Top=�6[   .+�5&   &     -   5        $ cccGeneralCards��5&   B   $ Waiting=�6[   $ Additional cards��6q    .  . $ Small=�6�    .  $	��Top=�6�   ��. $��2��6  $ 85Q�6  5'   #   ��5(   "   �� 5)   &    �� EndPoint$ 1,0.5&�6:    �� 
StartPoint$ 0,0.5&�6K   .��5*   *   �  � Color$ White��67    � Offset$ 0Q�6E   � 5+   *   -   5        
gsStop��$ 	AliceBlue��5+   G   $ 0Q�6Y   � 5,   *   $ White��67   $ 1Q�6E   5-   '    � Triggers 5/   "   :� 50   &    :� RoutedEvent$ Loaded��63   .[�51   *   �� .��52   .   ��  g� RepeatBehavior$ Forever��69   .��53   2   W�  �� 
TargetName$ gsStop��6B     �� TargetProperty$  Offset�6a   ! W� From$! 0X�6�   	" W� To$" 1X�6�   # g� Duration$# 0:0:5D�6�   $ g� AutoReverse$ . 6�   54   /   55   +   56   '   58      59      �� 5:      -   5        $��bOpacityLayerTop��5:   6   $	��Top=�6!   % J� IsHitTestVisible% .  $��20��6g   $	��Top=�6s   $y�	Collapsed=�6�   ��5;      �� 5<   "   $	 0,0&�66   $	 0,1&�6G   .��5=   &   � � 5>   *   $ White��67   $ 0Q�6E   � 5?   *   $ Transparent��67   $ 1Q�6K   5@   '   5A   #   5C      �� 5D      -   5        $��bOpacityLayerLeft��5D   7   $
��Left=�6!   % .  $��20��6i   $
��Left=�6t   $y�	Collapsed=�6�   ��5E      �� 5F   "   $	 0,0&�66   $	 1,0&�6G   .��5G   &   � � 5H   *   $ White��67   $ 0Q�6E   � 5I   *   $ Transparent��67   $ 1Q�6K   5J   '   5K   #   5M      �� 5N      -   5        $��bOpacityLayerRight��5N   8   $��Right=�6!   % .  $��20��6k   $��Right=�6v   $y�	Collapsed=�6�   ��5O      �� 5P   "   $	 1,0&�66   $	 0,0&�6G   .��5Q   &   � � 5R   *   $ White��67   $ 0Q�6E   � 5S   *   $ Transparent��67   $ 1Q�6K   5T   '   5U   #   5W      �� 5X      -   5        $��bOpacityLayerBottom��5X   9   $��Bottom=�6!   % .  $��20��6m   $��Bottom=�6y   $y�	Collapsed=�6�   ��5Y      �� 5Z   "   $	 0,1&�66   $	 0,0&�6G   .��5[   &   � � 5\   *   $ White��67   $ 0Q�6E   � 5]   *   $ Transparent��67   $ 1Q�6K   5^   '   5_   #   5a      5b      5c      5d      5e      5f      5g      