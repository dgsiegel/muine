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

public class InfoWindow
{
	[Glade.Widget]
	Window window;
	[Glade.Widget]
	TextView textview;
	private TextBuffer buffer;

	/* FIXME destructor, static?.. */
	/* FIXME cleanup tray menu */

	public InfoWindow (string title, Window parent)
	{
		Glade.XML gxml = new Glade.XML (null, "InfoWindow.glade", "window", null);
		gxml.Autoconnect (this);

		window.Title = title;
		window.TransientFor = parent;

		int width;
		try {
			width = (int) Muine.GConfClient.Get ("/apps/muine/information_window/width");
		} catch {
			width = 350;
		}

		int height;
		try {
			height = (int) Muine.GConfClient.Get ("/apps/muine/information_window/height");
		} catch {
			height = 300;
		}

		window.SetDefaultSize (width, height);

		window.SizeAllocated += new SizeAllocatedHandler (HandleSizeAllocated);

		buffer = textview.Buffer;

		AddTags ();
	}

	private void AddTags ()
	{
		TextTag tag = new TextTag ("title");
		tag.Weight = Pango.Weight.Bold;
		tag.Scale = Pango.Scale.Large;
		buffer.TagTable.Add (tag);
	}

	private void HandleSizeAllocated (object o, SizeAllocatedArgs args)
	{
		int width, height;

		window.GetSize (out width, out height);

		Muine.GConfClient.Set ("/apps/muine/information_window/width", width);
		Muine.GConfClient.Set ("/apps/muine/information_window/height", height);
	}

	~InfoWindow ()
	{
		Console.WriteLine ("killing..");
	}

	public void Run ()
	{
		window.ShowAll ();
	}

	private void HandleCloseButtonClicked (object o, EventArgs args)
	{
		window.Destroy ();
	}

	private void InsertTextWithTag (string text, string tag)
	{
		TextIter iter = buffer.GetIterAtMark (buffer.InsertMark);

		int begin, end;
		
		begin = buffer.CharCount;
		buffer.Insert (iter, text);
		end = buffer.CharCount;
		
		if (tag != null) {
			TextIter begin_iter = buffer.GetIterAtOffset (begin);
			TextIter end_iter = buffer.GetIterAtOffset (end);

			buffer.ApplyTag (tag, begin_iter, end_iter);
		}
	}

	private void InsertTrack (Song song, int n)
	{
		InsertTextWithTag ("\n" + n + " " + song.Title, null);
	}

	public void Load (Album album) 
	{
		/* insert album cover */
		TextIter iter = buffer.GetIterAtOffset (0);
		buffer.InsertPixbuf (iter, album.CoverImage);
		
		/* insert album name */
		InsertTextWithTag (album.Name, "title");

		/* insert tracks */
		int n = 1;
		foreach (Song song in album.Songs) {
			InsertTrack (song, n);
			n++;
		}
	}
}
