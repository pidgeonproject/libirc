libirc
=========

Open source irc library for c#, based on pidgeon irc system

This library is used in projects that require very low level control of the irc
protocol. It supports the latest IRC features and allows you to alter hook or
change every single bit or procedure within the IRC protocol.

It doesn't just allow you to hook to various events, but you can even hook to
very RAW network traffic that is transmitted between the server and client, and
EVEN ALTER IT, which gives you very low level control over everything that is
happening. This library is designed for very advanced IRC users as well as
newbies, because it can also operate in automatic mode where most of stuff is
processed by library itself.

You can use this irc library to design own bot or client. This library is not
suitable in order to create IRC server.

The basic philosophy is this library should allow you to extremely easily
create a simple irc client just with few lines of code, just as it allows you
to create very robust and huge solution.


How to use it
==============

There is a folder examples which contains simple irc bot and client, check their
source code, there is also documentation at http://pidgeonclient.org/doc/libirc

Also read files in folder Docs


Where to report bugs and request new stuff
===========================================

http://pidgeonclient.org/bugzilla


Where to get help
===================

irc://irc.tm-irc.org/#pidgeon

or SSL

ircs://irc.tm-irc.org:6697/#pidgeon
