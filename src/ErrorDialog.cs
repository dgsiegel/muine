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

using Gtk;
using GLib;

using Mono.Posix;

namespace Muine
{
	public class ErrorDialog
	{
		// Widgets
		[Glade.Widget] private Dialog window;
		[Glade.Widget] private Label  label;

		// Constructor
		public ErrorDialog (Window parent,
				    string primary_text,
				    string secondary_text)
		: this (primary_text, secondary_text)
		{
			window.TransientFor = parent;
		}

		public ErrorDialog (string primary_text, string secondary_text)
		{
			Glade.XML gxml = new Glade.XML (null, "ErrorDialog.glade", "window", null);
			gxml.Autoconnect (this);

			string full_text = String.Format ("<span size=\"large\" weight=\"bold\">{0}</span>\n\n{1}",
			                                  StringUtils.EscapeForPango (primary_text),
							  StringUtils.EscapeForPango (secondary_text));

			label.Markup = full_text;

			window.Run ();
			window.Destroy ();
		}		
	}
}
