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
			Catalog.GetString ("Importing {0}...");
		
		// Widgets
		[Glade.Widget]
		Window window;
		[Glade.Widget]
		Label loading_label;
		[Glade.Widget]
		Container file_label_container;
		private EllipsizingLabel file_label;

		private bool canceled = false;

		private static string title_format;
		
		public ProgressWindow (Window parent)
		{
			Glade.XML gxml = new Glade.XML (null, "ProgressWindow.glade", "window", null);
			gxml.Autoconnect (this);

			window.TransientFor = parent;

			window.SetDefaultSize (300, -1);

			file_label = new EllipsizingLabel ("");
			file_label.Xalign = 0.0f;
			file_label.Visible = true;
			file_label_container.Add (file_label);

			MarkupUtils.LabelSetMarkup (loading_label, 0, StringUtils.GetByteLength (loading_label.Text),
						    false, true, false);

			title_format = string_title;
		}

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

		public void Done ()
		{
			window.Destroy ();
		}

		private void OnWindowResponse (object o, EventArgs a)
		{
			window.Visible = false;

			canceled = true;
		}

		private void OnWindowDeleteEvent (object o, EventArgs a)
		{
			window.Visible = false;

			DeleteEventArgs args = (DeleteEventArgs) a;

			args.RetVal = true;

			canceled = true;
		}
	}
}
