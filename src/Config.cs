/*
 * Copyright (C) 2005 Jorn Baayen <jbaayen@gnome.org>
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

namespace Muine
{
	public sealed class Config 
	{
		// Variables
		private static GConf.Client gconf_client;
		
		// Methods
		// Methods :: Public
		// Methods :: Public :: Init
		public static void Init ()
		{
			gconf_client = new GConf.Client ();
		}

		// Methods :: Public :: Get		
		public static object Get (string key)
		{
		       return gconf_client.Get (key);
		}
		
		public static object Get (string key, object default_val)
	        {
	                object val;

	                try {
	                        val = Get (key);
	                } catch {
	                        val = default_val;
	                }

	                return val;
	        }

		// Methods :: Public :: Set
	        public static void Set (string key, object val)
	        {
	        	gconf_client.Set (key, val);        	
	        }

		// Methods :: Public :: AddNotify
		public static void AddNotify (string key, GConf.NotifyEventHandler notify)
		{
			gconf_client.AddNotify (key, notify);
		}
	}
}
