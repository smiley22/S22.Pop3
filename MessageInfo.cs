using System;

namespace S22.Pop3 {
	/// <summary>
	/// Describes status information of a mail message.
	/// </summary>
	[Serializable]
	public class MessageInfo {
		internal MessageInfo(uint messageNumber, UInt64 messageSize) {
			Number = messageNumber;
			Size = messageSize;
		}

		/// <summary>
		/// The message number of this mail message.
		/// </summary>
		public uint Number {
			get;
			private set;
		}

		/// <summary>
		/// The size of this mail message, in bytes.
		/// </summary>
		public UInt64 Size {
			get;
			private set;
		}
	}
}
