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

using Gdk;

namespace Muine
{
	public class CoverDatabase 
	{
		// Constants
		// Constants :: CoverSize
		// 	We don't bother to get this one back from GtkIconSize as we'd
		// 	have to resize all our covers.
		public const int CoverSize = 66;

		// Events
		public delegate void DoneLoadingHandler ();
		public event         DoneLoadingHandler DoneLoading;

		// Internal Classes
		// Internal Classes :: LoadThread
		// 	XXX: Split off? This is big.
		private class LoadThread : ThreadBase
		{
			// Structs
			private struct LoadedCover {
				public Pixbuf Pixbuf;
				public Item Item;

				public LoadedCover (Item item, Pixbuf pixbuf) {
					Item   = item;
					Pixbuf = pixbuf;
				}
			}

			// Variables
			private Database db;			

			// Methods
			// Methods :: Public
			// Methods :: Public :: LoadThread
			public LoadThread (Database db)
			{
				this.db = db;
				thread.Start ();
			}

			// Methods :: Protected
			// Methods :: Protected :: MainLoopIdle
			protected override bool MainLoopIdle ()
			{
				if (queue.Count == 0) {
					if (thread_done) {
						Global.CoverDB.EmitDoneLoading ();
						
						return false;
					} else
						return true;
				}

				LoadedCover lc = (LoadedCover) queue.Dequeue ();
			
				lc.Item.CoverImage = lc.Pixbuf;

				return true;
			}

			// Methods :: Private
			// Methods :: Private :: DecodeFunction
			private void DecodeFunction (string key, IntPtr data)
			{
				IntPtr p = data;

				bool being_checked;
				p = Database.UnpackBool (p, out being_checked);
		
				Pixbuf pixbuf = null;
				if (!being_checked) {
					IntPtr pix_handle;
					p = Database.UnpackPixbuf (p, out pix_handle);
					pixbuf = new Pixbuf (pix_handle);
				}

				if (Global.CoverDB.Covers.Contains (key)) {
					if (being_checked)
						return;
					else // stored covers take priority
						Global.CoverDB.Covers.Remove (key);
				}
				
				// Add independent of whether item is null or not,
				// this way manually set covers will stay for
				// removable devices.
				if (!being_checked) 
					Global.CoverDB.Covers.Add (key, pixbuf);

				Item item = Global.DB.GetAlbum (key);
				if (item == null)
					item = Global.DB.GetSong (key);
				if (item != null) {
					if (being_checked) {
						// false, as we don't want to write to the db
						// while we're loading
						pixbuf = Global.CoverDB.Getter.GetAmazon ((Album) item, false);
						Global.CoverDB.Covers.Add (key, null);
					}
					
					LoadedCover lc = new LoadedCover (item, pixbuf);
					queue.Enqueue (lc);
				}
			}

			// Delegate Functions 
			// Delegate Functions :: ThreadFunc
			protected override void ThreadFunc ()
			{
				lock (Global.CoverDB) {
					db.DecodeFunction = new Database.DecodeFunctionDelegate (DecodeFunction);
					db.Load ();
				}

				thread_done = true;
			}
		}

		// Objects
		private Hashtable covers;
		private Pixbuf downloading_pixbuf;
		private CoverGetter getter;
		private Database db;	

		// Variables
		private bool loading = true;

		// Constructor
		public CoverDatabase (int version)
		{
			db = new Database (FileUtils.CoversDBFile, version);
			db.EncodeFunction = new Database.EncodeFunctionDelegate (EncodeFunction);

			covers = new Hashtable ();

			// Hack to get the GtkStyle
			Gtk.Label label = new Gtk.Label ("");
			label.EnsureStyle ();
			downloading_pixbuf = label.RenderIcon ("muine-cover-downloading", StockIcons.CoverSize, null);
			label.Destroy ();

			getter = new CoverGetter (this);
		}

		// Properties
		// Properties :: Covers (get;)
		public Hashtable Covers {
			get { return covers; }
		}

		// Properties :: DownloadingPixbuf (get;)
		public Pixbuf DownloadingPixbuf {
			get { return downloading_pixbuf; }
		}

		// Properties :: Getter (get;)
		public CoverGetter Getter {
			get { return getter; }
		}

		// Properties :: Loading (get;)
		public bool Loading {
			get { return loading; }
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: Load
		public void Load ()
		{
			LoadThread l = new LoadThread (db);
		}

		// Methods :: Public :: SetCover
		public void SetCover (string key, Pixbuf pix)
		{
			lock (this) {
				bool replace = Covers.ContainsKey (key);

				if (replace)
					Covers.Remove (key);

				Covers.Add (key, pix);

				db.Store (key, pix != null ?
					       pix.Handle : IntPtr.Zero, replace);
			}
		}

		// Methods :: Public :: RemoveCover
		public void RemoveCover (string key)
		{
			lock (this) {
				if (!Covers.ContainsKey (key))
					return;

				db.Delete (key);

				Covers.Remove (key);
			}
		}

		// Methods :: Public :: MarkAsBeingChecked
		public void MarkAsBeingChecked (string key)
		{
			SetCover (key, null);
		}

		// Methods ::: Public :: UnmarkAsBeingChecked
		public void UnmarkAsBeingChecked (string key)
		{
			RemoveCover (key);
		}
				
		// Methods :: Private
		// Methods :: Private :: EncodeFunction
		// 	Database interaction
		private IntPtr EncodeFunction (IntPtr handle, out int length)
		{
			IntPtr p = Database.PackStart ();

			bool being_checked = (handle == IntPtr.Zero);
			
			Database.PackBool (p, being_checked);

			if (!being_checked)
				Database.PackPixbuf (p, handle);

			return Database.PackEnd (p, out length);
		}

		// Methods :: Private :: EmitDoneLoading
		private void EmitDoneLoading ()
		{
			loading = false;

			if (DoneLoading != null)
				DoneLoading ();
		}


	}
}
