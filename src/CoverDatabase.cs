/*
 * Copyright (C) 2004, 2005 Jorn Baayen <jbaayen@gnome.org>
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
using System.Threading;

using Gdk;

namespace Muine
{
	public class CoverDatabase 
	{
		// we don't bother to get this one back from GtkIconSize as we'd
		// have to resize all our covers ..
		public const int CoverSize = 66;

		private Hashtable covers;
		public Hashtable Covers {
			get { return covers; }
		}

		private Pixbuf downloading_pixbuf;
		public Pixbuf DownloadingPixbuf {
			get { return downloading_pixbuf; }
		}

		private CoverGetter getter;
		public CoverGetter Getter {
			get { return getter; }
		}
		
		private Database db;

		/*** constructor ***/
		public CoverDatabase (int version)
		{
			db = new Database (FileUtils.CoversDBFile, version);
			db.DecodeFunction = new Database.DecodeFunctionDelegate (DecodeFunction);
			db.EncodeFunction = new Database.EncodeFunctionDelegate (EncodeFunction);

			covers = Hashtable.Synchronized (new Hashtable ());

			/* Hack to get the GtkStyle .. */
			Gtk.Label label = new Gtk.Label ("");
			label.EnsureStyle ();
			downloading_pixbuf = label.RenderIcon ("muine-cover-downloading",
							       StockIcons.CoverSize, null);
			label.Destroy ();

			getter = new CoverGetter (this);
		}

		/*** loading ***/
		private void DecodeFunction (string key, IntPtr data)
		{
			IntPtr p = data;

			bool being_checked;
			p = Database.UnpackBool (p, out being_checked);
		
			LoadedCover lc;
			if (being_checked) {
				lc = new LoadedCover (key);
			} else {
				IntPtr pix_handle;
				p = Database.UnpackPixbuf (p, out pix_handle);

				lc = new LoadedCover (key, pix_handle);
			}

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

			private bool being_checked;
			public bool BeingChecked {
				get { return being_checked; }
			}

			public LoadedCover (string key, IntPtr pixbuf_ptr) {
				this.key = key;
				pixbuf = new Pixbuf (pixbuf_ptr);
				being_checked = false;
			}

			public LoadedCover (string key) {
				this.key = key;
				pixbuf = null;
				being_checked = true;
			}
		}

		private bool ProcessActionsFromThread ()
		{
			int counter = 0;
			
			if (loaded_covers.Count > 0) {
				while (loaded_covers.Count > 0 && counter < 10) {
					counter++;

					LoadedCover lc = (LoadedCover) loaded_covers.Dequeue ();
		
					if (lc.BeingChecked) {
						Album ab = Muine.DB.GetAlbum (lc.Key);

						if (ab != null)
							ab.SetCoverAmazon ();
							
						continue;
					}

					if (!Covers.ContainsKey (lc.Key))
						Covers.Add (lc.Key, lc.Pixbuf);

					Album a = Muine.DB.GetAlbum (lc.Key);
					if (a != null)
						a.CoverImage = lc.Pixbuf;
					else {
						Song s = Muine.DB.GetSong (lc.Key);

						if (s != null)
							s.CoverImage = lc.Pixbuf;
					}
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
		public void SetCover (string key, Pixbuf pix)
		{
			bool replace = Covers.ContainsKey (key);

			if (replace)
				Covers.Remove (key);

			Covers.Add (key, pix);

			lock (db)
				db.Store (key, pix != null ?
					       pix.Handle : IntPtr.Zero, replace);
		}

		public void RemoveCover (string key)
		{
			if (!Covers.ContainsKey (key))
				return;

			lock (db)
				db.Delete (key);

			Covers.Remove (key);
		}

		public void MarkAsBeingChecked (string key)
		{
			SetCover (key, null);
		}

		private IntPtr EncodeFunction (IntPtr handle, out int length)
		{
			IntPtr p = Database.PackStart ();

			bool being_checked = (handle == IntPtr.Zero);
			
			Database.PackBool (p, being_checked);

			if (!being_checked)
				Database.PackPixbuf (p, handle);

			return Database.PackEnd (p, out length);
		}
	}
}
