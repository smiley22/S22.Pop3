using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace S22.Pop3 {
	/// <summary>
	/// Allows applications to communicate with a mail server by using the
	/// Post Office Protocol version 3 (POP3).
	/// </summary>
	public class Pop3Client : IDisposable {
		private Stream stream;
		private TcpClient client;
		private readonly object readLock = new object();
		private readonly object writeLock = new object();
		private readonly object sequenceLock = new object();
		private string[] capabilities;


		/// <summary>
		/// Indicates whether the client is authenticated with the server
		/// </summary>
		public bool Authed {
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the Pop3Client class and connects to the specified port
		/// on the specified host, optionally using the Secure Socket Layer (SSL) security protocol.
		/// </summary>
		/// <param name="hostname">The DNS name of the server to which you intend to connect.</param>
		/// <param name="port">The port number of the server to which you intend to connect.</param>
		/// <param name="ssl">Set to true to use the Secure Socket Layer (SSL) security protocol.</param>
		/// <param name="validate">Delegate used for verifying the remote Secure Sockets
		/// Layer (SSL) certificate which is used for authentication. Set this to null if not needed</param>
		/// <exception cref="ArgumentOutOfRangeException">The port parameter is not between MinPort
		/// and MaxPort.</exception>
		/// <exception cref="ArgumentNullException">The hostname parameter is null.</exception>
		/// <exception cref="SocketException">An error occurred while accessing the socket used for
		/// establishing the connection to the POP3 server. Use the ErrorCode property to obtain the
		/// specific error code</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">An authentication
		/// error occured while trying to establish a secure connection.</exception>
		/// <exception cref="BadServerResponseException">Thrown if an unexpected response is received
		/// from the server upon connecting.</exception>
		/// <include file='Examples.xml' path='S22/Pop3/Pop3Client[@name="ctor-1"]/*'/>
		public Pop3Client(string hostname, int port = 110, bool ssl = false,
			RemoteCertificateValidationCallback validate = null) {
			Connect(hostname, port, ssl, validate);
		}

		/// <summary>
		/// Initializes a new instance of the Pop3Client class and connects to the specified port on
		/// the specified host, optionally using the Secure Socket Layer (SSL) security protocol and
		/// attempts to authenticate with the server using the specified authentication method and
		/// credentials.
		/// </summary>
		/// <param name="hostname">The DNS name of the server to which you intend to connect.</param>
		/// <param name="port">The port number of the server to which you intend to connect.</param>
		/// <param name="username">The username with which to login in to the POP3 server.</param>
		/// <param name="password">The password with which to log in to the POP3 server.</param>
		/// <param name="method">The requested method of authentication. Can be one of the values
		/// of the AuthMethod enumeration.</param>
		/// <param name="ssl">Set to true to use the Secure Socket Layer (SSL) security protocol.</param>
		/// <param name="validate">Delegate used for verifying the remote Secure Sockets Layer
		/// (SSL) certificate which is used for authentication. Set this to null if not needed</param>
		/// <exception cref="ArgumentOutOfRangeException">The port parameter is not between MinPort
		/// and MaxPort.</exception>
		/// <exception cref="ArgumentNullException">The hostname parameter is null.</exception>
		/// <exception cref="SocketException">An error occurred while accessing the socket used for
		/// establishing the connection to the POP3 server. Use the ErrorCode property to obtain the
		/// specific error code</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">An authentication
		/// error occured while trying to establish a secure connection.</exception>
		/// <exception cref="BadServerResponseException">Thrown if an unexpected response is received
		/// from the server upon connecting.</exception> 
		/// <exception cref="InvalidCredentialsException">Thrown if authentication using the
		/// supplied credentials failed.</exception>
		/// <include file='Examples.xml' path='S22/Pop3/Pop3Client[@name="ctor-2"]/*'/>
		public Pop3Client(string hostname, int port, string username, string password, AuthMethod method =
			AuthMethod.Login, bool ssl = false, RemoteCertificateValidationCallback validate = null) {
			Connect(hostname, port, ssl, validate);
			Login(username, password, method);
		}

		/// <summary>
		/// Connects to the specified port on the specified host, optionally using the Secure Socket Layer
		/// (SSL) security protocol.
		/// </summary>
		/// <param name="hostname">The DNS name of the server to which you intend to connect.</param>
		/// <param name="port">The port number of the server to which you intend to connect.</param>
		/// <param name="ssl">Set to true to use the Secure Socket Layer (SSL) security protocol.</param>
		/// <param name="validate">Delegate used for verifying the remote Secure Sockets
		/// Layer (SSL) certificate which is used for authentication. Set this to null if not needed</param>
		/// <exception cref="ArgumentOutOfRangeException">The port parameter is not between MinPort
		/// and MaxPort.</exception>
		/// <exception cref="ArgumentNullException">The hostname parameter is null.</exception>
		/// <exception cref="SocketException">An error occurred while accessing the socket used for
		/// establishing the connection to the POP3 server. Use the ErrorCode property to obtain the
		/// specific error code.</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">An authentication
		/// error occured while trying to establish a secure connection.</exception>
		/// <exception cref="BadServerResponseException">Thrown if an unexpected response is received
		/// from the server upon connecting.</exception>
		private void Connect(string hostname, int port, bool ssl, RemoteCertificateValidationCallback validate) {
			client = new TcpClient(hostname, port);
			stream = client.GetStream();
			if (ssl) {
				SslStream sslStream = new SslStream(stream, false, validate ??
					((sender, cert, chain, err) => true));
				sslStream.AuthenticateAsClient(hostname);
				stream = sslStream;
			}
			/* Server issues +OK greeting upon connect */
			string greeting = GetResponse();
			if (!IsResponseOK(greeting))
				throw new BadServerResponseException(greeting);
		}

		/// <summary>
		/// Determines whether the received response is a valid POP3 OK response.
		/// </summary>
		/// <param name="response">A response string received from the server</param>
		/// <returns>True if the response is a valid POP3 OK response, otherwise false
		/// is returned.</returns>
		private bool IsResponseOK(string response) {
			return response.StartsWith("+OK");
		}

		/// <summary>
		/// Sends a command string to the server. This method blocks until the command has
		/// been transmitted.
		/// </summary>
		/// <param name="command">Command string to be sent to the server. The command string is
		/// suffixed by CRLF (as is required by the POP3 protocol) prior to sending.</param>
		private void SendCommand(string command) {
			byte[] bytes = Encoding.ASCII.GetBytes(command + "\r\n");
			lock (writeLock) {
				stream.Write(bytes, 0, bytes.Length);
			}
		}

		/// <summary>
		/// Sends a command string to the server and subsequently waits for a response, which is
		/// then returned to the caller. This method blocks until the server response has been
		/// received.
		/// </summary>
		/// <param name="command">Command string to be sent to the server. The command string is
		/// suffixed by CRLF (as is required by the POP3 protocol) prior to sending.</param>
		/// <returns>The response received by the server.</returns>
		private string SendCommandGetResponse(string command) {
			lock (readLock) {
				lock (writeLock) {
					SendCommand(command);
				}
				return GetResponse();
			}
		}

		/// <summary>
		/// Waits for a response from the server. This method blocks
		/// until a response has been received.
		/// </summary>
		/// <returns>A response string from the server</returns>
		private string GetResponse() {
			const int Newline = 10, CarriageReturn = 13;
			using (var mem = new MemoryStream()) {
				lock (readLock) {
					while (true) {
						byte b = (byte)stream.ReadByte();
						if (b == CarriageReturn)
							continue;
						if (b == Newline) {
							return Encoding.ASCII.GetString(mem.ToArray());
						} else
							mem.WriteByte(b);
					}
				}
			}
		}

		/// <summary>
		/// Attempts to establish an authenticated session with the server using the specified
		/// credentials.
		/// </summary>
		/// <param name="username">The username with which to login in to the POP3 server.</param>
		/// <param name="password">The password with which to log in to the POP3 server.</param>
		/// <param name="method">The requested method of authentication. Can be one of the values
		/// of the AuthMethod enumeration.</param>
		/// <exception cref="InvalidCredentialsException">Thrown if authentication using the
		/// supplied credentials failed.</exception>
		/// <include file='Examples.xml' path='S22/Pop3/Pop3Client[@name="Login"]/*'/>
		public void Login(string username, string password, AuthMethod method) {
			string response = null;
			switch (method) {
				case AuthMethod.Login:
					lock (sequenceLock) {
						response = SendCommandGetResponse("USER " + username);
						if (!IsResponseOK(response))
							throw new BadServerResponseException(response);
						response = SendCommandGetResponse("PASS " + password);
						if (!IsResponseOK(response))
							throw new InvalidCredentialsException(response);
					}
					break;
				case AuthMethod.CRAMMD5:
					response = SendCommandGetResponse("AUTHENTICATE CRAM-MD5");
					/* retrieve server key */
					string key = Encoding.Default.GetString(
						Convert.FromBase64String(response.Replace("+ ", "")));
					/* compute the hash */
/*					using (var kMd5 = new HMACMD5(Encoding.ASCII.GetBytes(password))) {
						byte[] hash1 = kMd5.ComputeHash(Encoding.ASCII.GetBytes(key));
						key = BitConverter.ToString(hash1).ToLower().Replace("-", "");
						string command = Convert.ToBase64String(
							Encoding.ASCII.GetBytes(username + " " + key));
						response = SendCommandGetResponse(command);
					}
 */
					break;
				case AuthMethod.SaslOAuth:
//					response = SendCommandGetResponse(tag + "AUTHENTICATE XOAUTH " + password);
					break;
			}
			Authed = true;
		}

		/// <summary>
		/// Logs an authenticated client out of the server. After the logout sequence has
		/// been completed, the server closes the connection with the client.
		/// </summary>
		/// <exception cref="BadServerResponseException">Thrown if an unexpected response is
		/// received from the server during the logout sequence</exception>
		/// <remarks>Calling Logout in a non-authenticated state has no effect</remarks>
		public void Logout() {
			if (!Authed)
				return;
			lock (sequenceLock) {
				string response = SendCommandGetResponse("QUIT");
				if(!IsResponseOK(response))
					throw new BadServerResponseException(response);
				Authed = false;
			}
		}

		/// <summary>
		/// Returns a listing of capabilities that the POP3 server supports. All strings
		/// in the returned array are guaranteed to be upper-case.
		/// </summary>
		/// <exception cref="NotSupportedException">Thrown if the server does not support retrieving
		/// a list of its capabilities.</exception>
		/// <returns>A listing of supported capabilities as an array of strings</returns>
		/// <remarks>This is one of the few methods which can be called in a non-authenticated
		/// state. The command for retrieving a list of capabilities is an optional extension to
		/// the POP3 protocol, so not every server may support it.</remarks>
		/// <include file='Examples.xml' path='S22/Pop3/Pop3Client[@name="ctor-1"]/*'/>
		public string[] Capabilities() {
			if (capabilities != null)
				return capabilities;
			List<string> list = new List<string>();
			lock (sequenceLock) {
				string response = SendCommandGetResponse("CAPA");
				/* This is an optional extension, so the server may not support it */
				if (!IsResponseOK(response))
					throw new NotSupportedException("The server does not support this feature");
				while ((response = GetResponse()) != ".")
					list.Add(response.ToUpper());
				capabilities = list.ToArray();
				return capabilities;
			}
		}

		/// <summary>
		/// Returns whether the specified capability is supported by the server.
		/// </summary>
		/// <param name="capability">The capability to probe for (for example "IDLE")</param>
		/// <exception cref="BadServerResponseException">Thrown if an unexpected response is received
		/// from the server during the request. The message property of the exception contains
		/// the error message returned by the server.</exception>
		/// <exception cref="NotSupportedException">Thrown if the server does not implement
		/// the underlying infrastructure the Supports method relies upon.</exception>
		/// <returns>Returns true if the specified capability is supported by the server, 
		/// otherwise false is returned.</returns>
		public bool Supports(string capability) {
			return (capabilities ?? Capabilities()).Contains(capability.ToUpper());
		}

		/// <summary>
		/// Retrieves status information (list of messages with message numbers as well
		/// as size for each message)</summary>
		/// <returns>An array of MessageInfo objects containing status information for the
		/// mailbox.</returns>
		/// <exception cref="NotAuthenticatedException">Thrown if the method was called
		/// in a non-authenticated state, i.e. before logging into the server with
		/// valid credentials.</exception>
		/// <exception cref="BadServerResponseException">Thrown if the operation could
		/// not be completed. The message property of the exception contains the error message
		/// returned by the server.</exception>
		/// <include file='Examples.xml' path='S22/Pop3/Pop3Client[@name="GetStatus"]/*'/>
		public MessageInfo[] GetStatus() {
			if (!Authed)
				throw new NotAuthenticatedException();
			List<MessageInfo> list = new List<MessageInfo>();
			lock (sequenceLock) {
				string response = SendCommandGetResponse("LIST");
				if (!IsResponseOK(response))
					throw new BadServerResponseException(response);
				while ((response = GetResponse()) != ".") {
					Match m = Regex.Match(response, @"(\d+)\s(\d+)");
					if (!m.Success)
						continue;
					uint number = Convert.ToUInt32(m.Groups[1].Value);
					UInt64 size = Convert.ToUInt64(m.Groups[2].Value);
					list.Add(new MessageInfo(number, size));
				}
			}
			return list.ToArray();
		}

		/// <summary>
		/// Retrieves a list of message numbers of all mail messages in the mailbox.
		/// </summary>
		/// <returns>An array of message numbers representing the mail messages in the
		/// mailbox on the server.</returns>
		/// <exception cref="NotAuthenticatedException">Thrown if the method was called
		/// in a non-authenticated state, i.e. before logging into the server with
		/// valid credentials.</exception>
		/// <exception cref="BadServerResponseException">Thrown if the operation could
		/// not be completed. The message property of the exception contains the error message
		/// returned by the server.</exception>
		public uint[] GetMessageNumbers() {
			if (!Authed)
				throw new NotAuthenticatedException();
			List<uint> list = new List<uint>();
			lock (sequenceLock) {
				string response = SendCommandGetResponse("LIST");
				if (!IsResponseOK(response))
					throw new BadServerResponseException(response);
				while ((response = GetResponse()) != ".") {
					Match m = Regex.Match(response, @"(\d+)\s(\d+)");
					if (!m.Success)
						continue;
					uint number = Convert.ToUInt32(m.Groups[1].Value);
					list.Add(number);
				}
			}
			return list.ToArray();
		}

		/// <summary>
		/// Retrieves a mail message from the POP3 server.
		/// </summary>
		/// <param name="number">The message number of the mail message to retrieve</param>
		/// <param name="options">A value from the FetchOptions enumeration which allows
		/// for fetching selective parts of a mail message.</param>
		/// <param name="delete">Set this to true to delete the message on the server
		/// after it has been retrieved.</param>
		/// <exception cref="NotAuthenticatedException">Thrown if the method was called
		/// in a non-authenticated state, i.e. before logging into the server with
		/// valid credentials.</exception>
		/// <exception cref="BadServerResponseException">Thrown if the mail message could
		/// not be retrieved. The message property of the exception contains the error message
		/// returned by the server.</exception>
		/// <returns>An initialized instance of the MailMessage class representing the
		/// fetched mail message</returns>
		/// <include file='Examples.xml' path='S22/Pop3/Pop3Client[@name="GetMessage"]/*'/>
		public MailMessage GetMessage(uint number, FetchOptions options = FetchOptions.Normal,
			bool delete = false) {
			if (!Authed)
				throw new NotAuthenticatedException();
			string command = options == FetchOptions.HeadersOnly ? ("TOP " + number + " 0") :
				("RETR " + number);
			StringBuilder builder = new StringBuilder();
			lock (sequenceLock) {
				string response = SendCommandGetResponse(command);
				if (!IsResponseOK(response))
					throw new BadServerResponseException(response);
				while ((response = GetResponse()) != ".")
					builder.AppendLine(response);
				if (delete)
					DeleteMessage(number);
			}
			return options == FetchOptions.HeadersOnly ?
				MessageBuilder.FromHeader(builder.ToString()) :
				MessageBuilder.FromMIME822(builder.ToString());
		}

		/// <summary>
		/// Retrieves a set of mail messages. If no parameters are specified, all
		/// mail messages in the mailbox will be retrieved.
		/// </summary>
		/// <param name="numbers">An array of message numbers of the mail messages to
		/// retrieve. If this parameter is null, all mail messages will be retrieved.
		/// </param>
		/// <param name="options">A value from the FetchOptions enumeration which allows
		/// for fetching selective parts of a mail message.</param>
		/// <param name="delete">Set this to true to delete the messages on the server
		/// after they have been retrieved.</param>
		/// <exception cref="NotAuthenticatedException">Thrown if the method was called
		/// in a non-authenticated state, i.e. before logging into the server with
		/// valid credentials.</exception>
		/// <exception cref="BadServerResponseException">Thrown if the mail messages could
		/// not be fetched. The message property of the exception contains the error message
		/// returned by the server.</exception>
		/// <returns>An array of initialized instances of the MailMessage class representing
		/// the fetched mail messages</returns>
		/// <include file='Examples.xml' path='S22/Pop3/Pop3Client[@name="GetMessages"]/*'/>
		public MailMessage[] GetMessages(uint[] numbers = null, FetchOptions options =
			FetchOptions.Normal, bool delete = false) {
			List<MailMessage> list = new List<MailMessage>();
			if (numbers == null)
				numbers = GetMessageNumbers();
			foreach (uint n in numbers)
				list.Add(GetMessage(n, options, delete));
			return list.ToArray();
		}

		/// <summary>
		/// Deletes the mail message with the specified message number.
		/// </summary>
		/// <param name="number">The message number of the mail message that is to be
		/// deleted.</param>
		/// <exception cref="NotAuthenticatedException">Thrown if the method was called
		/// in a non-authenticated state, i.e. before logging into the server with
		/// valid credentials.</exception>
		/// <exception cref="BadServerResponseException">Thrown if the mail message could
		/// not be deleted. The message property of the exception contains the error
		/// message returned by the server.</exception>
		public void DeleteMessage(uint number) {
			if (!Authed)
				throw new NotAuthenticatedException();
			lock (sequenceLock) {
				string response = SendCommandGetResponse("DELE " + number);
				if (!IsResponseOK(response))
					throw new BadServerResponseException(response);
			}
		}

		/// <summary>
		/// Releases all resources used by this Pop3Client object.
		/// </summary>
		public void Dispose() {
			stream.Close();
			client.Close();
			stream = null;
			client = null;
		}
	}
}
