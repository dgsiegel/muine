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

using Gtk;
using GLib;

public class ProgressWindow
{
	[Glade.Widget]
	Window window;
	[Glade.Widget]
	Label loading_label;
	[Glade.Widget]
	Container file_label_container;
	private EllipsizingLabel file_label;

	bool canceled;
	
	public ProgressWindow (Window parent, string folder)
	{
		Glade.XML gxml = new Glade.XML (null, "ProgressWindow.glade", "window", null);
		gxml.Autoconnect (this);

		window.TransientFor = parent;
		window.Title = String.Format (Muine.Catalog.GetString ("Importing {0}..."), folder);

		window.SetDefaultSize (300, -1);

		file_label = new EllipsizingLabel ("");
		file_label.Xalign = 0.0f;
		file_label.Visible = true;
		file_label_container.Add (file_label);

		MarkupUtils.LabelSetMarkup (loading_label, 0, StringUtils.GetByteLength (loading_label.Text),
					    false, true, false);

		canceled = false;

		file_label.Text = "...";

		while (MainContext.Pending ())
			Main.Iteration ();
	}

	public bool ReportFile (string file)
	{
		if (canceled)
			return false;

		window.Visible = true;

		file_label.Text = file;

		while (MainContext.Pending ())
			Main.Iteration ();

		return true;
	}

	public void Done ()
	{
		window.Destroy ();
	}

	private void HandleWindowResponse (object o, EventArgs a)
	{
		window.Visible = false;

		canceled = true;
	}

	private void HandleWindowDeleteEvent (object o, EventArgs a)
	{
		window.Visible = false;

		DeleteEventArgs args = (DeleteEventArgs) a;

		args.RetVal = true;

		canceled = true;
	}
}
