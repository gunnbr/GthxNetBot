# GthxNetBot
This is the gthx IRC bot reimplemented in C# instead of the original python.
The main reasons for this are :
* Gthx was originally created to properly handled unicode and python is a real pain to work with unicode. (Or at least Python2 certainly is!)
* C# is my main language for daily coding, so I'm much more familiar with it and can code things significantly faster
* I have free credit on Azure that I'd like to use and it seems easier to publish .NET apps to Azure than Python
* Gthx replaced the bot it used to track, so all the bot tracking code no longer needs to exist

# TODO
* Verify that actions are detected and update the last seen
* Verify that direct messages work
* Implement lurkers module
* Fix automatic build and deploy rules on github
