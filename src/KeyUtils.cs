/*
 * Copyright © 2004 Jorn Baayen <jorn@nl.linux.org>
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

public class KeyUtils
{
	/* If we have modifiers, and either Ctrl, Mod1 (Alt), or any
	 * of Mod3 to Mod5 (Mod2 is num-lock...) are pressed, we
	 * let Gtk+ handle the key */

	public static bool HaveModifier (Gdk.EventKey e) {
		if (e.state != 0
		 	&& (((e.state & (uint) Gdk.ModifierType.ControlMask) != 0)
			 || ((e.state & (uint) Gdk.ModifierType.Mod1Mask) != 0)
			 || ((e.state & (uint) Gdk.ModifierType.Mod3Mask) != 0)
			 || ((e.state & (uint) Gdk.ModifierType.Mod4Mask) != 0)
			 || ((e.state & (uint) Gdk.ModifierType.Mod5Mask) != 0))) {
			return true;
		}

		return false;
	}
}
