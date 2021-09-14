Ilya Golan 313689796
Gilad Shmueli 314673187
Division of labour was as follows:
	* server side: Ilya
	* client side: Gilad
	* combining server and client sides and fixing bugs: Gilad and Ilya
IMPORTANT:
	* make sure you move the db folder files to C:\fourinrow
	* if you want to recreate them, use GameDB_create solution instead.
	  Make sure you have a folder named fourinrow in C:\ (there should be a folder called C:\fourinrow)
features:
	* initial window has "sign in" and "register" buttons.

	  the "username" and "password" fields apply to both buttons.

	* there is a small delay in game between a player making a move on his board
	  and his opponent registering it on his, please be patient.

instructions:
	* to login you need to be registered. Note: the name and password are case-sensitive.

	* the lobby shows the player details about themselves,
	  a list of waiting players and games online.
	
	* to invite a player you need to pick them from the list and click invite,
	  then wait for their response. If they accepted, a game window will appear automatically.

	* if being invited, a messagebox will pop up and ask you to accept or decline the invitation.
	  if you accept, a game window will appear automatically.

	* in the game window you need to pick a column by clicking it. 
	  If it's possible and it's your turn, the move will be made.
	
	