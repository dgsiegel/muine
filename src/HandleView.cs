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

public class HandleView : TreeView
{
	[DllImport ("libmuine")]
	private static extern IntPtr pointer_list_view_new ();

	private SignalUtils.SignalDelegatePtr pointer_activated_cb;
	private SignalUtils.SignalDelegatePtr selection_changed_cb;

	public HandleView () : base (IntPtr.Zero)
	{
		Raw = pointer_list_view_new ();

		pointer_activated_cb = new SignalUtils.SignalDelegatePtr (OnPointerActivated);
		selection_changed_cb = new SignalUtils.SignalDelegatePtr (OnSelectionChanged);

		SignalUtils.SignalConnect (Raw, "pointer_activated", pointer_activated_cb);
		SignalUtils.SignalConnect (Raw, "selection_changed", selection_changed_cb);
	}

	~HandleView ()
	{
		Dispose ();
	}

	[DllImport ("libmuine")]
	private static extern void pointer_list_view_add_column (IntPtr view,
								 IntPtr renderer,
								 CellDataFuncNative data_func,
								 bool expand);
							
	public delegate void CellDataFunc (HandleView view, CellRenderer renderer, IntPtr handle);

	internal delegate void CellDataFuncNative (IntPtr view, IntPtr renderer, IntPtr handle);

	internal class CellDataFuncWrapper : GLib.DelegateWrapper
	{
		public void NativeCallback (IntPtr view, IntPtr renderer, IntPtr handle)
		{
			HandleView v = (HandleView) GLib.Object.GetObject (view, false);
			CellRenderer r = (CellRenderer) GLib.Object.GetObject (renderer, false);
			
			_managed (v, r, handle);
		}

		internal CellDataFuncNative NativeDelegate;
		protected CellDataFunc _managed;

		public CellDataFuncWrapper (CellDataFunc managed, object o) : base (o)
		{
			NativeDelegate = new CellDataFuncNative (NativeCallback);
			_managed = managed;
		}
	}
	
	public void AddColumn (CellRenderer renderer, CellDataFunc data_func, bool expand)
	{
		CellDataFuncWrapper wrapper = new CellDataFuncWrapper (data_func, this);
		pointer_list_view_add_column (Raw, renderer.Handle, wrapper.NativeDelegate, expand);
	}		

	[DllImport ("libmuine")]
	private static extern void pointer_list_view_append (IntPtr view,
	                                                     IntPtr pointer);
							     
	public void Append (IntPtr handle)
	{
		pointer_list_view_append (Raw, handle);
	}

	[DllImport ("libmuine")]
	private static extern void pointer_list_view_insert (IntPtr view,
							     IntPtr pointer,
	                                                     IntPtr ins,
							     uint pos);

	public void Insert (IntPtr handle, IntPtr ins, TreeViewDropPosition pos)
	{
		pointer_list_view_insert (Raw, handle, ins, (uint) pos);
	}

	[DllImport ("libmuine")]
	private static extern bool pointer_list_view_contains (IntPtr view,
							       IntPtr pointer);

	public bool Contains (IntPtr handle)
	{
		return pointer_list_view_contains (Raw, handle);
	}

	[DllImport ("libmuine")]
	private static extern void pointer_list_view_changed (IntPtr view,
							      IntPtr pointer);

	public void Changed (IntPtr handle)
	{
		pointer_list_view_changed (Raw, handle);
	}
	
	[DllImport ("libmuine")]
	private static extern void pointer_list_view_remove (IntPtr view,
							     IntPtr pointer);

	public void Remove (IntPtr handle)
	{
		pointer_list_view_remove (Raw, handle);
	}

	[DllImport ("libmuine")]
	private static extern void pointer_list_view_remove_delta (IntPtr view,
								   IntPtr delta);

	public void RemoveDelta (List delta)
	{
		pointer_list_view_remove_delta (Raw, delta.Handle);
	}

	[DllImport ("libmuine")]
	private static extern void pointer_list_view_clear (IntPtr view);
	
	public void Clear ()
	{
		bool playing_changed = (Playing != IntPtr.Zero);
		
		pointer_list_view_clear (Raw);

		if (playing_changed && PlayingChanged != null)
			PlayingChanged (IntPtr.Zero);
	}

	[DllImport ("libmuine")]
	private static extern IntPtr pointer_list_view_get_contents (IntPtr view);

	public List Contents {
		get {
			List ret = new List (pointer_list_view_get_contents (Raw), typeof (int));
			ret.Managed = true;
			return ret;
		}
	}

	[DllImport ("libmuine")]
	private static extern IntPtr pointer_list_get_handle_from_path (IntPtr view,
			                                                IntPtr path);

	public IntPtr GetHandleFromPath (TreePath path) {
		return pointer_list_get_handle_from_path (Raw, path.Handle);
	}

	[DllImport ("libmuine")]
	private static extern int pointer_list_view_get_length (IntPtr view);

	public int Length {
		get {
			return pointer_list_view_get_length (Raw);
		}
	}

	[DllImport ("libmuine")]
	private static extern IntPtr pointer_list_view_get_selection (IntPtr view);

	public List SelectedPointers {
		get {
			List ret = new List (pointer_list_view_get_selection (Raw), typeof (int));
			ret.Managed = true;
			return ret;
		}
	}

	[DllImport ("libmuine")]
	private static extern void pointer_list_view_select_first (IntPtr view);

	public void SelectFirst ()
	{
		pointer_list_view_select_first (Raw);
	}

	[DllImport ("libmuine")]
	private static extern bool pointer_list_view_select_prev (IntPtr view);

	public bool SelectPrevious ()
	{
		return pointer_list_view_select_prev (Raw);
	}

	[DllImport ("libmuine")]
	private static extern bool pointer_list_view_select_next (IntPtr view);

	public bool SelectNext ()
	{
		return pointer_list_view_select_next (Raw);
	}

	[DllImport ("libmuine")]
	private static extern void pointer_list_view_select (IntPtr view, 
						             IntPtr handle,
							     bool center);

	public void Select (IntPtr handle, bool center)
	{
		pointer_list_view_select (Raw, handle, center);
	}

	public void Select (IntPtr handle)
	{
		Select (handle, true);
	}

	[DllImport ("libmuine")]
	private static extern void pointer_list_view_set_sort_func (IntPtr view,
	                                                            CompareFuncNative sort_func);

	public delegate int CompareFunc (IntPtr a, IntPtr b);

	internal delegate int CompareFuncNative (IntPtr a, IntPtr b);

	internal class CompareFuncWrapper : GLib.DelegateWrapper
	{
		public int NativeCallback (IntPtr a, IntPtr b)
		{
			return (int) _managed (a, b);
		}

		internal CompareFuncNative NativeDelegate;
		protected CompareFunc _managed;

		public CompareFuncWrapper (CompareFunc managed, object o) : base (o)
		{
			NativeDelegate = new CompareFuncNative (NativeCallback);
			_managed = managed;
		}
	}
	
	public CompareFunc SortFunc {
		set {
			CompareFuncWrapper wrapper = new CompareFuncWrapper (value, this);
			pointer_list_view_set_sort_func (Raw, wrapper.NativeDelegate);
		}
	}

	[DllImport ("libmuine")]
	private static extern void pointer_list_view_sort (IntPtr view,
	                                                   CompareFuncNative sort_func);

	public void Sort (CompareFunc func)
	{
		CompareFuncWrapper wrapper = new CompareFuncWrapper (func, this);
                pointer_list_view_sort (Raw, wrapper.NativeDelegate);
	}

	[DllImport ("libmuine")]
	private static extern IntPtr pointer_list_view_get_playing (IntPtr view);
	[DllImport ("libmuine")]
	private static extern void pointer_list_view_set_playing (IntPtr view,
								  IntPtr pointer);

	public IntPtr Playing {
		get {
			return pointer_list_view_get_playing (Raw);
		}

		set {
			pointer_list_view_set_playing (Raw, value);

			if (PlayingChanged != null)
				PlayingChanged (value);
		}
	}

	[DllImport ("libmuine")]
	private static extern bool pointer_list_view_has_first (IntPtr view);

	public bool HasFirst {
		get {
			return pointer_list_view_has_first (Raw);
		}
	}

	[DllImport ("libmuine")]
	private static extern bool pointer_list_view_has_prev (IntPtr view);

	public bool HasPrevious {
		get {
			return pointer_list_view_has_prev (Raw);
		}
	}

	[DllImport ("libmuine")]
	private static extern bool pointer_list_view_has_next (IntPtr view);

	public bool HasNext {
		get {
			return pointer_list_view_has_next (Raw);
		}
	}

	[DllImport ("libmuine")]
	private static extern IntPtr pointer_list_view_first (IntPtr view);

	public IntPtr First ()
	{
		IntPtr ret = pointer_list_view_first (Raw);

		if (PlayingChanged != null)
			PlayingChanged (ret);

		return ret;
	}

	[DllImport ("libmuine")]
	private static extern IntPtr pointer_list_view_last (IntPtr view);

	public IntPtr Last ()
	{
		IntPtr ret = pointer_list_view_last (Raw);

		if (PlayingChanged != null)
			PlayingChanged (ret);

		return ret;
	}

	[DllImport ("libmuine")]
	private static extern IntPtr pointer_list_view_prev (IntPtr view);

	public IntPtr Previous ()
	{
		IntPtr ret = pointer_list_view_prev (Raw);

		if (PlayingChanged != null)
			PlayingChanged (ret);

		return ret;
	}

	[DllImport ("libmuine")]
	private static extern IntPtr pointer_list_view_next (IntPtr view);

	public IntPtr Next ()
	{
		IntPtr ret = pointer_list_view_next (Raw);

		if (PlayingChanged != null)
			PlayingChanged (ret);

		return ret;
	}

	public bool ForwardKeyPress (Widget orig_widget,
	                             Gdk.EventKey e)
	{
		bool go = false;
		bool ret = false;
		
		Gdk.ModifierType mod = 0;

		if ((e.State != 0) &&
		    ((e.State & Gdk.ModifierType.ControlMask) != 0)) {
		    	go = true;

			mod = Gdk.ModifierType.ControlMask;
		} else if ((e.State != 0) &&
		           ((e.State & Gdk.ModifierType.Mod1Mask) != 0)) {
			go = true;

			mod = Gdk.ModifierType.Mod1Mask;
		} else if ((e.State != 0) &&
		           ((e.State & Gdk.ModifierType.ShiftMask) != 0)) {
			go = true;

			mod = Gdk.ModifierType.ShiftMask;
		} else if (!KeyUtils.HaveModifier (e) &&
		           !KeyUtils.IsModifier (e)) {
			go = true;
			
			mod = 0;
		}

		if (go) {
			/* hack to forward key press events to the treeview */
			Gdk.GC saved_gc = Style.BaseGC (StateType.Selected);
			Style.SetBaseGC (StateType.Selected, Style.BaseGC (StateType.Active));

			GrabFocus ();

			ret = Global.BindingsActivate (this, (uint) e.Key, mod);

			Style.SetBaseGC (StateType.Selected, saved_gc);

			orig_widget.GrabFocus ();
		}

		return ret;
	}

	private void OnPointerActivated (IntPtr obj, IntPtr ptr)
	{
		if (RowActivated != null)
			RowActivated (ptr);
	}

	public new delegate void RowActivatedHandler (IntPtr handle);
	public new event HandleView.RowActivatedHandler RowActivated;

	private void OnSelectionChanged (IntPtr obj, IntPtr unused_data)
	{
		if (SelectionChanged != null)
			SelectionChanged ();
	}

	public delegate void SelectionChangedHandler ();
	public event SelectionChangedHandler SelectionChanged;

	public delegate void PlayingChangedHandler (IntPtr handle);
	public event PlayingChangedHandler PlayingChanged;
}
