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
using System.Runtime.InteropServices;

using Gtk;
using GLib;

namespace Muine
{
	public class HandleView : TreeView
	{
		// Events
		public new delegate void RowActivatedHandler (IntPtr handle);
		public new event HandleView.RowActivatedHandler RowActivated;

		public delegate void SelectionChangedHandler ();
		public event SelectionChangedHandler SelectionChanged;

		public delegate void PlayingChangedHandler (IntPtr handle);
		public event PlayingChangedHandler PlayingChanged;

		// Delegates
		// Delegates :: Public
		public delegate void CellDataFunc (HandleView view, CellRenderer renderer, IntPtr handle);
		public delegate int  CompareFunc  (IntPtr a, IntPtr b);
		
		// Delegates :: Private
		private SignalUtils.SignalDelegatePtr pointer_activated_cb;
		private SignalUtils.SignalDelegatePtr selection_changed_cb;

		// Delegates :: Internal
		internal delegate void CellDataFuncNative (IntPtr view, IntPtr renderer, IntPtr handle);
		internal delegate int  CompareFuncNative  (IntPtr a, IntPtr b);

		// Constructor
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_view_new ();

		public HandleView () : base (IntPtr.Zero)
		{
			Raw = pointer_list_view_new ();

			pointer_activated_cb = new SignalUtils.SignalDelegatePtr (OnPointerActivated);
			selection_changed_cb = new SignalUtils.SignalDelegatePtr (OnSelectionChanged);

			SignalUtils.SignalConnect (Raw, "pointer_activated", pointer_activated_cb);
			SignalUtils.SignalConnect (Raw, "selection_changed", selection_changed_cb);
		}

		// Destructor
		~HandleView ()
		{
			Dispose ();
		}
		
		// Properties
		// Properties :: Model (get;)
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_view_get_model (IntPtr view);
		
		public new IntPtr Model {
			get { return pointer_list_view_get_model (Raw); }
		}
		
		// Properties :: SortFunc (set;)
		[DllImport("libmuine")]
		private static extern void pointer_list_model_set_sorting (IntPtr model, CompareFuncNative sort_func);

		public CompareFunc SortFunc {
			set {
				CompareFuncWrapper wrapper = new CompareFuncWrapper (value, this);
				pointer_list_model_set_sorting (Model, wrapper.NativeDelegate);
			}
		}

		// Properties :: Playing (set; get;)
		[DllImport("libmuine")]
		private static extern void pointer_list_model_set_current (IntPtr model, IntPtr pointer);

		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_model_get_current (IntPtr model);

		public IntPtr Playing {
			set {
				pointer_list_model_set_current (Model, value);

				if (PlayingChanged != null)
					PlayingChanged (value);
			}

			get { return pointer_list_model_get_current (Model); }
		}

		// Properties :: Contents (get;)
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_model_get_pointers (IntPtr model);

		public List Contents {
			get {
				List ret = new List (pointer_list_model_get_pointers (Model), typeof (int));
				ret.Managed = true;
				return ret;
			}
		}

		// Properties :: Length (get;)
		[DllImport("libmuine")]
		private static extern int pointer_list_view_get_length (IntPtr view);

		public int Length {
			get {
				return pointer_list_view_get_length (Raw);
			}
		}

		// Properties :: SelectedPointers (get;)
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_view_get_selection (IntPtr view);

		public List SelectedPointers {
			get {
				List ret = new List (pointer_list_view_get_selection (Raw), typeof (int));
				ret.Managed = true;
				return ret;
			}
		}


		// Methods
		// Methods :: Public
		// Methods :: Public :: AddColumn
		[DllImport("libmuine")]
		private static extern void pointer_list_view_add_column (IntPtr view, IntPtr renderer, CellDataFuncNative data_func, 
									 bool expand);
								
		public void AddColumn (CellRenderer renderer, CellDataFunc data_func, bool expand)
		{
			CellDataFuncWrapper wrapper = new CellDataFuncWrapper (data_func, this);
			pointer_list_view_add_column (Raw, renderer.Handle, wrapper.NativeDelegate, expand);
		}		

		// Methods :: Public :: Append
		[DllImport("libmuine")]
		private static extern void pointer_list_model_add (IntPtr model, IntPtr pointer);
								     
		public void Append (IntPtr handle)
		{
			pointer_list_model_add (Model, handle);
		}

		// Methods :: Public :: Insert
		[DllImport("libmuine")]
		private static extern void pointer_list_model_insert (IntPtr model, IntPtr pointer, IntPtr ins, uint pos);

		public void Insert (IntPtr handle, IntPtr ins, TreeViewDropPosition pos)
		{
			pointer_list_model_insert (Model, handle, ins, (uint) pos);
		}

		// Methods :: Public :: Contains
		[DllImport("libmuine")]
		private static extern bool pointer_list_model_contains (IntPtr model, IntPtr pointer);

		public bool Contains (IntPtr handle)
		{
			return pointer_list_model_contains (Model, handle);
		}

		// Methods :: Public :: Changed
		[DllImport("libmuine")]
		private static extern void pointer_list_view_changed (IntPtr view, IntPtr pointer);

		public void Changed (IntPtr handle)
		{
			pointer_list_view_changed (Raw, handle);
		}
		
		// Methods :: Public :: Remove
		[DllImport("libmuine")]
		private static extern void pointer_list_model_remove (IntPtr model, IntPtr pointer);

		public void Remove (IntPtr handle)
		{
			pointer_list_model_remove (Model, handle);
		}

		// Methods :: Public :: RemoveDelta
		[DllImport("libmuine")]
		private static extern void pointer_list_model_remove_delta (IntPtr model, IntPtr delta);

		public void RemoveDelta (List delta)
		{
			pointer_list_model_remove_delta (Model, delta.Handle);
		}

		// Methods :: Public :: Clear
		[DllImport("libmuine")]
		private static extern void pointer_list_model_clear (IntPtr model);
		
		public void Clear ()
		{
			bool playing_changed = (Playing != IntPtr.Zero);
			
			pointer_list_model_clear (Model);

			if (playing_changed && PlayingChanged != null)
				PlayingChanged (IntPtr.Zero);
		}

		// Methods :: Public :: GetHandleFromPath
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_get_handle_from_path (IntPtr view, IntPtr path);

		public IntPtr GetHandleFromPath (TreePath path) {
			return pointer_list_get_handle_from_path (Raw, path.Handle);
		}

		// Methods :: Public :: SelectFirst
		[DllImport("libmuine")]
		private static extern void pointer_list_view_select_first (IntPtr view);

		public void SelectFirst ()
		{
			pointer_list_view_select_first (Raw);
		}

		// Methods :: Public :: SelectPrevious
		[DllImport("libmuine")]
		private static extern bool pointer_list_view_select_prev (IntPtr view);

		public bool SelectPrevious ()
		{
			return pointer_list_view_select_prev (Raw);
		}

		// Methods :: Public :: SelectNext
		[DllImport("libmuine")]
		private static extern bool pointer_list_view_select_next (IntPtr view);

		public bool SelectNext ()
		{
			return pointer_list_view_select_next (Raw);
		}

		// Methods :: Public :: Select
		[DllImport("libmuine")]
		private static extern void pointer_list_view_select (IntPtr view, IntPtr handle, bool center);

		public void Select (IntPtr handle)
		{
			Select (handle, true);
		}

		public void Select (IntPtr handle, bool center)
		{
			pointer_list_view_select (Raw, handle, center);
		}

		// Methods :: Public :: Sort
		[DllImport("libmuine")]
		private static extern void pointer_list_model_sort (IntPtr model, CompareFuncNative sort_func);

		public void Sort (CompareFunc func)
		{
			CompareFuncWrapper wrapper = new CompareFuncWrapper (func, this);
	                pointer_list_model_sort (Model, wrapper.NativeDelegate);
		}

		// Methods :: Public :: HasFirst
		[DllImport("libmuine")]
		private static extern bool pointer_list_model_has_first (IntPtr model);

		public bool HasFirst {
			get { return pointer_list_model_has_first (Model); }
		}

		// Methods :: Public :: HasPrevious
		[DllImport("libmuine")]
		private static extern bool pointer_list_model_has_prev (IntPtr model);

		public bool HasPrevious {
			get { return pointer_list_model_has_prev (Model); }
		}

		// Methods :: Public :: HasNext
		[DllImport("libmuine")]
		private static extern bool pointer_list_model_has_next (IntPtr model);

		public bool HasNext {
			get { return pointer_list_model_has_next (Model); }
		}

		// Methods :: Public :: First
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_model_first (IntPtr model);

		public IntPtr First ()
		{
			IntPtr ret = pointer_list_model_first (Model);

			if (PlayingChanged != null)
				PlayingChanged (ret);

			return ret;
		}

		// Methods :: Public :: Last
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_model_last (IntPtr model);

		public IntPtr Last ()
		{
			IntPtr ret = pointer_list_model_last (Model);

			if (PlayingChanged != null)
				PlayingChanged (ret);

			return ret;
		}

		// Methods :: Public :: Previous
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_model_prev (IntPtr model);

		public IntPtr Previous ()
		{
			IntPtr ret = pointer_list_model_prev (Model);

			if (PlayingChanged != null)
				PlayingChanged (ret);

			return ret;
		}

		// Methods :: Public :: Next
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_model_next (IntPtr model);

		public IntPtr Next ()
		{
			IntPtr ret = pointer_list_model_next (Model);

			if (PlayingChanged != null)
				PlayingChanged (ret);

			return ret;
		}

		// Methods :: Public :: ForwardKeyPress
		// hack to forward key press events to the treeview
		public bool ForwardKeyPress (Widget orig_widget, Gdk.EventKey e)
		{
			bool go  = false;
			bool ret = false;
			
			Gdk.ModifierType mod = 0;

			if        ((e.State != 0) && ((e.State & Gdk.ModifierType.ControlMask) != 0)) {
			    	go = true;
				mod = Gdk.ModifierType.ControlMask;

			} else if ((e.State != 0) && ((e.State & Gdk.ModifierType.Mod1Mask   ) != 0)) {
				go = true;
				mod = Gdk.ModifierType.Mod1Mask;

			} else if ((e.State != 0) && ((e.State & Gdk.ModifierType.ShiftMask  ) != 0)) {
				go = true;
				mod = Gdk.ModifierType.ShiftMask;

			} else if (!KeyUtils.HaveModifier (e) && !KeyUtils.IsModifier (e)) {
				go = true;
				mod = 0;
			}

			if (go) {
				Gdk.GC saved_gc = Style.BaseGC (StateType.Selected);
				Style.SetBaseGC (StateType.Selected, Style.BaseGC (StateType.Active));

				GrabFocus ();

				ret = Gtk.Global.BindingsActivate (this, (uint) e.Key, mod);

				Style.SetBaseGC (StateType.Selected, saved_gc);

				orig_widget.GrabFocus ();
			}

			return ret;
		}

		// Handlers
		// Handlers :: OnPointerActivated
		private void OnPointerActivated (IntPtr obj, IntPtr ptr)
		{
			if (RowActivated != null)
				RowActivated (ptr);
		}

		// Handlers :: OnSelectionChanged
		private void OnSelectionChanged (IntPtr obj, IntPtr unused_data)
		{
			if (SelectionChanged != null)
				SelectionChanged ();
		}

		// Internal Classes
		// Internal Classes :: CellDataFuncWrapper
		internal class CellDataFuncWrapper : GLib.DelegateWrapper
		{
			protected CellDataFunc _managed;
			internal CellDataFuncNative NativeDelegate;

			public void NativeCallback (IntPtr view, IntPtr renderer, IntPtr handle)
			{
				HandleView v = (HandleView) GLib.Object.GetObject (view, false);
				CellRenderer r = (CellRenderer) GLib.Object.GetObject (renderer, false);
				
				_managed (v, r, handle);
			}

			public CellDataFuncWrapper (CellDataFunc managed, object o) : base (o)
			{
				NativeDelegate = new CellDataFuncNative (NativeCallback);
				_managed = managed;
			}
		}
		
		// InternalClasses :: CompareFuncWrapper
		internal class CompareFuncWrapper : GLib.DelegateWrapper
		{
			protected CompareFunc _managed;
			internal CompareFuncNative NativeDelegate;

			public int NativeCallback (IntPtr a, IntPtr b)
			{
				return (int) _managed (a, b);
			}

			public CompareFuncWrapper (CompareFunc managed, object o) : base (o)
			{
				NativeDelegate = new CompareFuncNative (NativeCallback);
				_managed = managed;
			}
		}
	}
}
