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

public class ErrorDialog
{
	[Glade.Widget]
	Window window;
	[Glade.Widget]
	Label label;

	public void Setup (string text)
	{
		Glade.XML gxml = new Glade.XML (null, "ErrorDialog.glade", "window", null);
		gxml.Autoconnect (this);

		MarkupUtils.LabelSetMarkup (label, 0, StringUtils.GetByteLength (text),
					    true, true, false);

		label.Text = text;

		((Dialog) window).Run ();

		window.Destroy ();
	}
	
	public ErrorDialog (string text)
	{
		Setup (text);
	}

	public ErrorDialog (string text, Window parent)
	{
		Setup (text);

		window.TransientFor = parent;
	}
}
