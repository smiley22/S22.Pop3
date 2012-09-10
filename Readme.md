### Introduction

This repository contains an easy-to-use and well-documented .NET library component for communicating
with and receiving electronic mail from a Post Office Protocol (POP3) server.


### Motivation

There's already a plethora of POP3 libraries available for .NET, so it'a not like there is a need
for another one. I primarily created this for the fun of it and because writing a POP3
library really is a breeze, when you already have a [solid implementation of
IMAP with a MIME parser](https://github.com/smiley22/S22.Imap).


### Usage & Examples

To use the library add the S22.Pop3.dll assembly to your project references in Visual Studio. Here's
a simple example that initializes a new instance of the Pop3Client class and connects to Gmail's
POP3 server:

	using System;
	using S22.Pop3;

	namespace Test {
		class Program {
			static void Main(string[] args) {
				/* connect on port 995 using SSL */
				using (Pop3Client Client = new Pop3Client("pop.gmail.com", 995, true))
				{
					Console.WriteLine("We are connected!");
				}
			}
		}
	}

[Here](https://github.com/smiley22/S22.Pop3/blob/master/Examples.md) are a couple of examples of how to use
the library. Please also see the [documentation](http://smiley22.github.com/S22.Pop3/Documentation/) for
further details on using the classes and methods exposed by the S22.Pop3 namespace.

### Features

+ Supports POP over SSL
+ API designed to be very easy to use
+ Allows selectively fetching the headers of mail messages
+ Inherently thread-safe
+ Well documented with lots of example code
+ Free to use in commercial and personal projects ([MIT license](https://github.com/smiley22/S22.Pop3/blob/master/License.md))

### Credits

This library is copyright © 2012 Torben Könke.


### License

This library is released under the [MIT license](https://github.com/smiley22/S22.Pop3/blob/master/License.md).


### Bug reports

Please send your bug reports and questions to [smileytwentytwo@gmail.com](mailto:smileytwentytwo@gmail.com) or create a new
issue on the GitHub project homepage.
