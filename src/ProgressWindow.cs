/*
 * Copyright © 2004 Jorn Baayen <jorn@nl.linux.org>
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
using GtkSharp;
using GLib;

public class ProgressWindow
{
	[Glade.Widget]
	Window window;
	[Glade.Widget]
	Label loading_label;
	[Glade.Widget]
	Label file_label;

	bool canceled;
	
	public ProgressWindow (Window parent)
	{
		Glade.XML gxml = new Glade.XML (null, "ProgressWindow.glade", "window", null);
		gxml.Autoconnect (this);

		window.TransientFor = parent;
		window.Title = "Importing...";

		MarkupUtils.LabelSetMarkup (loading_label, 0, StringUtils.GetByteLength (loading_label.Text),
					    false, true, false);

		window.Visible = true;

		canceled = false;
	}

	public bool ReportFolder (string folder)
	{
		if (canceled)
			return false;
			
		window.Title = "Importing " + folder + "...";

		while (Global.EventsPending () == 1)
			Main.Iteration ();

		return true;
	}

	public bool ReportFile (string file)
	{
		if (canceled)
			return false;

		file_label.Text = file;

		while (Global.EventsPending () == 1)
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
