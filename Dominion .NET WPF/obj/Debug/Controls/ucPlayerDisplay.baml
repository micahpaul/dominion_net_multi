   M S B A M L   `   `   `  ����L  HDominion .NET WPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null3clr-namespace:Dominion.NET_WPFDominion.NET_WPF  5   	   E'clr-namespace:Dominion.NET_WPF.ControlsDominion.NET_WPF.Controls  5   	   /clr-namespace:ScrollableAreaScrollableArea  5   	   /    )Dominion.NET_WPF.Controls.ucPlayerDisplay   5      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   0localclr-namespace:Dominion.NET_WPF     5   	   8ctrl'clr-namespace:Dominion.NET_WPF.Controls     5   	   +saclr-namespace:ScrollableArea     5   	   0   *Dominion.NET_WPF.Controls.CardStackControl    CardStackControlClick$6  0CardCollectionControl_CardCollectionControlClick��5	   	   .��5      ��  �� CornerRadius$ 2��6   $��2q�6   
����� �._�5   
   ]� .+�5      ]� $��Bottom=�6   .+�5      �� -   5        $��bStuffDivider��5       K� ZIndex$ 1��6.   $��1q�6?   
����� �$	��Top=�6m   ]� 5      -   5        $��dpStuff��5      ��.  
�������.+�5      �� -   5        $��
lStageText��5      $��Stage:��6.   $��Trebuchet MS)�6?   $��16(�6Y   $
��Bold"�6g   $
��Left=�6y   $��Right=�6�   
�������6�   �� 5      -   5        $��lStage��5      $
��Left=�6*   $��Center=�6@   $��0,0,5,0q�6b   $
��Bold"�6t   
�������6�   �� 5      -   5        $��bVictoryPointsDivider��5      $ 1��6:   $��1q�6K   
���   �$
��Left=�6s   $y�	Collapsed=�6�   �� 5      -   5        $��lVictoryPointsTitle��5      $��Victory Points:��67   $��Trebuchet MS)�6Q   $��16(�6k   $
��Bold"�6y   $
��Left=�6�   $��Right=�6�   
������$y�	Collapsed=�6�   �� 5      -   5        $��lVictoryPoints��5      $
��Left=�62   $��Center=�6H   $��0,0,5,0q�6j   $
��Bold"�6|   
���   �
������$y�	Collapsed=�6�   5      5      ]� 5      $	��Top=�6   .+�5      � $��6��6   $��6��6!   
�������$
��Left=�6C   .+�5      �� $��4��6   $��4��6(   
����� �$��1q�6L   
����  �$��1,0,0,0q�6v   $
��Left=�6�   5      � 5      -   5        $��6��5      
�������.+�5      �� -	   5        $��bHorizontal��5      $��4��60   $��55��6;   
����� �$	��0,1q�6`   
����  �$��0,0,0,0q�6�   $
��Left=�6�   5      5      ]� 5      $
��Left=�6   .+�5       � -
   5        $��6��5       
�������.+�5!      �� -   5        $��	bVertical��5!      $��4��6.   $��44��68   
����� �$	��1,0q�6^   
����  �$��0,0,0,0q�6�   $	��top=�6�   5"      5#      � 5$      .+�5%      �� -   5         ��Name$ svPlayerDisplay��5%      $��Hidden=�68   $��Hidden=�6_   $��Stretch=�6�   $��1q�6�   
���   �$   ScrollableArea.KineticBehavior   HandleKineticScrolling .    Friction$
 0.90Q�6�   .��5&      ]� -   5        $��dpPlayerDisplay��5&       ��.  .+�5'      ]� ��.  $	��Top=�6:   .+�5(      ,   &Dominion.NET_WPF.CardCollectionControl  -   5          Name$ cardDiscard��5(   :   $
��Auto��6O   $��0,0,0,0q�6]   $
��Left=�6p   �� 5)      -   5        $��bDeckDiscardDivider��5)   %   $��2q�6@   
���   �$
��Left=�6h   $y�	Collapsed=�6~     5*      -   5        $ cardDeck��5*   :   $
��Auto��6L   $��0,0,0,0q�6Z   $
��Left=�6m   �� 5+      -   5        $��bDeckHandDivider��5+   %   $��2q�6=   
���   �$
��Left=�6e   � 5,      -   5        $��wpHand��5,   (    � Orientation$ 
Horizontal=�66   $
��Left=�6O   .+�5-   "     -   5        $ cardHand��5-   >   $
��Auto��6P   $��0,0,0,0q�6^   5.      5/      �� 50      -   5        $��bHandInPlayivider��50   !   $ 1��6:   $��1q�6K   
���   �$	��Top=�6s   ]� 51      -   5        $��dpInPlay��51   $   ��.  $	��Top=�6J   .+�52        -   5        $ 
cardInPlay��52   :   $
��Auto��6N   $��0,0,0,0q�6\   $
��Left=�6o   �� 53      -   5        $��bSetAsideDivider��53   %   $��2q�6=   
���   �$
��Left=�6e   $y�	Collapsed=�6{     54      -   5        $ cardSetAside��54   :   $
��Auto��6P   $��0,0,0,0q�6^   $
��Left=�6q   $y�	Collapsed=�6�   �� 55      -   5        $��bInPlayRevealedDivider��55   %   $��2q�6C   
���   �$
��Left=�6k   $y�	Collapsed=�6�     56      -   5        $ cardRevealed��56   :   $
��Auto��6P   $��0,0,0,0q�6^   $
��Left=�6q   $y�	Collapsed=�6�   �� 57      -   5        $��bRevealedLookingAtDivider��57   %   $��2q�6F   
���   �$
��Left=�6n   $y�	Collapsed=�6�     58      -   5        $ cardPrivate��58   :   $
��Auto��6O   $��0,0,0,0q�6]   $
��Left=�6p   $y�	Collapsed=�6�   59      �� 5:      -   5        $��bInPlayMatsDivider��5:   !   $ 1��6;   $��1q�6L   
���   �$	��Top=�6t   ]� 5;      -   5        $��dpMatsandPiles��5;   $   ��.  $	��Top=�6P   5<      5=      5>      �� 5?      -   5        $��bOpacityLayerTop��5?   .   $	��Top=�6    J� IsHitTestVisible .  $��20��6_   $	��Top=�6k   $y�	Collapsed=�6�   ��5@      �� 5A      	 �� 
StartPoint$		 0,0&�6.   
 �� EndPoint$	
 0,1&�6?   .��5B      � � 5C   "    � Color$ White��6/    � Offset$ 0Q�6=   � 5D   "   $ Transparent��6/   $ 1Q�6C   5E      5F      5H      �� 5I      -    5        $��bOpacityLayerLeft��5I   /   $
��Left=�6    .  $��20��6a   $
��Left=�6l   $y�	Collapsed=�6�   ��5J      �� 5K      $		 0,0&�6.   $	
 1,0&�6?   .��5L      � � 5M   "   $ White��6/   $ 0Q�6=   � 5N   "   $ Transparent��6/   $ 1Q�6C   5O      5P      5R      �� 5S      -!   5        $��bOpacityLayerRight��5S   0   $��Right=�6    .  $��20��6c   $��Right=�6n   $y�	Collapsed=�6�   ��5T      �� 5U      $		 1,0&�6.   $	
 0,0&�6?   .��5V      � � 5W   "   $ White��6/   $ 0Q�6=   � 5X   "   $ Transparent��6/   $ 1Q�6C   5Y      5Z      5\      �� 5]      -"   5        $��bOpacityLayerBottom��5]   1   $��Bottom=�6    .  $��20��6e   $��Bottom=�6q   $y�	Collapsed=�6�   ��5^      �� 5_      $		 0,1&�6.   $	
 0,0&�6?   .��5`      � � 5a   "   $ White��6/   $ 0Q�6=   � 5b   "   $ Transparent��6/   $ 1Q�6C   5c      5d      5f      5g      5h      5i      5j      