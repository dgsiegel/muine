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
using System.IO;

using Gtk;
using GtkSharp;
using GLib;

public class InfoWindow
{
	[Glade.Widget]
	Window window;
	[Glade.Widget]
	TextView textview;
	private HTML html;

	/* FIXME destructor, static?.. */
	/* FIXME cleanup tray menu */
	/* FIXME vis from tray */
	/* FIXME albumchanged signal? */
	/* FIXME consider async loading? */

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

		html = new HTML ();
		html.Editable = false;

	    	html.UrlRequested += new UrlRequestedHandler (HandleUrlRequested);

		((Container) gxml ["scrolledwindow"]).Add (html);
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

	private void InsertTrack (Song song, int n)
	{
	}

	private string DumpCoverImageToFile (Gdk.Pixbuf pixbuf)
	{
		if (pixbuf == null)
			return null;

		string path = Path.GetTempFileName ();

		try {
			pixbuf.Savev (path, "png", null, null);
		} catch {
			return null;
		}

		return path;
	}

	public void Load (Album album) 
	{
		HTMLStream stream = html.Begin ("text/html");

		/* in order to be able to inject the album cover into HTML, we
		   are gonna spit it out to disk first */
		string cover_image_fn = DumpCoverImageToFile (album.CoverImage);

		if (cover_image_fn != null) {
			stream.Write ("<img src=\"" + cover_image_fn + "\"/>");
		}
		
		/* insert album name */
		stream.Write ("<b>" + album.Name + "</b>");

		/* insert year of release */
		stream.Write ("<i>" + album.Year + "</i>");

		/* insert tracks */
		int n = 1;
		foreach (Song song in album.Songs) {
			InsertTrack (song, n);
			n++;
		}

		html.End (stream, HTMLStreamStatus.Ok);

		if (cover_image_fn != null) {
			/* clean up */
			FileInfo cover_image_finfo = new FileInfo (cover_image_fn);

			cover_image_finfo.Delete ();
		}
	}

	private void HandleUrlRequested (object obj, UrlRequestedArgs args)
	{
		/* we only read from local files anyway, so this'll do. */
		FileStream stream;
		BinaryReader reader;
		
		try {
			stream = new FileStream (args.Url, FileMode.Open);
			reader = new BinaryReader (stream);
		} catch {
			args.Handle.Close (HTMLStreamStatus.Error);

			return;
		}

		byte [] bytes = new byte [8192];
		while (reader.Read (bytes, 0 , 8192) > 0)
			args.Handle.Write (bytes, bytes.Length);

		args.Handle.Close (HTMLStreamStatus.Ok);

		reader.Close ();
		stream.Close ();
	}
}
