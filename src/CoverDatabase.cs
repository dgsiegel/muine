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
using System.IO;
using System.Runtime.InteropServices;

using Gnome;
using Gdk;

public class CoverDatabase 
{
	private IntPtr dbf;

	public Hashtable Covers;

	private delegate void DecodeFuncDelegate (string key, IntPtr data, IntPtr user_data);
	
	[DllImport ("libmuine")]
	private static extern IntPtr db_open (string filename, out string error);
	[DllImport ("libmuine")]
	private static extern void db_foreach (IntPtr dbf, DecodeFuncDelegate decode_func,
					       IntPtr user_data);
						   
	public CoverDatabase ()
	{
		DirectoryInfo dinfo = new DirectoryInfo (User.DirGet () + "/muine");
		if (!dinfo.Exists) {
			try {
				dinfo.Create ();
			} catch (Exception e) {
				throw e;
			}
		}
		
		string filename = dinfo.ToString () + "/covers.db";

		string error = null;

		dbf = db_open (filename, out error);

		if (dbf == IntPtr.Zero) {
			throw new Exception ("Failed to open database: " + error);
		}

		Covers = new Hashtable ();
	}

	public void Load ()
	{
		db_foreach (dbf, new DecodeFuncDelegate (DecodeFunc), IntPtr.Zero);
	}

	[DllImport ("libmuine")]
	private static extern IntPtr db_unpack_pixbuf (IntPtr p, out IntPtr pixbuf);
	
	private void DecodeFunc (string key, IntPtr data, IntPtr user_data)
	{
		IntPtr pix_handle;
		
		db_unpack_pixbuf (data, out pix_handle);

		Muine.CoverDB.Covers.Add (String.Copy (key), new Pixbuf (pix_handle));
	}

	private Pixbuf CoverPixbufFromFile (string filename)
	{
		Pixbuf cover, border;
		int target_size = 64; /* if this is changed, the glade file needs to be updated, too */

		/* read the cover image */
		cover = new Pixbuf (filename);

		/* scale the cover image if necessary */
		if (cover.Height > target_size || cover.Width > target_size) {
			int new_width, new_height;

			if (cover.Height > cover.Width) {
				new_width = (int) Math.Round ((double) target_size / (double) cover.Height * cover.Width);
				new_height = target_size;
			} else {
				new_height = (int) Math.Round ((double) target_size / (double) cover.Width * cover.Height);
				new_width = target_size;
			}

			cover = cover.ScaleSimple (new_width, new_height, InterpType.Bilinear);
		}

		/* create the background + border pixbuf */
		border = new Pixbuf (Colorspace.Rgb, true, 8, cover.Width + 2, cover.Height + 2);
		border.Fill (0x000000ff); /* TODO get from theme */
			
		/* put the cover image on the border area */
		cover.CopyArea (0, 0, cover.Width, cover.Height, border, 1, 1);

		/* done */
		return border;
	}

	private delegate IntPtr EncodeFuncDelegate (IntPtr handle, out int length);

	[DllImport ("libmuine")]
	private static extern void db_store (IntPtr dbf, string key, bool overwrite,
					     EncodeFuncDelegate encode_func,
					     IntPtr user_data);

	public Pixbuf AddCover (string filename)
	{
		Pixbuf pix = CoverPixbufFromFile (filename);

		if (pix == null)
			return null;

		Covers.Add (filename, pix);

		db_store (dbf, filename, false,
		          new EncodeFuncDelegate (EncodeFunc), pix.Handle);

		return pix;
	}

	[DllImport ("libmuine")]
	private static extern void db_delete (IntPtr dbf, string key);

	public void RemoveCover (string filename)
	{
		db_delete (dbf, filename);

		Covers.Remove (filename);
	}

	[DllImport ("libmuine")]
        private static extern IntPtr db_pack_start ();
	[DllImport ("libmuine")]
	private static extern void db_pack_pixbuf (IntPtr p, IntPtr pixbuf);
	[DllImport ("libmuine")]
        private static extern IntPtr db_pack_end (IntPtr p, out int length);

	private IntPtr EncodeFunc (IntPtr handle, out int length)
	{
		IntPtr p = db_pack_start ();
		db_pack_pixbuf (p, handle);
		return db_pack_end (p, out length);
	}

	public Pixbuf CoverFromFile (string filename)
	{
		if (filename.Length == 0)
			return null;

		Pixbuf ret = (Pixbuf) Covers [filename];

		if (ret == null)
			ret = AddCover (filename);
		
		return ret;
	}
}
