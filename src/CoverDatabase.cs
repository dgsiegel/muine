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
using System.Collections;
using System.IO;
using System.Web;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

using Gnome;
using Gdk;

public class CoverDatabase 
{
	public const int AlbumCoverSize = 66;

        private const string GConfKeyAmazonLocale = "/apps/muine/amazon_locale";
        private const string GConfDefaultAmazonLocale = "us";

	private Hashtable covers;
	public Hashtable Covers {
		get { return covers; }
	}

	private Pixbuf downloading_pixbuf;
	public Pixbuf DownloadingPixbuf {
		get { return downloading_pixbuf; }
	}
	
	private string amazon_locale;

	/*** constructor ***/
	private Database db;

	private GnomeProxy proxy;

	public CoverDatabase (int version)
	{
		amazon_locale = (string) Muine.GetGConfValue (GConfKeyAmazonLocale, GConfDefaultAmazonLocale);

		db = new Database (Muine.CoversDBFile, version);
		db.DecodeFunction = new Database.DecodeFunctionDelegate (DecodeFunction);
		db.EncodeFunction = new Database.EncodeFunctionDelegate (EncodeFunction);

		covers = new Hashtable ();

		proxy = new GnomeProxy ();

		/* Hack to get the GtkStyle .. */
		Gtk.Label label = new Gtk.Label ("");
		label.EnsureStyle ();
		downloading_pixbuf = label.RenderIcon ("muine-cover-downloading",
						       StockIcons.AlbumCoverSize, null);
		label.Destroy ();
	}

	/*** loading ***/

	private void DecodeFunction (string key, IntPtr data)
	{
		IntPtr pix_handle;
		
		Database.UnpackPixbuf (data, out pix_handle);

		LoadedCover lc = new LoadedCover (key, pix_handle);

		loaded_covers.Enqueue (lc);
	}

	private bool thread_done;

	private Queue loaded_covers;

	private bool loading = true;
	public bool Loading {
		get { return loading; }
	}

	public delegate void DoneLoadingHandler ();
	public event DoneLoadingHandler DoneLoading;

	public void Load ()
	{
		thread_done = false;

		loaded_covers = Queue.Synchronized (new Queue ());

		GLib.Idle.Add (new GLib.IdleHandler (ProcessActionsFromThread));
		
		Thread thread = new Thread (new ThreadStart (LoadThread));
		thread.Priority = ThreadPriority.BelowNormal;
		thread.Start ();
	}

	private struct LoadedCover {
		private string key;
		public string Key {
			get { return key; }
		}
		
		private Pixbuf pixbuf;
		public Pixbuf Pixbuf {
			get { return pixbuf; }
		} 

		public LoadedCover (string key, IntPtr pixbuf_ptr) {
			this.key = key;
			pixbuf = new Pixbuf (pixbuf_ptr);
		}
	}

	private bool ProcessActionsFromThread ()
	{
		int counter = 0;
		
		if (loaded_covers.Count > 0) {
			while (loaded_covers.Count > 0 && counter < 10) {
				LoadedCover lc = (LoadedCover) loaded_covers.Dequeue ();
	
				if (!Covers.ContainsKey (lc.Key))
					Covers.Add (lc.Key, lc.Pixbuf);

				Album a = (Album) Muine.DB.Albums [lc.Key];
				if (a != null) {
					a.CoverImage = lc.Pixbuf;

					Muine.DB.EmitAlbumChanged (a);
				}

				counter++;
			}

			return true;
		} else if (thread_done) {
			loading = false;

			if (DoneLoading != null)
				DoneLoading ();
		}
		
		return !thread_done;
	}

	private void LoadThread ()
	{
		lock (db)
			db.Load ();

		thread_done = true;
	}

	/*** storing ***/
	private Pixbuf BeautifyPixbuf (Pixbuf cover)
	{
		Pixbuf border;

		/* 1px border, so -2 .. */
		int target_size = AlbumCoverSize - 2;

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

		/* create the background + black border pixbuf */
		border = new Pixbuf (Colorspace.Rgb, true, 8, cover.Width + 2, cover.Height + 2);
		border.Fill (0x000000ff);
			
		/* put the cover image on the border area */
		cover.CopyArea (0, 0, cover.Width, cover.Height, border, 1, 1);

		/* done */
		return border;
	}

	public Pixbuf DownloadCoverPixbuf (string album_url)
	{
		Pixbuf cover;

		/* read the cover image */
		HttpWebRequest req = (HttpWebRequest) WebRequest.Create (album_url);
		req.UserAgent = "Muine";
		req.KeepAlive = false;
		req.Timeout = 30000; /* Timeout after 30 seconds */
		if (proxy.Use)
			req.Proxy = proxy.Proxy;
			
		WebResponse resp = null;
	
		/* May throw an exception, but we catch it in the calling
		 * function in Song.cs */
		resp = req.GetResponse ();

		Stream s = resp.GetResponseStream ();
	
		cover = new Pixbuf (s);

		resp.Close ();

		/* Trap Amazon 1x1 images */
		if (cover.Height == 1 && cover.Width == 1)
			return null;

		return BeautifyPixbuf (cover);
	}

	public Pixbuf AddCoverLocal (string key, string filename)
	{
		Pixbuf pix;

		try {
			pix = new Pixbuf (filename);
		} catch (Exception e) {
			return null;
		}

		pix = BeautifyPixbuf (pix);

		AddCover (key, pix);

		return pix;
	}

	public Pixbuf AddCoverEmbedded (string key, Pixbuf cover_image)
	{
		Pixbuf pix = BeautifyPixbuf (cover_image);

		AddCover (key, pix);

		return pix;
	}

	public Pixbuf AddCoverDownloading (string key)
	{
		Covers.Add (key, DownloadingPixbuf);

		return DownloadingPixbuf;
	}

	public void AddCover (string key, Pixbuf pix)
	{
		if (pix == null)
			return;

		Covers.Add (key, pix);

		lock (db)
			db.Store (key, pix.Handle);
	}

	public void ReplaceCover (string key, Pixbuf pix)
	{
		Covers.Remove (key);

		AddCover (key, pix);
	}

	public void RemoveCover (string key)
	{
		lock (db)
			db.Delete (key);

		Covers.Remove (key);
	}

	private IntPtr EncodeFunction (IntPtr handle, out int length)
	{
		IntPtr p = Database.PackStart ();
		Database.PackPixbuf (p, handle);
		return Database.PackEnd (p, out length);
	}

	private string SanitizeString (string s)
	{
		s = s.ToLower ();
		s = Regex.Replace (s, "\\(.*\\)", "");
		s = Regex.Replace (s, "\\[.*\\]", "");
		s = s.Replace ("-", " ");
		s = s.Replace ("_", " ");
		s = Regex.Replace (s, " +", " ");

		return s;
	}

	public Pixbuf GetAlbumCoverFromAmazon (Song song)
	{
		AmazonSearchService search_service = new AmazonSearchService ();

		string sane_album_title = SanitizeString (song.Album);
		/* remove "disc 1" and family */
		sane_album_title =  Regex.Replace (sane_album_title, @"[,:]?\s*(cd|dis[ck])\s*(\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s*$", "");

		string [] album_title_array = sane_album_title.Split (' ');
		Array.Sort (album_title_array);

		/* This assumes the right artist is always in Artists [0] */
		string sane_artist = SanitizeString (song.Artists [0]);
		
		/* Prepare for handling multi-page results */
		int total_pages = 1;
		int current_page = 1;
		int max_pages = 2; /* check no more than 2 pages */
		
		/* Create Encapsulated Request */
		ArtistRequest asearch = new ArtistRequest ();
		asearch.devtag = "INSERT DEV TAG HERE";
		asearch.artist = sane_artist;
		asearch.keywords = sane_album_title;
		asearch.type = "heavy";
		asearch.mode = "music";
		asearch.tag = "webservices-20";

		/* Use selected Amazon service */
		switch (amazon_locale) {
		case "uk":
			search_service.Url = "http://soap-eu.amazon.com/onca/soap3";
			asearch.locale = "uk";

			break;
		case "de":
			search_service.Url = "http://soap-eu.amazon.com/onca/soap3";
			asearch.locale = "de";

			break;
		case "jp":
			search_service.Url = "http://soap.amazon.com/onca/soap3";
			asearch.locale = "jp";

			break;
		default:
			search_service.Url = "http://soap.amazon.com/onca/soap3";

			break;
		}

		double best_match_percent = 0.0;
		Pixbuf best_match = null;

		while (current_page <= total_pages && current_page <= max_pages) {
			asearch.page = Convert.ToString (current_page);

			ProductInfo pi;
			
			/* Amazon API requires this .. */
			Thread.Sleep (1000);
		
			/* Web service calls timeout after 30 seconds */
			search_service.Timeout = 30000;
			if (proxy.Use)
				search_service.Proxy = proxy.Proxy;
			
			/* This may throw an exception, we catch it in Song.cs in the calling function */
			pi = search_service.ArtistSearchRequest (asearch);

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
				Array.Sort (product_name_array);

				int match_count = 0;
				foreach (string s in album_title_array) {
					if (Array.BinarySearch (product_name_array, s) >= 0)
						match_count++;
				}

				double match_percent;
				match_percent = match_count / (double) album_title_array.Length;

				if (match_percent >= 0.6) {
					string url = pi.Details [i].ImageUrlMedium;

					if (url != null && url.Length > 0)
					{
						double forward_match_percent = match_percent;
						double backward_match_percent = 0.0;
						int backward_match_count = 0;

						foreach (string s in product_name_array) {
							if (Array.BinarySearch (album_title_array, s) >= 0)
								backward_match_count++;
						}
						backward_match_percent = backward_match_count / (double) product_name_array.Length;

						double total_match_percent = match_percent + backward_match_percent;
						if (total_match_percent > best_match_percent) {
							Pixbuf pix;
							
							try {
								pix = DownloadCoverPixbuf (url);
								if (pix == null && amazon_locale != "us") {
									// Manipulate the image URL since Amazon sometimes return it wrong :(
									// http://www.amazon.com/gp/browse.html/103-1953981-2427826?node=3434651#misc-image
									url = Regex.Replace (url, "[.]0[0-9][.]", ".01.");
									pix = DownloadCoverPixbuf (url);
								}
							} catch (WebException e) {
								throw e;
							} catch (Exception e) {
								pix = null;
							}

							if (pix != null) {
								best_match_percent = total_match_percent;
								best_match = pix;

								if (best_match_percent == 2.0)
									return best_match;
							}
						}
						// ELSE keep iterating to find a better match
					}
				}
			}

			current_page++;
		}

		return best_match;
	}
}
