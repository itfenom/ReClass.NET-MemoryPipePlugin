﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace MemoryPipePlugin
{
	class MessageClient
	{
		private readonly PipeStream pipe;
		public PipeStream Pipe => pipe;

		private readonly Dictionary<int, Func<IMessage>> registeredMessages = new Dictionary<int, Func<IMessage>>();
		public IDictionary<int, Func<IMessage>> RegisteredMessages => registeredMessages;

		public MessageClient(PipeStream pipe)
		{
			Contract.Requires(pipe != null);

			this.pipe = pipe;
		}

		public IMessage Receive()
		{
			using (var ms = new MemoryStream())
			{
				var buffer = new byte[256];
				do
				{
					var length = pipe.Read(buffer, 0, buffer.Length);
					ms.Write(buffer, 0, length);
				}
				while (!pipe.IsMessageComplete);

				ms.Position = 0;

				using (var br = new BinaryReader(ms, Encoding.Unicode, true))
				{
					var type = br.ReadInt32();

					Func<IMessage> createFn;
					if (registeredMessages.TryGetValue(type, out createFn))
					{
						var message = createFn();
						message.ReadFrom(br);
						return message;
					}
				}
			}

			return null;
		}

		public void Send(IMessage message)
		{
			Contract.Requires(message != null);

			using (var ms = new MemoryStream())
			{
				using (var bw = new BinaryWriter(ms, Encoding.Unicode, true))
				{
					bw.Write(message.Type);
					message.WriteTo(bw);
				}

				var buffer = ms.ToArray();
				pipe.Write(buffer, 0, buffer.Length);
			}
		}
	}
}