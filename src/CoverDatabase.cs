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
using System.Web;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

using Gnome;
using Gdk;

public class CoverDatabase 
{
	private IntPtr dbf;

	public Hashtable Covers;

	private delegate void DecodeFuncDelegate (string key, IntPtr data, IntPtr user_data);
	
	[DllImport ("libmuine")]
	private static extern IntPtr db_open (string filename, int version, out string error);
	[DllImport ("libmuine")]
	private static extern void db_foreach (IntPtr dbf, DecodeFuncDelegate decode_func,
					       IntPtr user_data);
						   
	public CoverDatabase (int version)
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

		dbf = db_open (filename, version, out error);

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

	private Pixbuf BeautifyPixbuf (Pixbuf cover)
	{
		Pixbuf border;

		int target_size = 64; /* if this is changed, the glade file needs to be updated, too */

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

	public Pixbuf CoverPixbufFromFile (string filename)
	{
		Pixbuf cover;

		/* read the cover image */
		cover = new Pixbuf (filename);

		return BeautifyPixbuf (cover);
	}

	public Pixbuf CoverPixbufFromURL (string album_url)
	{
		Pixbuf cover;

		try {
			/* read the cover image */
			HttpWebRequest req = (HttpWebRequest) WebRequest.Create (album_url);
			req.UserAgent = "Muine";
			req.KeepAlive = false;
	
			WebResponse resp = req.GetResponse ();
			Stream s = resp.GetResponseStream ();
	
			try {
				cover = new Pixbuf (s);
			} catch {
				resp.Close ();

				return null;
			}

			resp.Close ();
		} catch {
			return null;
		}

		/* Trap Amazon 1x1 images */
		if (cover.Height == 1 && cover.Width == 1)
			return null;

		return BeautifyPixbuf (cover);
	}

	private delegate IntPtr EncodeFuncDelegate (IntPtr handle, out int length);

	[DllImport ("libmuine")]
	private static extern void db_store (IntPtr dbf, string key, bool overwrite,
					     EncodeFuncDelegate encode_func,
					     IntPtr user_data);

	public Pixbuf AddCoverLocal (string key, string filename)
	{
		Pixbuf pix = CoverPixbufFromFile (filename);

		AddCover (key, pix);

		return pix;
	}

	public void AddCover (string key, Pixbuf pix)
	{
		if (pix == null)
			return;

		Covers.Add (key, pix);

		db_store (dbf, key, false,
		          new EncodeFuncDelegate (EncodeFunc), pix.Handle);
	}

	public void ReplaceCover (string key, Pixbuf pix)
	{
		Covers.Remove (key);

		AddCover (key, pix);
	}

	public void AddCoverDummy (string key)
	{
		Covers.Add (key, null);
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

	private string SanitizeString (string s)
	{
		s = s.ToLower ();
		s = Regex.Replace (s, "\\(.*\\)", "");
		s = Regex.Replace (s, "\\[.*\\]", "");
		s = s.Replace ("-", " ");
		s = s.Replace ("_", " ");

		return s;
	}

	public string GetAlbumCoverURL (string artist, string album_title)
	{
		AmazonSearchService search_service = new AmazonSearchService ();

		string sane_album_title = SanitizeString (album_title);
		
		/* Prepare for handling multi-page results */
		int total_pages = 1;
		int current_page = 1;
		
		/* Create Encapsulated Request */
		ArtistRequest asearch = new ArtistRequest ();
		asearch.devtag = "INSERT DEV TAG HERE";
		asearch.artist = artist;
		asearch.keywords = sane_album_title;
		asearch.type = "heavy";
		asearch.mode = "music";
		asearch.tag = "webservices-20";
		
		while (current_page <= total_pages) {
			asearch.page = Convert.ToString (current_page);

			ProductInfo pi;
			try {
				pi = search_service.ArtistSearchRequest (asearch);
			} catch (Exception e){
				return null;
			}

			/* Amazon API requires this .. */
			Thread.Sleep (1000);
		
			int num_results = pi.Details.Length;
			total_pages = Convert.ToInt32 (pi.TotalPages);

			/* Work out how many matches are on this page */
			if (num_results < 1)
				return null;

			for (int i = 0; i < num_results; i++) {
				/* Ignore bracketed text on the result from Amazon */
				string sane_product_name = SanitizeString (pi.Details[i].ProductName);

				/* Compare the two strings statistically */
				string [] product_name_array = sane_product_name.Split (' ');
				string [] album_title_array = sane_album_title.Split (' ');
				Array.Sort (product_name_array);
				Array.Sort (album_title_array);

				int match_count = 0;
				foreach (string s in album_title_array) {
					if (Array.BinarySearch (product_name_array, s) >= 0)
						match_count++;
				}

				double match_percent;
				match_percent = match_count / (double) album_title_array.Length;

				if (match_percent > 0.6)
					return pi.Details [i].ImageUrlMedium;
			}

			current_page++;
		}

		return null;
	}
}
