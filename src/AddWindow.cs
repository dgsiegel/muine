/*
 * Copyright (C) 2005 Tamara Roberson <foxxygirltamara@gmail.com>
 *           (C) 2003, 2004, 2005 Jorn Baayen <jbaayen@gnome.org>
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
using Gtk;

namespace Muine
{
	public abstract class AddWindow : Window
	{
		// Enums
		public enum ResponseType {
			Close       = Gtk.ResponseType.Close,
			DeleteEvent = Gtk.ResponseType.DeleteEvent,
			Play        = 1,
			Queue       = 2
		};

		// Events
		public delegate void QueueEventHandler (GLib.List songs);
		public event QueueEventHandler QueueEvent;

		public delegate void PlayEventHandler (GLib.List songs);
		public event PlayEventHandler PlayEvent;

		// Widgets
		[Glade.Widget] private Window         window;
		[Glade.Widget] private Label          search_label;
		[Glade.Widget] private Container      entry_container;
		[Glade.Widget] private Button         play_button;
		[Glade.Widget] private Image          play_button_image;
		[Glade.Widget] private Button         queue_button;
		[Glade.Widget] private Image          queue_button_image;
		[Glade.Widget] private ScrolledWindow scrolledwindow;
		
		private AddWindowEntry entry         = new AddWindowEntry         ();
		private AddWindowList  list          = new AddWindowList          ();
		private CellRenderer   text_renderer = new Muine.CellRendererText ();

		// Objects
		private ICollection items;

		// Variables
		private string gconf_key_width, gconf_key_height;
		private int gconf_default_width, gconf_default_height;
		
		private bool process_changes_immediately = false;
		private uint search_timeout_id = 0;
		private const uint search_timeout = 100;
		private bool first_time = true;

		// Constructor
		public AddWindow () : base (IntPtr.Zero)
		{
			Glade.XML gxml = new Glade.XML (null, "AddWindow.glade", "window", null);
			gxml.Autoconnect (this);

			Raw = window.Handle;

			play_button_image.SetFromStock  ("stock_media-play", IconSize.Button);
			queue_button_image.SetFromStock ("stock_timer"     , IconSize.Button);

			// Label
			search_label.MnemonicWidget = entry;

			// Entry
			entry.KeyPressEvent += new Gtk.KeyPressEventHandler (OnEntryKeyPressEvent);
			entry.Changed       += new System.EventHandler      (OnEntryChanged);
			entry_container.Add (entry);
						
			// List
			list.RowActivated     += new AddWindowList.RowActivatedHandler     (OnRowActivated);
			list.SelectionChanged += new AddWindowList.SelectionChangedHandler (OnSelectionChanged);
			scrolledwindow.Add (list);

			entry.Show ();
			list.Show ();
		}

		// Properties
		// Properties :: List (get;)
		public AddWindowList List {
			get { return list; }
		}

		// Properties :: Entry (get;)
		public AddWindowEntry Entry {
			get { return entry; }
		}
		
		// Properties :: TextRenderer (get;)
		public CellRenderer TextRenderer {
			get { return text_renderer; }
		}

		// Properties :: Items (set; get;)
		public ICollection Items {
			set { items = value; }
			get { return items;  }
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: Run
		public void Run ()
		{
			if (first_time) {
				// So that we do the initial query *after* th
				// window has been shown. It seems more 
				// responsive this way.
				window.Realize ();
				window.GdkWindow.Cursor = new Gdk.Cursor (Gdk.CursorType.Watch);
				window.GdkWindow.Display.Flush ();

				search_timeout_id = GLib.Idle.Add (new GLib.IdleHandler (FirstSearch));

				first_time = false;
			}

			entry.GrabFocus ();

			window.Present ();
		}
			
		// Methods :: Protected
		// Methods :: Protected :: SetGConfSize
		protected void SetGConfSize (string key_width , int default_width, 
					     string key_height, int default_height)
		{
			gconf_key_width  = key_width;
			gconf_key_height = key_height;
			
			gconf_default_width  = default_width;
			gconf_default_height = default_height;
			
			int width  = (int) Config.Get (key_width , default_width );
			int height = (int) Config.Get (key_height, default_height);

			if (width < 1)
				width = gconf_default_width;
			
			if (height < 1)
				height = gconf_default_height;

			SetDefaultSize (width, height);
			
			AddOnSizeAllocated ();
		}

		// Methods :: Private
		// Methods :: Private :: Assertions
		// Methods :: Private :: Assertions :: HasGConfSize
		private bool HasGConfSize ()
		{
			return (gconf_key_width  != String.Empty && gconf_default_width  > 0 &&
				gconf_key_height != String.Empty && gconf_default_height > 0);
		}

		// Methods :: Private :: Assertions :: AssertHasGConfSize
		private void AssertHasGConfSize ()
		{
			if (!HasGConfSize ())
			    	throw new InvalidOperationException ();		
		}

		
		// Methods :: Private :: Assertions :: HasItems
		private bool HasItems ()
		{
			return (items != null);
		}

		// Methods :: Private :: Assertions :: AssertHasItems
		private void AssertHasItems ()
		{
			if (!HasItems ())
				throw new InvalidOperationException ();
		}

		// Methods :: Private :: AddOnSizeAllocated
		private void AddOnSizeAllocated ()
		{
			AssertHasGConfSize ();
			
			window.SizeAllocated += new SizeAllocatedHandler (OnSizeAllocated);		
		}

		// Methods :: Protected :: Search
		protected bool Search ()
		{
			AssertHasItems ();
		
			GLib.List l = new GLib.List (IntPtr.Zero, typeof (int));

			lock (Global.DB) {
				if (entry.Text.Length > 0) {
					foreach (Item item in items) {
						if (!item.FitsCriteria (entry.SearchBits))
							continue;

						l.Append (item.Handle);
					}
				} else {
					foreach (Item item in items) {
						if (!item.Public)
							continue;

						l.Append (item.Handle);
					}
				}
			}

			list.RemoveDelta (l);

			foreach (int p in l) {
				IntPtr ptr = new IntPtr (p);
				list.Append (ptr);
			}

			list.SelectFirst ();

			return false;
		}

		// Methods :: Private :: FirstSearch
		private bool FirstSearch ()
		{
			Search ();

			// We want to get the normal cursor back *after* treeview
			// has done its thing.
			GLib.Idle.Add (new GLib.IdleHandler (RestoreCursor));

			return false;
		}
		
		// Methods :: Private :: RestoreCursor
		private bool RestoreCursor ()
		{
			window.GdkWindow.Cursor = null;

			return false;
		}

		// Handlers
		// Handlers :: OnWindowDeleteEvent
		private void OnWindowDeleteEvent (object o, DeleteEventArgs args)
		{
			args.RetVal = true;
		}

		// Handlers :: OnRowActivated
		private void OnRowActivated (IntPtr handle)
		{
			play_button.Click ();
		}

		// Handlers :: OnSelectionChanged
		private void OnSelectionChanged ()
		{
			play_button.Sensitive  = list.HasSelection;
			queue_button.Sensitive = list.HasSelection;
		}
		
		// Handlers :: OnEntryKeyPressEvent
		private void OnEntryKeyPressEvent (object o, KeyPressEventArgs args)
		{
			args.RetVal = list.ForwardKeyPress (entry, args.Event);
		}

		// Handlers :: OnSizeAllocated
		private void OnSizeAllocated (object o, SizeAllocatedArgs args)
		{
			if (!HasGConfSize ())
				return;

			int width, height;
			window.GetSize (out width, out height);

			Config.Set (gconf_key_width , width );
			Config.Set (gconf_key_height, height);
		}

		// Handlers :: OnWindowResponse
		private void OnWindowResponse (object o, ResponseArgs args)
		{
			switch ((int) args.ResponseId) {
			case (int) ResponseType.DeleteEvent:
			case (int) ResponseType.Close:
				window.Visible = false;
				
				break;
				
			case (int) ResponseType.Play:
				window.Visible = false;
				
				if (PlayEvent != null)
					PlayEvent (list.Selected);

				list.SelectNext ();

				break;
				
			case (int) ResponseType.Queue:
				if (QueueEvent != null)
					QueueEvent (list.Selected);
					
				entry.GrabFocus ();

				list.SelectNext ();

				break;
				
			default:
				throw new ArgumentException ();
			}
		}
		
		// Handlers :: OnEntryChanged
		private void OnEntryChanged (object o, EventArgs args)
		{
			if (process_changes_immediately)
				Search ();
			else {
				if (search_timeout_id > 0)
					GLib.Source.Remove (search_timeout_id);

				search_timeout_id = GLib.Timeout.Add (search_timeout, new GLib.TimeoutHandler (Search));
			}
		}
		
		// Handlers :: OnAdded
		// 	UNUSED: Requires Mono 1.1+
		protected void OnAdded (Item item)
		{
			list.HandleAdded (item.Handle, item.FitsCriteria (entry.SearchBits));
		}

		// Handlers :: OnChanged
		// 	UNUSED: Requires Mono 1.1+
		protected void OnChanged (Item item)
		{
			list.HandleChanged (item.Handle, item.FitsCriteria (entry.SearchBits));
		}

		// Handlers :: OnRemoved
		// 	UNUSED: Requires Mono 1.1+
		protected void OnRemoved (Item item)
		{
			list.HandleRemoved (item.Handle);
		}
	}
}
