   M S B A M L   `   `   `  ����L  HDominion .NET WPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullE'clr-namespace:Dominion.NET_WPF.ControlsDominion.NET_WPF.Controls  5   	   "    Dominion.NET_WPF.wMessageBox   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   8ctrl'clr-namespace:Dominion.NET_WPF.Controls     5   	     � Title$  wMessageBox��5   	   $	��150��6   $	��350��6*    � 
ResizeMode$ NoResize=�66    � ShowInTaskbar .   � Topmost .  � WindowStartupLocation$ CenterOwner=�6q    � WindowStyle$ ThreeDBorderWindow=�6�   .��5      ]� .+�5   
   ]� $��Bottom=�6   ��.  .+�5      (   "Dominion.NET_WPF.Controls.ucButton  -   5          Name$ bCancel��5   G   (   "Dominion.NET_WPF.Controls.ucButton   Click$ bCancel_Click��6�      Text$ _Cancel��6   $��Right=�6+   $��Bottom=�6X   $��75��6s   	   IsCancel	 . $��Right=�6�   $��3q�6�   
   TextPadding$
 2q�6�     5	      -   5        $	 bNo��5	   C   $ 	bNo_Click��6v   $	 _No��6   $��Right=�6'   $��Bottom=�6P   $��75��6k   $��Right=�6�   $��3q�6�   $
 2q�6�     5
      -   5        $	 bOk��5
   C   $ 	bOk_Click��6�   $	 _OK��6   $��Right=�6'   $��Bottom=�6P   $��75��6k      	IsDefault . $��Right=�6�   $��3q�6�   $
 2q�6�   5      �� 5   
   -   5        $��iIcon��5      $
��Left=�6   $��75��63   $
��None=�6>   5      �� 5   
   -   5        $��tbBody��5      $��Center=�6"   $��WrapWithOverflow=�6=   $��10q�6]   5      5      5      