/*
 * Copyright (C) 2004 Ross Girshick <ross.girshick@gmail.com>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

using System;
using System.Net;
using System.Net.Sockets;
using Mono.Posix;
using System.Text;
using System.IO;

public class MessageConnection
{
	public enum StatusCode { 
		OK, 
		Retry, 
		SocketCreateFailure, 
		SocketDeleteFailure 
	};
	
	public enum ConnectionType { 
		Server, 
		Client 
	};

	private Socket socket;
	
	private string socket_filename;
	public string SocketFilename {
		get {
			return socket_filename;
		}
	}

	private ConnectionType role;
	public ConnectionType Role {
		get {
			return role;
		}
	}

	private StatusCode status;
	public StatusCode Status {
		get {
			return status;
		}
	}

	public delegate void MessageReceivedDelegate (string[] Message);

	private MessageReceivedDelegate message_received_handler;
	public MessageReceivedDelegate MessageReceivedHandler {
		set {
			message_received_handler = value;
		}
	}


	public MessageConnection ()
	{
		string socket_path;
		
		try {
			socket_path = System.IO.Path.GetTempPath ();
		} catch {
			socket_path = "/tmp";
		}

		status = StatusCode.OK;
		
		socket_filename = System.IO.Path.Combine (socket_path, "muine-" + Environment.GetEnvironmentVariable ("USER") + ".socket");
				
		socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
		EndPoint socket_end_point = new UnixEndPoint (socket_filename);

		if (File.Exists (socket_filename)) {
			role = ConnectionType.Client;
			try {
				socket.Connect (socket_end_point);
			} catch {
				try {
					File.Delete (socket_filename);
					status = StatusCode.Retry;
				} catch	{
					status = StatusCode.SocketDeleteFailure;
				}
			}
		} else {
			role = ConnectionType.Server;

			try {
				socket.Bind (socket_end_point);
				socket.Listen (5);
				socket.BeginAccept (new AsyncCallback (ListenCallback), socket);
			}
			catch
			{
				status = StatusCode.SocketCreateFailure;
			}
		}
	}
	
	~MessageConnection ()
	{
		Close ();
	}

	public int Send (byte[] message)
	{
		return socket.Send (message);
	}

	public void Close ()
	{
		if (role == ConnectionType.Server) {
			File.Delete (socket_filename);
		}
		socket.Close ();
	}

	private GLib.IdleHandler message_cb;
	private void ListenCallback (IAsyncResult state)
	{
		Socket Client = ((Socket) state.AsyncState).EndAccept (state);
		((Socket) state.AsyncState).BeginAccept (new AsyncCallback (ListenCallback), state.AsyncState);
		
		byte [] buf = new byte [1024];
		string [] message;
		int bytes = Client.Receive (buf);

		if (bytes > 0) {
			string [] raw_message = Encoding.UTF8.GetString (buf).Split ('\n');
			message = new string [raw_message.Length];
			for (int i = 0; i < raw_message.Length; ++i)
			{
				foreach (char c in raw_message [i]) {
					if (c == 0x0000)
						break;
					message [i] += c;
				}
			}
		} else {
			message = null;
		}

		message_cb = new GLib.IdleHandler (new IdleWork (message_received_handler, message).Run);
		GLib.Idle.Add (message_cb);
	}

	internal class IdleWork
	{
		private string [] message;
		private MessageReceivedDelegate callback;

		public IdleWork (MessageReceivedDelegate cb, string [] msg)
		{
			message = msg;
			callback = cb;
		}

		public bool Run ()
		{
			callback (message);
			return false;
		}
	}
}
