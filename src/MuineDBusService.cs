/*
 * Copyright (C) 2004 Sergio Rubio <sergio.rubio@hispalinux.es>
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

using DBus;

public sealed class MuineDBusService
{
	private Service service;
	private Connection conn;
	
	private MuineDBusService ()
	{
		Init ();
	}

	private static MuineDBusService instance;
	public static MuineDBusService Instance {
		get {
			if (instance == null)
				instance = new MuineDBusService ();
			return instance;
		}
	}

	private void Init ()
	{
		conn = Bus.GetSessionBus ();
		service = new Service (conn, "org.gnome.Muine");
	}

	public void RegisterObject (object obj, string path)
	{
		service.RegisterObject (obj, path);
	}
}
