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

public class ColoredCellRendererPixbuf : Gtk.CellRenderer 
{
	~ColoredCellRendererPixbuf ()
	{
		Dispose ();
	}

	protected ColoredCellRendererPixbuf (GLib.GType gtype) : base (gtype) {}
	public ColoredCellRendererPixbuf (IntPtr raw) : base (raw) {}

	[DllImport ("libmuine")]
	static extern IntPtr rb_cell_renderer_pixbuf_new ();

	public ColoredCellRendererPixbuf ()
	{
		Raw = rb_cell_renderer_pixbuf_new ();
	}

	public Gdk.Pixbuf Pixbuf {
		get {
			GLib.Value val = new GLib.Value (Handle, "pixbuf");
			GetProperty ("pixbuf", val);
			System.IntPtr raw_ret = (System.IntPtr) (GLib.UnwrappedObject) val;
			bool ref_owned = false;
			Gdk.Pixbuf ret = (Gdk.Pixbuf) GLib.Object.GetObject (raw_ret, ref_owned);
			return ret;
		}
		set {
			SetProperty ("pixbuf", new GLib.Value (value));
		}
	}

	[DllImport ("libmuine")]
	static extern uint rb_cell_renderer_pixbuf_get_type ();

	public static new uint GType { 
		get {
			uint raw_ret = rb_cell_renderer_pixbuf_get_type ();
			uint ret = raw_ret;
			return ret;
		}
	}
}
