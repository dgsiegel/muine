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

using Gtk;
using GLib;

using Mono.Posix;

namespace Muine
{
	public class ProgressWindow
	{
		// Strings
		private static readonly string string_title =
			Catalog.GetString ("Importing \"{0}\"");
		private static readonly string string_loading =
			Catalog.GetString ("Loading:");

		private static string title_format;
				
		// Widgets
		[Glade.Widget] private Window    window;
		[Glade.Widget] private Label     loading_label;
		[Glade.Widget] private Container file_label_container;

		private EllipsizingLabel file_label;

		// Variables
		private bool canceled = false;

		// Constructor
		public ProgressWindow (Window parent)
		{
			Glade.XML gxml = new Glade.XML (null, "ProgressWindow.glade", "window", null);
			gxml.Autoconnect (this);

			window.TransientFor = parent;

			window.SetDefaultSize (300, -1);

			file_label = new EllipsizingLabel ();
			file_label.Xalign = 0.0f;
			file_label.Visible = true;
			file_label_container.Add (file_label);

			loading_label.Markup = String.Format ("<b>{0}</b>",
							      StringUtils.EscapeForPango (string_loading));

			title_format = string_title;
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: Report
		public bool Report (string folder, string file)
		{
			if (canceled)
				return true;

			window.Title = String.Format (title_format, folder);

			if (file != null)
				file_label.Text = file;

			window.Visible = true;

			return false;
		}

		// Methods :: Public :: Done
		public void Done ()
		{
			window.Destroy ();
		}

		// Handlers
		// Handlers :: OnWindowResponse
		private void OnWindowResponse (object o, EventArgs a)
		{
			window.Visible = false;
			canceled = true;
		}

		// Handlers :: OnWindowDeleteEvent
		private void OnWindowDeleteEvent (object o, DeleteEventArgs args)
		{
			window.Visible = false;
			args.RetVal = true;
			canceled = true;
		}
	}
}
