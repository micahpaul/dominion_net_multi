   M S B A M L   `   `   `  ����U  QGongSolutions.Wpf.DragDrop, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullkLclr-namespace:GongSolutions.Wpf.DragDrop;assembly=GongSolutions.Wpf.DragDropGongSolutions.Wpf.DragDrop  5      L HDominion .NET WPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullG(clr-namespace:Dominion.NET_WPF.ViewModelDominion.NET_WPF.ViewModel 5      E'clr-namespace:Dominion.NET_WPF.ControlsDominion.NET_WPF.Controls 5      )   #Dominion.NET_WPF.Controls.ucChooser   5      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      [ddLclr-namespace:GongSolutions.Wpf.DragDrop;assembly=GongSolutions.Wpf.DragDrop     5      7vm(clr-namespace:Dominion.NET_WPF.ViewModel     5      8ctrl'clr-namespace:Dominion.NET_WPF.Controls     5        � 	Resources  5
       �� MergedDictionaries �� Source2 ,Dominion.NET_WPF.ViewModel.CardListViewModel   	ViewModel �� 
TargetType �� 	Resources �� Color�� 5   
    5      �� 5      $ ../Resources/Styles.xaml?�6%   %  &	        '��        5      6#   �� 5      " ��6    5      ���� 5      $ Transparent��6X   (��       6&   * SystemColors.HighlightBrushKey+)6X   �� 5      $ Transparent��6V   (��       6&   *SystemColors.ControlBrushKey+)6V   5      5      5      .��5      �� 
����+��$��3q�6&    �� CornerRadius$ 4��6:   $��2q�6K   ._�5   
   ]� .+�5      � -   5        $��	wpButtons��5      $��Bottom=�6)   .+�5      (  "Dominion.NET_WPF.Controls.ucButton  -   5          Name$	 bOK��5       (  "Dominion.NET_WPF.Controls.ucButton   Click$ 	bOK_Click��6a   $
��Left=�6-   $��4q�6H   $��50��6S   $
��Left=�6s   	   Text$
	 Done��6�   �� 5      -   5        $��cbAutoClick��5      
 d� 	IsChecked$

 TrueY�6.   $��4q�6?   $
��Left=�6J   $��Center=�6e   $
��Left=�6�   .��6�   
Auto-click6�   �� 5      -   5        $��
bButtonDiv��5      
����+��$��2q�6D   $��4q�6X     5      -   5        $
 bAll��5       $ 
bAll_Click��6c   $��Right=�6.   $��4q�6J   $��50��6U   $��Right=�6v   $		 All��6�     5      -   5        $ bNone��5       $ bNone_Click��6d   $��Right=�6/   $��4q�6K   $��50��6V   $��Right=�6x   $
	 None��6�   5       �� 5!      -   5        $��tbText��5!      $
��Auto��6$   $	��Top=�61   $
��Bold"�6F    �� TextWrapping$
 Wrap=�6X    �� 
IsReadOnly . |�.  ��.  6�   � 5"      -   5        $��
wrapPanel1��5"   &   $
��Auto��6   $
��Auto��68   $��2q�6E   $	��Top=�6P   �� 5#      $
��Auto=�6   .��5$      ��  �� Orientation$ Vertical=�6   .+�5%      �� -	   5        $��bReorder��5%      
���   �$��1q�6A   $y�Visible=�6U   ._�5&      ]� .+�5'      ]� ��.  $
��Left=�6>   $��1q�6T   .+�5(   "   �� $	��Top��6,   $	��Top=�67   $��Right=�6L   �� 5)   "   $��Bottom��6,   $��Bottom=�6:   $��Right=�6R   5*      �� 5+      -
   5        $��	lbReorder��5+   b   $��1q�6&   $��1q�61   $��35��6E   $��75��6T   )   #GongSolutions.Wpf.DragDrop.DragDrop   IsDragSource .    IsDropTarget . $
��Left=�5-   &   $��Stretch=�6<   $��Stretch=�6Z   ��5+   s   �� *Cards+   DragHandler 5,   F   ��    DropHandler 6j   ��  � DataContext 5.   "     5/   &   ��51   "   �� 52   &   .R�53   *   /  )Dominion.NET_WPF.Controls.ucICardCheckBox     IsCheckable .     ICard 6?   �� *ICard+6W   54   '      DragAdornerTemplate 56   "   �� 57   &   .R�58   *   +  %Dominion.NET_WPF.Controls.ucICardIcon     ICard 6;   �� *ICard+59   '   5;      �� 5<      -   5        $��	bCardPool��5<   %   
����+��$��2q�6O   $��4q�6c   $y�Visible=�6n   $��Stretch=�6�   $
��Left=�6�   �� 5=      -   5        $��
lbCardPool��5=   T   $��1q�6&   $��0q�61   $��35��6E   $y�Visible=�6f    .  . $��Disabled=�5?   &   $��Stretch=�6\   $��Stretch=�6z   ��5=   {   �� *Cards+ 5>   F   ��  6j   ��  5@   "     5A   &   ��5C   "   �� 5D   &   .R�5E   *      .   6?   �� *ICard+6W   5F   '   ��5H   "   �� 5I   &   .R�5J   *   �  K� IsItemsHost .  � Orientation$ 
Horizontal=�6G   5K   '    5M   "   �� 5N   &   .R�5O   *      6;   �� *ICard+5P   '   5R      5S      5T      �� 5U      -   5        $��bHidden��5U      
���   �$��1q�6@   $y�Visible=�6T   ._�5V      ]� .+�5W      �� $	��Top��6(   $
��Left=�63   $��Right=�6I   $	��Top=�6e   �� 5X      $��Bottom��6(   $��Right=�66   $��Right=�6M   $��Bottom=�6i   �� 5Y      -   5        $��lbHidden��5Y   b   $��0q�6&   $��1q�61   $��75��6E   $��35��6T    .  . $
��Left=�5[   "   $��Stretch=�68   $��Stretch=�6V   $��Disabled=�6r   ��5Y   r   �� *Cards+ 5Z   B   ��  6f   �� ��5\   "   �� 5]   &   .R�5^   *   �  . 64   5_   '    5a   "     5b   &   ��5d   "   �� 5e   &   .R�5f   *      .     Orientation$ Vertical=�6�   $��0q�6�    6?   �� *ICard+   CardVisibility 6W   �� *
Visibility+6�   5g   '   ��5i   "   �� 5j   &   " ��6,   .�5k   *   ��  �� Property � �� �� Value$ 0q�6D   5l   '    5n   "   �� 5o   &   .R�5p   *      6;   �� *ICard+   CardVisibility 6S   �� *
Visibility+5q   '   5s      5t      5u      5v      5w      5x      5y      5z      