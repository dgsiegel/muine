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
using System.Collections;
using System.Runtime.InteropServices;

using Gtk;

public class EllipsizingLabel : Label
{
	[DllImport ("libmuine")]
	private static extern IntPtr rb_ellipsizing_label_new (string text);
	
	public EllipsizingLabel (string text)
	{
		Raw = rb_ellipsizing_label_new (text);
	}

	~EllipsizingLabel ()
	{
		Dispose ();
	}

	[DllImport ("libmuine")]
	private static extern void rb_ellipsizing_label_set_text (IntPtr label,
								  string text);
	[DllImport ("libgtk-win32-2.0-0.dll")]
	private static extern string gtk_label_get_text (IntPtr label);

	public new string Text {
		set {
			rb_ellipsizing_label_set_text (Raw, value);
		}
		
		get {
			return gtk_label_get_text (Raw);
		}
	}
}