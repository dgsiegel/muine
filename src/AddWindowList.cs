/*
 * Copyright (C) 2005 Tamara Roberson <foxxygirltamara@gmail.com>
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

namespace Muine
{
	public class AddWindowList : HandleView
	{
		public AddWindowList () : base ()
		{
			base.Selection.Mode = Gtk.SelectionMode.Multiple;

		}
		
		public bool HasSelection {
			get { return (base.SelectedPointers.Count > 0); }
		}
		
		public Gtk.TargetEntry [] DragSource {
			set {
				base.EnableModelDragSource (Gdk.ModifierType.Button1Mask, 
							    value, Gdk.DragAction.Copy);
			}
		}

		public GLib.List Selected {
			get { return base.SelectedPointers; }
		}

		protected new void SelectFirst ()
		{
			base.ScrollToCell (new Gtk.TreePath ("0"), null, true, 0f, 0f);
			base.SelectFirst ();
		}

		protected void SelectFirstIfNeeded ()
		{
			if (!this.HasSelection)
				SelectFirst ();
		}

		public void HandleAdded (IntPtr ptr, bool fits)
		{
			if (fits)
				base.Append (ptr);
		}

		public void HandleChanged (IntPtr ptr, bool fits)
		{
			HandleChanged (ptr, fits, true);
		}

		public void HandleChanged (IntPtr ptr, bool fits, bool may_append)
		{
			if (fits) {
				if (base.Contains (ptr))
					base.Changed (ptr);
				else if (may_append)
					base.Append (ptr);
			} else {
				base.Remove (ptr);
			}

			SelectFirstIfNeeded ();	
		}
			
		public void HandleRemoved (IntPtr ptr)
		{
			base.Remove (ptr);
			SelectFirstIfNeeded ();	
		}

	}
}