   M S B A M L   `   `   `  ����L  HDominion .NET WPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null3clr-namespace:Dominion.NET_WPFDominion.NET_WPF  5   	   I)clr-namespace:Dominion.NET_WPF.ConvertersDominion.NET_WPF.Converters  5   	   "    Dominion.NET_WPF.wCardViewer   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   0localclr-namespace:Dominion.NET_WPF     5   	   :conv)clr-namespace:Dominion.NET_WPF.Converters     5   	     � Title$  Card Viewer��5   	    � WindowStartupLocation$ CenterOwner=�6    � SizeToContent$ WidthAndHeight=�6A    � 
ResizeMode$ NoResize=�6`    � 	Resources 5       �� MergedDictionaries �� Source@   :Dominion.NET_WPF.Converters.CardToBackgroundBrushConverter "  CardToBackgroundBrushConverter@   :Dominion.NET_WPF.Converters.CardToForegroundBrushConverter " CardToForegroundBrushConverter�� 5   
    5	      �� 5
      $ Resources/Styles.xaml?�6%   %:   &	        &	        5      62     5      62   5      .��5      ]� .+�5   
   o� $	��Top=�6    o� 
IsMainMenu . .@�5      m� $��_Window��6   .@�5      m� -   5        $��_Close��5      6+   5      5      ]� 5   
   $	��Top=�6   $	��0,2q�6)   .+�5      �� $��Set filter:��6   $
��Left=�6+   $	��4,0q�6A   $��Center=�6N    �� Padding$	 0,5q�6i   �� 5      -   5        $��cbSets��5      $��30��6P     NormalBrush#��[ 6\   ��5      �� 5      .R�5      �� 	 �� Orientation$	 
Horizontal=�6%   .+�5      �� $��imSource��6$   $��Right=�64   $	��2,1q�6P   $��20��6]   $��20��6i   ��6w   �� 
 �� Path$
 Image�6�   �� 5      $��setName��6(   ��67   �� $
 Source�5      5      5       5"      5#      ]� 5$   
   $	��Top=�6   $	��0,2q�6)   .+�5%      �� $��Select Card:��6   $
��Left=�6,   $	��4,0q�6B   $��Center=�6O   $	 0,5q�6j   �� 5&      -   5        $��cbCards��5&      #��[ 6R   ��5'      �� 5(       �� 
TargetType" ��6   .�5)      ��  �� Property � �� �� Value 67   ��  �� 	Converter# [  �� 5*       � �� 67   �� # [ �� 5+       � ��
 �   �68   �� 5,       � ��$ 0,1,0,1q�6<   5-      5/      50      ]� 51   
   .+�52      "   Dominion.NET_WPF.ToolTipCard  -   5          Name$ ttcCard��52       $y�Hidden=�61   53      54      55      