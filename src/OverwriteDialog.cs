/*
 * Copyright (C) 2004, 2005 Jorn Baayen <jbaayen@gnome.org>
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
using System.IO;

using Gtk;
using GLib;

using Mono.Posix;

namespace Muine
{
	public class OverwriteDialog
	{
		// Strings
		private static readonly string string_primary_text =
			Catalog.GetString ("Overwrite \"{0}\"?");

		private static readonly string string_secondary_text =
			Catalog.GetString ("A file with this name already exists. " +
			"If you choose to overwrite this file, the contents will be lost.");

		// Widgets
		[Glade.Widget] private Dialog window;
		[Glade.Widget] private Label  label;

		// Constructor
		public OverwriteDialog (Window parent, string fn)
		{
			Glade.XML gxml = new Glade.XML (null, "OverwriteDialog.glade", "window", null);
			gxml.Autoconnect (this);

			string primary_text = String.Format (string_primary_text,
				FileUtils.MakeHumanReadable (fn));

			label.Markup = String.Format ("<span size=\"large\" weight=\"bold\">{0}</span>\n\n{1}",
				StringUtils.EscapeForPango (primary_text),
				StringUtils.EscapeForPango (string_secondary_text));

			window.TransientFor = parent;
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: GetAnswer
		public bool GetAnswer ()
		{
			int response = window.Run ();
			window.Destroy ();

			return (response == (int) ResponseType.Yes);
		}
	}
}
