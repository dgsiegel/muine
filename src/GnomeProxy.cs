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
		use = (bool) Muine.GetGConfValue ("/system/http_proxy/use_http_proxy", false);

		if (!use)
			return;

		// Host / Proxy
		string host = (string) Muine.GetGConfValue ("/system/http_proxy/host", "");

		int port = (int) Muine.GetGConfValue ("/system/http_proxy/port", 8080);
		
		try {
			proxy = new WebProxy (host, port);
		} catch {
			use = false;
			return;
		}

		// Authentication
		bool use_auth = (bool) Muine.GetGConfValue ("/system/http_proxy/use_authentication", false);

		if (!use_auth)
			return;

		string user = (string) Muine.GetGConfValue ("/system/http_proxy/authentication_user", "");

		string passwd = (string) Muine.GetGConfValue ("/system/http_proxy/authentication_password", "");
				
		try {
			proxy.Credentials = new NetworkCredential (user, passwd);
		} catch {
			use_auth = false;
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
