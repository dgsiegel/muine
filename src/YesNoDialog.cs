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

// TODO: Make HIG-compliant
// TODO: Make the Dialog ourselves

using System;

using Gtk;
using GLib;

namespace Muine
{
	public class YesNoDialog
	{
		// Widgets
		[Glade.Widget] private Window window;
		[Glade.Widget] private Label label;

		// Constructor
		public YesNoDialog (string text, Window parent)
		{
			Glade.XML gxml = new Glade.XML (null, "YesNoDialog.glade", "window", null);
			gxml.Autoconnect (this);

			label.Text = text;

			window.TransientFor = parent;
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: GetAnswer
		public bool GetAnswer ()
		{
			bool ret = (((Dialog) window).Run () == (int) ResponseType.Yes);
			window.Destroy ();
			return ret;
		}
	}
}
