   M S B A M L   `   `   `  ����L  HDominion .NET WPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null3clr-namespace:Dominion.NET_WPFDominion.NET_WPF  5   <   E'clr-namespace:Dominion.NET_WPF.ControlsDominion.NET_WPF.Controls  5      /clr-namespace:ScrollableAreaScrollableArea  5          Dominion.NET_WPF.wMain   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      0localclr-namespace:Dominion.NET_WPF     6<   8ctrl'clr-namespace:Dominion.NET_WPF.Controls     5      +saclr-namespace:ScrollableArea     5      -   5        0   *Dominion.NET_WPF.Controls.CardStackControl    CardStackControlClick$6  0CardCollectionControl_CardCollectionControlClick��5       � Title$ Dominion .NET��5      $	��750��6   $	��850��6(    � WindowStyle$ ThreeDBorderWindow=�64    � 	Resources 5         imUpdate �� 	UriSource  imHuman  
imComputer  imTable 
 imSave 
 imLoad�� 5	   
   %�  &	        &	 I     &	 �     &	 �     &	     &	 _    �� 5
      $5 /pack://application:,,,/Resources/bullet_get.png?�6+   �� 5      $/ )pack://application:,,,/Resources/user.png?�6*   �� 5      $3 -pack://application:,,,/Resources/computer.png?�6-   �� 5      $5 /pack://application:,,,/Resources/card_array.png?�6*   �� 5      $/ )pack://application:,,,/Resources/disk.png?�6)   �� 5      $1 +pack://application:,,,/Resources/folder.png?�6)   5       J� CommandBindings 5      )  #System.Windows.Input.CommandBinding  5   
   -   5           Command$% local:CustomCommands.CardViewer��5      6D     5   
   -   5        $# local:CustomCommands.SaveGame��5      6B     5   
   -   5        $# local:CustomCommands.LoadGame��5      6B    J� InputBindings 5      �� 5   
   
 �� Key$ F1��6   	 �� Command$%	 local:CustomCommands.CardViewer��6   �� 5   
   $ S��6   
 �� 	Modifiers$
 Controlf�6   $#	 local:CustomCommands.SaveGame��61   �� 5   
   $ L��6   $
 Controlf�6   $#	 local:CustomCommands.LoadGame��61   .��5      ]� .+�5   
   o� $	��Top=�6    o� 
IsMainMenu . .@�5      m� $��_Game��6   .@�5      m� -   5        $��	miNewGame��5      $��	_New Game��6,   6?   m� 5       -   5        $��	miEndGame��5       $��_End current game��6,   {�.  6b   m� 5!      -   5        $��miReplay��5!      $ ��_Replay same Kingdom cards��6+   {�.  6i   �� 5"      m� 5#      -   5        $��
miSettings��5#      $��	_Settings��6-   6@   m� 5$      -	   5        $��
miLoadGame��5$      $��_Load game...��6-    m� InputGestureText$ Ctrl+L��6\    m� Icon 5%      �� 5&      #��[ 6    $��16��6M   $��16��6Y   5(      �� 5)      m� 5*      -
   5        $��E_xit��5*      6*   5+      m� 5,      -   5        $��miCurrentGame��5,      $��_Current Game��6,   .@�5-      m� -   5        $��
miSaveGame��5-      $��_Save current game...��6-   {�.  $ Ctrl+S��6}    5.      �� 5/      #��[ 6    $��16��6M   $��16��6Y   51      m� 52      -   5        $��miViewGameLog��52      $��_View Game Log��60   {�. 6n   m� 53      -   5        $��miAutoPlayTreasures��53      $#��Automatically play _Treasures��66   {�.  m� IsCheckable .  m� 	IsChecked 6�   ��  �� Path$ AutoPlayTreasures�6�   m� 54      -   5        $��miShowKingdomNames��54      $%��Show _Kingdom Supply pile names��65   {�.  .  6�   �� $ DisplaySupplyPileNames�6�   m� 55      -   5        $��miShowBasicNames��55      $#��Show _Basic Supply pile names��63   {�.  .  6~   �� $! DisplayBasicSupplyPileNames�6�   56      m� 57      ��58      �� 59       �� Orientation$ 
Horizontal=�6!   .+�5:      �� -   5        $��iUpdate��5:       #��[  6/   $y�	Collapsed=�6^   $��16��6u   $��16��6�   �� 5;      $��_Help��6    $��0q�60   5<      .@�5>      m� $��_Card Viewer��6    m� Command$% local:CustomCommands.CardViewer��61   $ F1��6[   �� 5?      m� 5@      -   5        $&�� Visit _Official Dominion site...��5@      6E   m� 5A      -   5        $��Visit _Developer site...��5A      6=   m� 5B      -   5        $��miCheckForUpdates��5B      $��Check for _Updates��65   6Q   m� 5C      -   5        $��
miDownload��5C      $��_Update to latest version��6-   $y�	Collapsed=�6P    5D      �� 5E      #��[  6    $��16��6O   $��16��6[   5G      5H      5I      �� 5J   
   $��Bottom=�6   .@�5K      �� .��5L      �� -   5        $��cpStatus��5L   #   $�� ��63   5M      �� 5N      $��Stretch=�6   .��5O      � -   5        $��pbStatus��5O      $	��100��6.   $y�	Collapsed=�6:   5P      5Q      � 5R   
   -   5        $��gMainDisplay��5R      $	��Top=�6#    � RowDefinitions 5S      �� 5T      -   5         �Name$ rdGrid0��5T       ��6/   ��  �� ElementName$ bStackPanelSupplyPiles��$ ActualHeight��� 5U      $��* �6    .+�5W      ]� -   5        $��dpTopInformation��5W      $��0��60   $��0��6@   $��0,0,0,4q�6M   .+�5Y      � -   5        $#��gStackPanelSupplyPilesOutside��5Y      $
��Left=�6<   $��Stretch=�6R   .+�5Z      �� -   5        $��bStackPanelSupplyPiles��5Z      
������ $��0q�6U   $	��Top=�6i   ._�5[      �� -   5        $��stackPanelSupplyPiles��5[   K   $
��Left=�6%   $��2q�6@   $	��231��6h   $	��211��6w   $	��Top=�6�   $ 
Horizontal=�6�   $
��Left=�6�   5\      5]      ]� 5^      -   5        $��
dpGameInfo��5^      $��Stretch=�6.   $y�	Collapsed=�6�   ��6J   �� $# gStackPanelSupplyPilesOutside��$ ActualHeight�.+�5_      )   #Dominion.NET_WPF.Controls.ucChooser  -   5          Name$ 
uccChooser��5_   %   )   #Dominion.NET_WPF.Controls.ucChooser   ChooserOKClick$ uccChooser_ChooserOKClick��69   $y�	Collapsed=�6d   $��Bottom=�6{   5`      5a      �� 5b      -    5         �� ResizeDirection$
 Rows=�5b      $��Stretch=�62   $��Bottom=�6P   $��0q�6k   $��4��6v   
����=H�6�   ]� 5c      $��0��6   $��1��6(   .+�5d      �� $
��Auto=�6   $��Hidden=�6B   .��5e      �� -!   5        $��dpStuff��5e   !   $��2q�60   $��Stretch=�6;   $	��Top=�6Y   $y�Visible=�6q   $
��Left=�6�   $	��135��6�   .+�5f      ]� .+�5g      �� $��Actions:��6$   $��Center=�67   $
��Bold"�6Y   $��16(�6k   $
��Left=�6y   �� 5h      -"   5        $��lActions��5h   $   $��Center=�64   $��Right=�6V   $��16(�6y   .��5i   "   �� -#   5        $��	tbActions��5i   ,   5j      5k      ]� 5l      .+�5m      �� $��Buys:��6$   $��Center=�64   $
��Bold"�6V   $��16(�6h   $
��Left=�6v   �� 5n      -$   5        $��lBuys��5n   $   $��Center=�61   $��Right=�6S   $��16(�6v   .��5o   "   �� -%   5        $��tbBuys��5o   ,   5p      5q      ]� 5r      .+�5s      �� $��Funds:��6$   $��Center=�65   $
��Bold"�6W   $��16(�6i   $
��Left=�6w   �� 5t      -&   5        $��	lCurrency��5t   $   $��Center=�65   $��16(�6W   $��Right=�6e   .��5u   "   �� -'   5        $��
tbCurrency��5u   ,   5v      5w      (   "Dominion.NET_WPF.Controls.ucButton  5x      -(   5          Name$ bPlayTreasures��5x   (   (   "Dominion.NET_WPF.Controls.ucButton   Click$ bPlayTreasures_Click��6U   $y�Visible=�6@   $��Stretch=�6r   $��Stretch=�6�   $��56��6�   $	��Top=�6�   $	��4,0q�6�      Text$ Play basic _Treasures��6�   $��56��6  $��28��6    5y      -)   5        $ bPlayCoinTokens��5y   (   $ bPlayCoinTokens_Click��6V   $y�Visible=�6A   $��Stretch=�6t   $��Stretch=�6�   $��56��6�   $	��Top=�6�   $	��4,0q�6�   $ Play _Coin tokens��6�   $��56��6  $��28��6    5z      -*   5        $ 	bBuyPhase��5z   (   $ bBuyPhase_Click��6�    � ToolTip$V PImmediately go to the Buy phase (no more Action or Treasure cards can be played)��6;   $y�Visible=�6�   $��Stretch=�6�   $��Stretch=�6�   $��56��6  $	��Top=�6  $	��4,0q�6*  $ Go to _Buy phase��67  $��28��6O  $��56��6^    5{      -+   5        $ bUndo��5{   (   $ bUndo_Click��6L   $y�Visible=�67   $��Stretch=�6`   $��Stretch=�6~   $��56��6�   $	��Top=�6�   $	��4,0q�6�   $ _Undo��6�   $��56��6�   $��28��6�     5|      -,   5        $ 	bTurnDone��5|   (   $ bTurnDone_Click��6P   $y�Visible=�6;   $��Stretch=�6h   $��Stretch=�6�   $��28��6�   $	��Top=�6�   $	��4,0q�6�   $ 	_End turn��6�   5}      5~      �� 5      --   5        $��tcAreas��5      $ �� IsSynchronizedWithCurrentItem$
 TrueY�6,   .@�5�      �� -.   5        $��tiGame��5�      ��5�      ]� 5�      .+�5�   "   �� -/   5        $��iTable��5�   (   #��[ 66   $��16��6d   $��16��6p   $
��Left=�6{   $��0,0,5,0q�6�   �� 5�   "   $��Table��6,   6;   5�      .��5�      ��  �� CornerRadius$ 2��6!   $��2q�62   
����� �._�5�      ]� .+�5�   "   ]� $	��Top=�6,   .+�5�   &   � $��6��6+   $��6��65   
�������$
��Left=�6W   .+�5�   *   �� $��4��61   $��4��6<   
����� �$��1q�6`   
����  �$��1,0,0,0q�6�   $
��Left=�6�   5�   '   � 5�   &   -0   5        $��6��5�   +   
�������.+�5�   *   �� -1   5        $��bGameHorizontal��5�   1   $��4��6H   $��55��6S   
����� �$	��0,1q�6x   
����  �$��0,0,0,0q�6�   $
��Left=�6�   5�   '   5�   #   ]� 5�   "   $
��Left=�6,   .+�5�   &   � -2   5        $��6��5�   +   
�������.+�5�   *   �� -3   5        $��bGameVertical��5�   1   $��4��6F   $��44��6P   
����� �$	��1,0q�6v   
����  �$��0,0,0,0q�6�   $	��top=�6�   5�   '   5�   #   � 5�   "   .+�5�   &   �� -4   5        $��svGame��5�   3   $��Disabled=�6A   $��Hidden=�6k   $��Stretch=�6�   $��1q�6�   
���   �$��Trebuchet MS)�6�   $   ScrollableArea.KineticBehavior   HandleKineticScrolling .     Friction$
  0.90Q�6  .��5�   *   ]� ��.  
�������.+�5�   .   � -5   5        $��wpTrash��5�   8   ! � Orientation$! 
Horizontal=�6G   $	��Top=�6`   .+�5�   2   ,   &Dominion.NET_WPF.CardCollectionControl  -6   5        "  Name$" 	cardTrash��5�   N   $
��Auto��6a   $��0,0,0,0q�6o   $
��Left=�6�   5�   /   �� 5�   .   -7   5        $��bTrashMatsDivider��5�   5   # K� ZIndex$# 1��6N   $��1q�6_   
���   �$	��Top=�6�   ]� 5�   .   -8   5        $��dpMatsandPiles��5�   8   ��.  $	��Top=�6d   5�   /   �� 5�   .   -9   5        $��bStuffDivider��5�   5   $# 1��6J   $��1q�6[   
���   �$��Bottom=�6�   $y�	Collapsed=�6�   ]� 5�   .   -:   5        $��dpGameStuff��5�   8   $��Bottom=�6K   ��.  
�������5�   /   5�   +   5�   '   �� 5�   &   -;   5        $��bOpacityLayerTop��5�   B   $	��Top=�6-   $ J� IsHitTestVisible$ .  $��20��6s   $	��Top=�6   $y�	Collapsed=�6�   ��5�   *   �� 5�   .   % �� 
StartPoint$	% 0,0&�6B   & �� EndPoint$	& 0,1&�6S   .��5�   2   � � 5�   6   ' � Color$' White��6C   ( � Offset$( 0Q�6Q   � 5�   6   $' Transparent��6C   $( 1Q�6W   5�   3   5�   /   5�   '   �� 5�   &   -<   5        $��bOpacityLayerLeft��5�   C   $
��Left=�6-   $ .  $��20��6u   $
��Left=�6�   $y�	Collapsed=�6�   ��5�   *   �� 5�   .   $	% 0,0&�6B   $	& 1,0&�6S   .��5�   2   � � 5�   6   $' White��6C   $( 0Q�6Q   � 5�   6   $' Transparent��6C   $( 1Q�6W   5�   3   5�   /   5�   '   �� 5�   &   -=   5        $��bOpacityLayerRight��5�   D   $��Right=�6-   $ .  $��20��6w   $��Right=�6�   $y�	Collapsed=�6�   ��5�   *   �� 5�   .   $	% 1,0&�6B   $	& 0,0&�6S   .��5�   2   � � 5�   6   $' White��6C   $( 0Q�6Q   � 5�   6   $' Transparent��6C   $( 1Q�6W   5�   3   5�   /   5�   '   �� 5�   &   ->   5        $��bOpacityLayerBottom��5�   E   $��Bottom=�6-   $ .  $��20��6y   $��Bottom=�6�   $y�	Collapsed=�6�   ��5�   *   �� 5�   .   $	% 0,1&�6B   $	& 0,0&�6S   .��5�   2   � � 5�   6   $' White��6C   $( 0Q�6Q   � 5�   6   $' Transparent��6C   $( 1Q�6W   5�   3   5�   /   5�   '   5�   #   5�      5�      5�      �� 5�      -?   5        $��	tiGameLog��5�   0   $��Game Log��6   .��5�      )	   #Dominion.NET_WPF.Controls.ucGameLog	  -@   5        ) 	 Name$) glMain��5�   )   "* 	  VerticalScrollBarVisibility$* Visible=�69   $+ 	  HorizontalScrollBarVisibility$
+ Auto=�6_   $��Stretch=�6�   5�      5�      5�      5�      5�      5�      