/*
 * Copyright © 2004 Jorn Baayen <jorn@nl.linux.org>
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
using System.Runtime.InteropServices;

public class MessageConnection
{
	private IntPtr conn;
	
	[DllImport ("libmuine")]
	private static extern IntPtr bacon_message_connection_new (string name);

	public MessageConnection ()
	{
		conn = bacon_message_connection_new ("Muine");
	}

	[DllImport ("libmuine")]
	private static extern bool bacon_message_connection_get_is_server (IntPtr conn);

	public bool IsServer {
		get {
			return bacon_message_connection_get_is_server (conn);
		}
	}

	public delegate void MessageReceivedHandler (string message,
						     IntPtr user_data);

	[DllImport ("libmuine")]
	private static extern void bacon_message_connection_set_callback (IntPtr conn,
									  MessageReceivedHandler callback,
									  IntPtr user_data);

	public void SetCallback (MessageReceivedHandler handler)
	{
		bacon_message_connection_set_callback (conn, handler, IntPtr.Zero);
	}

	[DllImport ("libmuine")]
	private static extern void bacon_message_connection_send (IntPtr conn, string command);

	public void Send (string command)
	{
		bacon_message_connection_send (conn, command);
	}

	[DllImport ("libmuine")]
	private static extern void bacon_message_connection_free (IntPtr conn);

	public void Close ()
	{
		bacon_message_connection_free (conn);
	}
}
