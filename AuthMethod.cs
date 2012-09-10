using System;

namespace S22.Pop3 {
	/// <summary>
	/// Defines supported means of authenticating with the POP3 server.
	/// </summary>
	public enum AuthMethod {
		/// <summary>
		/// Login using plaintext username/password authentication. This
		/// is the default supported by most servers.
		/// </summary>
		Login,
		/// <summary>
		/// Login using the CRAM-MD5 authentication mechanism.
		/// </summary>
		CRAMMD5,
		/// <summary>
		/// Login using the OAuth authentication mechanism over
		/// the Simple Authentication and Security Layer (Sasl).
		/// </summary>
		SaslOAuth
	}
}
