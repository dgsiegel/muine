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

namespace Muine
{
	public class GnomeProxy
	{
		// GConf
		private const string GConfProxyPath = "/system/http_proxy";

		private const string GConfKeyUse = GConfProxyPath + "/use_http_proxy";
		private const bool   GConfDefaultUse = false;

		private const string GConfKeyHost = GConfProxyPath + "/host";
		private const string GConfDefaultHost = "";

		private const string GConfKeyPort = GConfProxyPath + "/port";
		private const int    GConfDefaultPort = 8080;

		private const string GConfKeyUseAuth = GConfProxyPath + "/use_authentication";
		private const bool   GConfDefaultUseAuth = false;

		private const string GConfKeyUser = GConfProxyPath + "/authentication_user";
		private const string GConfDefaultUser = "";

		private const string GConfKeyPass = GConfProxyPath + "/authentication_password";
		private const string GConfDefaultPass = "";

		// Objects
		private WebProxy proxy;

		// Variables		
		private bool use;

		// Constructor		
		public GnomeProxy ()
		{
			Setup ();

			Config.AddNotify (GConfProxyPath, 
				new GConf.NotifyEventHandler (OnConfigChanged));
		}

		// Properties
		// Properties :: Use (get;)
		public bool Use {
			get { return use; }
		}

		// Properties :: Proxy (get;)
		public WebProxy Proxy {
			get { return proxy; }
		}

		// Methods
		// Methods :: Private
		// Methods :: Private :: Setup
		private void Setup ()
		{
			proxy = null;

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

			string user   = (string) Config.Get (GConfKeyUser, GConfDefaultUser);
			string passwd = (string) Config.Get (GConfKeyPass, GConfDefaultPass);
					
			try {
				proxy.Credentials = new NetworkCredential (user, passwd);

			} catch {
				use_auth = false;
			}
		}

		// Handlers
		// Handlers :: OnConfigChanged
		private void OnConfigChanged (object o, GConf.NotifyEventArgs args)
		{
			Setup ();
		}
	}
}
