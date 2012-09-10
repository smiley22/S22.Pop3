### Examples

* [Connecting to an POP3 server using SSL](#1)
* [Download new mail messages](#2)
* [Download mail headers only instead of the entire mail message](#3)
	
<a name="1"></a>**Connecting to an POP3 server using SSL**

	using System;
	using S22.Pop3;

	namespace Test {
		class Program {
			static void Main(string[] args)
			{
				using (Pop3Client Client = new Pop3Client("pop.gmail.com", 995,
				 "username", "password", Authmethod.Login, true))
				{
					Console.WriteLine("We are connected!");
				}
			}
		}
	}

<a name="2"></a>**Download new mail messages**

	using System;
	using S22.Pop3;

	namespace Test {
		class Program {
			static void Main(string[] args)
			{
				using (Pop3Client Client = new Pop3Client("pop.gmail.com", 995,
				 "username", "password", Authmethod.Login, true))
				{
					MailMessages[] messages = Client.GetMessages();
				}
			}
		}
	}

<a name="3"></a>**Download mail headers only instead of the entire mail message**

	using System;
	using S22.Pop3;

	namespace Test {
		class Program {
			static void Main(string[] args)
			{
				using (Pop3Client Client = new Pop3Client("pop.gmail.com", 995,
				 "username", "password", Authmethod.Login, true))
				{
					MailMessage[] messages = Client.GetMessages(null, FetchOptions.HeadersOnly);
				}
			}
		}
	}
