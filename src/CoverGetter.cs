/*
 * Copyright (C) 2005 Jorn Baayen <jbaayen@gnome.org>
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
using System.Web;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Collections;

using Gdk;

namespace Muine
{
	public class CoverGetter
	{
		private const string GConfKeyAmazonLocale = "/apps/muine/amazon_locale";
		private const string GConfDefaultAmazonLocale = "us";
		
		private CoverDatabase db;
		private GnomeProxy proxy;
		private string amazon_locale;
		private GetAmazonThread amazon_thread;

		private static string [] cover_filenames = {
			"cover.jpg",
			"Cover.jpg",
			"cover.jpeg",
			"Cover.jpeg",
			"cover.png",
			"Cover.png",
			"folder.jpg",
			"Folder.jpg",
			"cover.gif",
			"Cover.gif"
		};

		public CoverGetter (CoverDatabase db)
		{
			this.db = db;
			
			amazon_locale = (string) Config.Get (GConfKeyAmazonLocale,
							     GConfDefaultAmazonLocale);
			Config.AddNotify (GConfKeyAmazonLocale,
					  new GConf.NotifyEventHandler (OnAmazonLocaleChanged));

			proxy = new GnomeProxy ();

			amazon_thread = new GetAmazonThread (this);
		}

		private void OnAmazonLocaleChanged (object o, GConf.NotifyEventArgs args)
		{
			amazon_locale = (string) args.Value;
		}

		// Getters
		public Pixbuf GetLocal (string key, string file)
		{
			// May throw an exception
			Pixbuf pix = new Pixbuf (file);

			pix = AddBorder (pix);

			db.SetCover (key, pix);

			return pix;
		}

		public Pixbuf GetEmbedded (string key, Pixbuf pixbuf)
		{
			Pixbuf pix = AddBorder (pixbuf);

			db.SetCover (key, pix);

			return pix;
		}

		public Pixbuf GetFolderImage (string key, string folder)
		{
			foreach (string fn in cover_filenames) {
				FileInfo cover = new FileInfo (Path.Combine (folder, fn));
				
				if (cover.Exists) {
					Pixbuf pix;

					try {
						pix = new Pixbuf (cover.FullName);
					} catch {
						continue;
					}

					pix = AddBorder (pix);

					db.SetCover (key, pix);

					return pix;
				}
			}

			return null;
		}

		// Web getting
		public Pixbuf GetWeb (string key, string url,
				      GotCoverDelegate done_func)
		{
			db.RemoveCover (key);

			new GetWebThread (this, key, url, done_func);

			return db.DownloadingPixbuf;
		}

		private class GetWebThread
		{
			private CoverGetter getter;
			private string key;
			private string url;
			private GotCoverDelegate done_func;
			private Pixbuf pixbuf = null;

			private void ThreadFunc ()
			{
				try {
					pixbuf = getter.Download (url);
					pixbuf = getter.AddBorder (pixbuf);
				} catch {}

				GLib.IdleHandler idle = new GLib.IdleHandler (SignalIdle);
				GLib.Idle.Add (idle);
			}
			
			private bool SignalIdle ()
			{
				if (Global.CoverDB.Covers [key] != null) {
					// has been modified while we were downloading ..
					return false;
				}

				if (pixbuf != null)
					Global.CoverDB.SetCover (key, pixbuf);

				done_func (pixbuf);

				return false;
			}
			
			public GetWebThread (CoverGetter getter,
					     string key, string url,
					     GotCoverDelegate done_func)
			{
				this.getter = getter;
				this.key = key;
				this.url = url;
				this.done_func = done_func;

				Thread thread = new Thread (new ThreadStart (ThreadFunc));
				thread.Priority = ThreadPriority.BelowNormal;
				thread.Start ();
			}
		}

		// Amazon getting
		public Pixbuf GetAmazon (Album album)
		{
			db.MarkAsBeingChecked (album.Key);

			amazon_thread.Queue.Enqueue (album);

			return db.DownloadingPixbuf;
		}

		private class GetAmazonThread
		{
			private CoverGetter getter;
			private Queue queue;

			private void ThreadFunc ()
			{
				while (true) {
					while (queue.Count > 0) {
						Album album = (Album) queue.Dequeue ();
						Pixbuf pixbuf = null;

						try {
							pixbuf = getter.DownloadFromAmazon (album);
							pixbuf = getter.AddBorder (pixbuf);
						} catch (WebException e) {
							// Temporary web problem (Timeout etc.) - re-queue
							Thread.Sleep (60000); /* wait for a minute first */
							queue.Enqueue (album);
							continue;
						} catch (Exception e) {}

						new IdleData (album, pixbuf);
					}

					Thread.Sleep (1000);
				}
			}
			
			private class IdleData
			{
				private Album album;
				private Pixbuf pixbuf;

				private bool IdleFunc ()
				{
					string key = album.Key;
					
					if (Global.CoverDB.Covers [key] != null) {
						// has been modified while we were downloading ..
						return false;
					}

					if (pixbuf != null)
						Global.CoverDB.SetCover (key, pixbuf);
					else
						Global.CoverDB.UnmarkAsBeingChecked (key);

					album.CoverImage = pixbuf;
		
					return false;
				}

				public IdleData (Album album, Pixbuf pixbuf)
				{
					this.album = album;
					this.pixbuf = pixbuf;

					GLib.IdleHandler idle = new GLib.IdleHandler (IdleFunc);
					GLib.Idle.Add (idle);
				}
			}
			
			public GetAmazonThread (CoverGetter getter)
			{
				this.getter = getter;

				queue = Queue.Synchronized (new Queue ());

				Thread thread = new Thread (new ThreadStart (ThreadFunc));
				thread.Priority = ThreadPriority.BelowNormal;
				thread.Start ();
			}

			public Queue Queue {
				get { return queue; }
			}
		}

		public Pixbuf DownloadFromAmazon (Album album)
		{
			Amazon.AmazonSearchService search_service = new Amazon.AmazonSearchService ();

			string sane_album_title = SanitizeString (album.Name);
			/* remove "disc 1" and family */
			sane_album_title =  Regex.Replace (sane_album_title, @"[,:]?\s*(cd|dis[ck])\s*(\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s*$", "");

			string [] album_title_array = sane_album_title.Split (' ');
			Array.Sort (album_title_array);

			/* This assumes the right artist is always in Artists [0] */
			string sane_artist = SanitizeString (album.Artists [0]);
			
			/* Prepare for handling multi-page results */
			int total_pages = 1;
			int current_page = 1;
			int max_pages = 2; /* check no more than 2 pages */
			
			/* Create Encapsulated Request */
			Amazon.ArtistRequest asearch = new Amazon.ArtistRequest ();
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

				Amazon.ProductInfo pi;
				
				/* Amazon API requires this .. */
				Thread.Sleep (1000);
			
				/* Web service calls timeout after 30 seconds */
				search_service.Timeout = 30000;
				if (proxy.Use)
					search_service.Proxy = proxy.Proxy;
				
				/* This may throw an exception, we catch it in the calling function */
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

					if (match_percent < 0.6)
						continue;

					string url = pi.Details [i].ImageUrlMedium;

					if (url == null || url.Length == 0)
						continue;

					double forward_match_percent = match_percent;
					double backward_match_percent = 0.0;
					int backward_match_count = 0;

					foreach (string s in product_name_array) {
						if (Array.BinarySearch (album_title_array, s) >= 0)
							backward_match_count++;
					}
					backward_match_percent = backward_match_count / (double) product_name_array.Length;

					double total_match_percent = match_percent + backward_match_percent;
					if (total_match_percent <= best_match_percent)
						continue; // look for a better match
						
					Pixbuf pix;
								
					try {
						pix = Download (url);
						if (pix == null && amazon_locale != "us") {
							// Manipulate the image URL since Amazon sometimes return it wrong :(
							// http://www.amazon.com/gp/browse.html/103-1953981-2427826?node=3434651#misc-image
							url = Regex.Replace (url, "[.]0[0-9][.]", ".01.");
							pix = Download (url);
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

				current_page++;
			}

			return best_match;
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

		// Utility functions
		public Pixbuf Download (string url)
		{
			Pixbuf cover;

			/* read the cover image */
			HttpWebRequest req = (HttpWebRequest) WebRequest.Create (url);
			req.UserAgent = "Muine";
			req.KeepAlive = false;
			req.Timeout = 30000; /* Timeout after 30 seconds */
			if (proxy.Use)
				req.Proxy = proxy.Proxy;
				
			WebResponse resp = null;
		
			/* May throw an exception, but we catch it in the calling
			 * function */
			resp = req.GetResponse ();

			Stream s = resp.GetResponseStream ();
		
			cover = new Pixbuf (s);

			resp.Close ();

			/* Trap Amazon 1x1 images */
			if (cover.Height == 1 && cover.Width == 1)
				return null;

			return cover;
		}

		public Pixbuf AddBorder (Pixbuf cover)
		{
			Pixbuf border;

			/* 1px border, so -2 .. */
			int target_size = db.CoverSize - 2;

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

		public delegate void GotCoverDelegate (Pixbuf pixbuf);
	}
}
