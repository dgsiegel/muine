/*
 * Copyright (C) 2004 Jorn Baayen <jorn@nl.linux.org>
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

public class GnomeProxy
{
	private bool use;

	private WebProxy proxy;
	
	public GnomeProxy ()
	{
		try {
			use = (bool) Muine.GConfClient.Get ("/system/http_proxy/use_http_proxy");
		} catch {
			use = false;
		}

		if (use) {
			string host;
			try {
				host = (string) Muine.GConfClient.Get ("/system/http_proxy/host");
			} catch {
				host = "";
			}

			int port;
			try {
				port = (int) Muine.GConfClient.Get ("/system/http_proxy/port");
			} catch {
				port = 8080;
			}
		
			proxy = new WebProxy (host, port);

			bool use_auth;
			try {
				use_auth = (bool) Muine.GConfClient.Get ("/system/http_proxy/use_authentication");
			} catch {
				use_auth = false;
			}

			if (use_auth) {
				string user;
				try {
					user = (string) Muine.GConfClient.Get ("/system/http_proxy/authentication_user");
				} catch {
					user = "";
				}

				string passwd;
				try {
					passwd = (string) Muine.GConfClient.Get ("/system/http_proxy/authentication_password");
				} catch {
					passwd = "";
				}
				
				proxy.Credentials = new NetworkCredential (user, passwd);
			}
		}
	}

	public bool Use {
		get {
			return use;
		}
	}

	public WebProxy Proxy {
		get {
			return proxy;
		}
	}
}
