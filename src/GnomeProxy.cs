/*
 * Copyright (C) 2004 Jorn Baayen <jbaayen@gnome.org>
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
	private const string GConfKeyUse = "/system/http_proxy/use_http_proxy";
	private const bool GConfDefaultUse = false;

	private const string GConfKeyHost = "/system/http_proxy/host";
	private const string GConfDefaultHost = "";

	private const string GConfKeyPort = "/system/http_proxy/port";
	private const int GConfDefaultPort = 8080;

	private const string GConfKeyUseAuth = "/system/http_proxy/use_authentication";
	private const bool GConfDefaultUseAuth = false;

	private const string GConfKeyUser = "/system/http_proxy/authentication_user";
	private const string GConfDefaultUser = "";

	private const string GConfKeyPass = "/system/http_proxy/authentication_password";
	private const string GConfDefaultPass = "";

	private bool use;

	private WebProxy proxy;
	
	public GnomeProxy ()
	{
		use = (bool) Config.Get (GConfKeyUse, GConfDefaultUse);

		if (!use)
			return;

		// Host / Proxy
		string host = (string) Config.Get (GConfKeyHost, GConfDefaultHost);

		int port = (int) Config.Get (GConfKeyPort, GConfDefaultPort);
		
		try {
			proxy = new WebProxy (host, port);
		} catch {
			use = false;
			return;
		}

		// Authentication
		bool use_auth = (bool) Config.Get (GConfKeyUseAuth, GConfDefaultUseAuth);

		if (!use_auth)
			return;

		string user = (string) Config.Get (GConfKeyUser, GConfDefaultUser);

		string passwd = (string) Config.Get (GConfKeyPass, GConfDefaultPass);
				
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
