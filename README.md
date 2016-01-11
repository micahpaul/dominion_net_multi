# dominion_net_multi
A Multi-Player Dominion.NET Mod

Most of the code and attributes are from the project at http://dominion.technowall.net/.

My plan is to make it fully playable with 1-6 human players. AI players are a nice plus, but I won't lose any sleep over it if I break them.

_Known Issues_:

* When loading a saved game, only the first player's hand will show (rendering the game pretty much unplayable). I think this is because the _Player variable doesn't get set properly. On load, we may need to cycle through, setting up each human player as such in turn. Also: Be sure the game saves properly, with correct current turn, etc.

* When changing whose turn it is, the tab doesn't change automatically, which is a bit annoying but can be worked around.


_Resolved (I Think) Issues_:
* On Outpost (and maybe other cards; I don't know), you can play your second turn, but you can't buy anything because the buying section didn't get refigured properly.

* Humans + Robots = Super jacked up Game.
