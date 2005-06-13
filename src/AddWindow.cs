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

		// Constants
		private const uint search_timeout = 100;

		// Objects
		private ICollection items;

		// Variables
		private string gconf_key_width, gconf_key_height;
		private int gconf_default_width, gconf_default_height;
		
		private uint search_timeout_id = 0;
		private bool first_time = true;
		private bool ignore_change = false;

		// Constructor
		/// <summary>
		///	Create a new <see cref="AddWindow" />.
		/// </summary>
		/// <remarks>
		///	This is used as a base class for 
		///	<see cref="AddAlbumWindow" /> and
		///	<see cref="AddSongWindow" />.
		/// </remarks>
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
			list.RowActivated      += new RowActivatedHandler (OnRowActivated);
			list.Selection.Changed += new EventHandler        (OnSelectionChanged);
			scrolledwindow.Add (list);

			entry.Show ();
			list.Show ();

			// And realize, needed for the cursor changing later on
			window.Realize ();
		}

		// Properties
		// Properties :: List (get;)
		/// <summary>
		/// 	The associated <see cref="AddWindowList" />.
		/// </summary>
		/// <returns>
		///	A <see cref="AddWindowList" />.
		/// </returns>
		public AddWindowList List {
			get { return list; }
		}

		// Properties :: Entry (get;)
		/// <summary>
		/// 	The associated <see cref="AddWindowEntry" />.
		/// </summary>		
		/// <returns>
		///	A <see cref="AddWindowEntry" />.
		/// </returns>
		public AddWindowEntry Entry {
			get { return entry; }
		}
		
		// Properties :: TextRenderer (get;)
		/// <summary>
		/// 	The associated <see cref="CellRenderer" />.
		/// </summary>
		/// <returns>
		///	A <see cref="CellRenderer" />.
		/// </returns>
		public CellRenderer TextRenderer {
			get { return text_renderer; }
		}

		// Properties :: Items (set; get;)
		/// <summary>
		/// 	A collection of the items in the list.
		/// </summary>
		/// <param name="value">
		///	An <see cref="ICollection" />.
		/// </param>
		/// <returns>
		///	An <see cref="ICollection" />.
		/// </returns>
		public ICollection Items {
			set { items = value; }
			get { return items;  }
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: Run
		/// <summary>
		/// 	Show the window.
		/// </summary>		
		public void Run (uint time)
		{
                        bool was_visible = Visible;
                        
			if (first_time || entry.Text.Length > 0) {
				window.GdkWindow.Cursor = new Gdk.Cursor (Gdk.CursorType.Watch);
				window.GdkWindow.Display.Flush ();

				ignore_change = true;
				entry.Text = "";
				ignore_change = false;

				GLib.Idle.Add (new GLib.IdleHandler (Reset));

				first_time = false;

			} else {
				list.SelectFirst ();
			}

			entry.GrabFocus ();

			window.Show ();

                        if (was_visible)
                                window.GdkWindow.Focus (time);
		}
			
		// Methods :: Protected
		// Methods :: Protected :: SetGConfSize
		/// <summary>
		/// 	Set the default window size according to GConf.
		/// </summary>
		/// <param name="key_width">
		///	The GConf key where the default window width is stored.
		/// </param>
		/// <param name="default_width">
		///	The width to be used if the GConf value cannot be
		///	found or is invalid.
		/// </param>
		/// <param name="key_height">
		///	The GConf key where the default window height is stored.
		/// </param>
		/// <param name="default_height">
		///	The height to be used if the GConf value cannot be
		///	found or is invalid.
		/// </param>
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

		// Methods :: Protected :: Search
		//	TODO: Make private; make void (not called from a GLib loop).
		/// <summary>
		/// 	Execute a search according to the terms currently in the entry box.
		/// </summary>
		/// <returns>
		///	False.
		/// </returns>
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

			list.Model.RemoveDelta (l);

			foreach (int p in l) {
				IntPtr ptr = new IntPtr (p);
				list.Model.Append (ptr);
			}

			list.SelectFirst ();

			return false;
		}

		// Methods :: Private
		// Methods :: Private :: Assertions
		// Methods :: Private :: Assertions :: HasGConfSize
		/// <summary>
		/// 	Returns whether or not GConf contains a valid window size.
		/// </summary>
		/// <returns>
		/// 	True if GConf keys exist for both width and height and
		///	are > 0, otherwise returns false.
		/// </returns>
		private bool HasGConfSize ()
		{
			return (gconf_key_width  != String.Empty && gconf_default_width  > 0 &&
				gconf_key_height != String.Empty && gconf_default_height > 0);
		}

		// Methods :: Private :: Assertions :: AssertHasGConfSize
		/// <summary>
		/// 	If GConf does not contain a valid size, then throws an
		///	exception.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// 	Thrown if GConf does not contain a valid size.
		/// </exception>
		private void AssertHasGConfSize ()
		{
			if (!HasGConfSize ())
				throw new InvalidOperationException ();		
		}

		
		// Methods :: Private :: Assertions :: HasItems
		/// <summary>
		/// 	Returns whether or not the list is empty.
		/// </summary>
		/// <returns>
		/// 	True if the list has items, False otherwise.
		/// </returns>
		private bool HasItems ()
		{
			return (items != null);
		}

		// Methods :: Private :: Assertions :: AssertHasItems
		/// <summary>
		/// 	Throws an exception if the list is empty.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// 	Thrown if the list is empty.
		/// </exception>
		private void AssertHasItems ()
		{
			if (!HasItems ())
				throw new InvalidOperationException ();
		}

		// Methods :: Private :: AddOnSizeAllocated
		/// <summary>
		/// 	Adds the <see cref="OnSizeAllocated">OnSizeAllocated</see>
		///	handler.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// 	Thrown if GConf does not contain a valid size.
		/// </exception>
		private void AddOnSizeAllocated ()
		{
			AssertHasGConfSize ();
			
			window.SizeAllocated += new SizeAllocatedHandler (OnSizeAllocated);		
		}
		
		// Methods :: Private :: RestoreCursor
		/// <summary>
		/// 	Reset the cursor to be normal.
		/// </summary>
		/// <returns>
		///	False, as we only want to run once.
		/// </returns>
		private bool RestoreCursor ()
		{
			window.GdkWindow.Cursor = null;

			return false;
		}

		// Delegate Functions
		// Delegate Functions :: Reset
		/// <summary>
		/// 	Delegate function used to display the new results.
		/// </summary>
		/// <returns>
		/// 	False, as we only want to run once.
		/// </return>
		private bool Reset ()
		{
			Search ();

			// We want to get the normal cursor back *after* treeview
			// has done its thing.
			GLib.Idle.Add (new GLib.IdleHandler (RestoreCursor));

			return false;
		}

		// Handlers
		// Handlers :: OnWindowDeleteEvent
		//	TODO: Why not just hide the window here?
		/// <summary>
		/// 	Handler called when the window is closed.
		/// </summary>
		/// <remarks>
		///	This refuses to let the window close because that is
		///	handled by <see cref="OnWindowResponse" /> so it can be
		///	hidden instead.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="DeleteEventArgs" />.
		/// </param>
		private void OnWindowDeleteEvent (object o, DeleteEventArgs args)
		{
			args.RetVal = true;
		}

		// Handlers :: OnRowActivated
		/// <summary>
		/// 	Handler called when the a row is activated (such as with
		///	a double-click).
		/// </summary>
		/// <remarks>
		///	Activating a row is the same as clicking the Play button.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="RowActivatedArgs" />.
		/// </param>
		private void OnRowActivated (object o, RowActivatedArgs args)
		{
			play_button.Click ();
		}

		// Handlers :: OnSelectionChanged
		/// <summary>
		/// 	Handler called when the selection is changed.
		/// </summary>
		/// <remarks>
		/// 	If no selection is present, the Play and Queue buttons
		///	are disabled. Otherwise, they are enabled.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnSelectionChanged (object o, EventArgs args)
		{
			play_button.Sensitive  = list.HasSelection;
			queue_button.Sensitive = list.HasSelection;
		}
		
		// Handlers :: OnEntryKeyPressEvent
		/// <summary>
		/// 	Handler called when a key is pressed.
		/// </summary>
		/// <remarks>
		/// 	Forwards the value of the key on to the 
		///	<see cref="HandleView" />.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="KeyPressEventArgs" />.
		/// </param>
		/// <seealso cref="HandleView.ForwardKeyPress" />
		private void OnEntryKeyPressEvent (object o, KeyPressEventArgs args)
		{
			args.RetVal = list.ForwardKeyPress (entry, args.Event);
		}

		// Handlers :: OnSizeAllocated
		/// <summary>
		/// 	Handler called when the window is resized.
		/// </summary>
		/// <remarks>
		/// 	Sets the new size in GConf if keys are present.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="SizeAllocatedArgs" />.
		/// </param>
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
		/// <summary>
		/// 	Handler called when a response has been chosen 
		/// </summary>
		/// <remarks>
		///	If the window is closed or the Close button is clicked,
		///	the window simply hides. If the Play button is clicked,
		///	the window hides and the selected item starts playing. 
		///	If the Queue button is clicked, the selected item is 
		///	added to the queue but the window is not hidden and the
		///	item does not start playing.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="ResponseArgs" />.
		/// </param>
		/// <exception cref="ArgumentException">
		///	Thrown if the response is not window deleted, close, play,
		///	or queue. Really only possible if we add another button
		///	to the window but forget to add it here.
		/// </exception>
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
		/// <summary>
		/// 	Handler called when the entry has been changed.
		/// </summary>
		/// <remarks>
		/// 	Calls <see cref="Search" /> unless
		///	changes are currently being ignored.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnEntryChanged (object o, EventArgs args)
		{
			if (ignore_change)
				return;

			if (search_timeout_id > 0)
				GLib.Source.Remove (search_timeout_id);

			search_timeout_id = GLib.Timeout.Add (search_timeout, 
				new GLib.TimeoutHandler (Search));
		}
		
		// Handlers :: OnAdded
		// 	FIXME, UNUSED: Requires Mono 1.1+
		/// <summary>
		/// 	Handler called when an <see cref="Item" /> is added.
		/// </summary>
		/// <param name="item">
		///	The <see cref="Item" /> which has been added.
		/// </param>
		protected void OnAdded (Item item)
		{
			list.HandleAdded (item.Handle, item.FitsCriteria (entry.SearchBits));
		}

		// Handlers :: OnChanged
		// 	FIXME, UNUSED: Requires Mono 1.1+
		/// <summary>
		/// 	Handler called when an <see cref="Item" /> is changed.
		/// </summary>
		/// <param name="item">
		///	The <see cref="Item" /> which has been changed.
		/// </param>
		protected void OnChanged (Item item)
		{
			list.HandleChanged (item.Handle, item.FitsCriteria (entry.SearchBits));
		}

		// Handlers :: OnRemoved
		// 	FIXME, UNUSED: Requires Mono 1.1+
		/// <summary>
		/// 	Handler called when an <see cref="Item" /> is removed.
		/// </summary>
		/// <param name="item">
		///	The <see cref="Item" /> which has been removed.
		/// </param>
		protected void OnRemoved (Item item)
		{
			list.HandleRemoved (item.Handle);
		}
	}
}
