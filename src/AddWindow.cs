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
		[Glade.Widget] private Container      entry_container;
		[Glade.Widget] private Button         play_button;
		[Glade.Widget] private Image          play_button_image;
		[Glade.Widget] private Button         queue_button;
		[Glade.Widget] private Image          queue_button_image;
		[Glade.Widget] private ScrolledWindow scrolledwindow;
		
		private AddWindowEntry entry         = new AddWindowEntry   ();
		private AddWindowList  list          = new AddWindowList    ();
		private CellRenderer   text_renderer = new CellRendererText ();

		// Variables
		private string gconf_key_width, gconf_key_height;
		private int gconf_default_width, gconf_default_height;
		
		private bool process_changes_immediately = false;
		private uint search_idle_id = 0;

		// Constructor
		public AddWindow () : base (IntPtr.Zero)
		{
			Glade.XML gxml = new Glade.XML (null, "AddWindow.glade", "window", null);
			gxml.Autoconnect (this);

			Raw = window.Handle;

			play_button_image.SetFromStock  ("stock_media-play", IconSize.Button);
			queue_button_image.SetFromStock ("stock_timer"     , IconSize.Button);

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
		public AddWindowList List {
			get { return list; }
		}

		public AddWindowEntry Entry {
			get { return entry; }
		}
		
		public CellRenderer TextRenderer {
			get { return text_renderer; }
		}
		
		// Abstract methods
		protected abstract bool Search ();

		// Public Methods
		public void Run ()
		{
			entry.GrabFocus ();

			list.SelectFirst ();

			window.Present ();
		}
			
		// Protected Methods
		protected void SetGConfSize (string key_width, string key_height, 
					     int default_width, int default_height)
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

		protected void Reset ()
		{
			process_changes_immediately = true;
			
			entry.Clear ();

			process_changes_immediately = false;
		}
		
		// Private Methods
		// Private Methods :: Assertions
		private bool HasGConfSize ()
		{
			return (gconf_key_width  != String.Empty && gconf_default_width  > 0 &&
				gconf_key_height != String.Empty && gconf_default_height > 0);
		}

		private void AssertHasGConfSize ()
		{
			if (!HasGConfSize())
			    	throw new InvalidOperationException ();		
		}

		// Private Methods :: Other		
		private void AddOnSizeAllocated ()
		{
			AssertHasGConfSize ();
			
			window.SizeAllocated += new SizeAllocatedHandler (OnSizeAllocated);		
		}

		// Handlers
		private void OnWindowDeleteEvent (object o, DeleteEventArgs args)
		{
			args.RetVal = true;
		}

		private void OnRowActivated (IntPtr handle)
		{
			play_button.Click ();
		}

		private void OnSelectionChanged ()
		{
			play_button.Sensitive  = list.HasSelection;
			queue_button.Sensitive = list.HasSelection;
		}
		
		private void OnEntryKeyPressEvent (object o, KeyPressEventArgs args)
		{
			args.RetVal = list.ForwardKeyPress (entry, args.Event);
		}

		private void OnSizeAllocated (object o, SizeAllocatedArgs args)
		{
			if (!HasGConfSize ())
				return;

			int width, height;
			window.GetSize (out width, out height);

			Config.Set (gconf_key_width , width );
			Config.Set (gconf_key_height, height);
		}

		private void OnWindowResponse (object o, ResponseArgs args)
		{
			switch ((int) args.ResponseId) {
			case (int) ResponseType.DeleteEvent:
			case (int) ResponseType.Close:
				window.Visible = false;
				
				Reset ();
				
				break;
				
			case (int) ResponseType.Play:
				window.Visible = false;
				
				if (PlayEvent != null)
					PlayEvent (list.Selected);

				Reset ();

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
		
		private void OnEntryChanged (object o, EventArgs args)
		{
			if (process_changes_immediately) {
				Search ();

			} else {
				if (search_idle_id > 0)
					GLib.Source.Remove (search_idle_id);

				search_idle_id = GLib.Idle.Add (new GLib.IdleHandler (Search));
			}
		}
	}
}
