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
	public class AddWindowEntry : Gtk.Entry
	{
		// Variables
		private static readonly int min_query_length = 3;

		private string [] search_bits;

		// Constructor
		public AddWindowEntry () : base ()
		{
			ActivatesDefault = true;

			Changed += new EventHandler (OnChanged);
		}

		// Properties
		// Properties :: SearchBits (get;)
		public string [] SearchBits {
			get { return search_bits; }
		}

		// Properties :: MinQueryLength (get;)
		public int MinQueryLength {
			get { return min_query_length;  }
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: Clear
		public void Clear ()
		{
			base.Text = "";
		}

		// Handlers
		// Handlers :: OnChanged
		private void OnChanged (object o, EventArgs args)
		{
			search_bits = base.Text.ToLower ().Split (' ');
		}
	}
}
