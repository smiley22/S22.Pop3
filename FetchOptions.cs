using System;

namespace S22.Pop3 {
	/// <summary>
	/// Fetch options that can be used with the GetMessage and GetMessages methods
	/// to selectively retrieve parts of a mail message while skipping others.
	/// </summary>
	public enum FetchOptions {
		/// <summary>
		/// Fetches the entire mail message with all of its content.
		/// </summary>
		Normal,
		/// <summary>
		/// Only the mail message headers will be retrieved, while the actual content will
		/// not be downloaded. If this option is specified, only the header fields of the
		/// returned MailMessage object will be initialized.
		/// </summary>
		HeadersOnly
	}
}
