/*
 * Copyright (C) 2004 Ross Girshick <ross.girshick@gmail.com>
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

using GLib;
using Gtk;
using System;

namespace Muine
{
	class VolumeButton : ToggleButton
	{
		private Image icon;
		private Window popup;
		private int volume;
		private int revert_volume;

		/* GDK_CURRENT_TIME doesn't seem to have an equiv in gtk-sharp yet. */
		const uint CURRENT_TIME = 0;

		public int Volume {
			set {
				string id = "muine-volume-";
				
				volume = value;

				if (volume <= 0)
					id += "zero";
				else if (volume <= 100 / 3)
					id += "min";
				else if (volume <= 200 / 3)
					id += "medium";
				else
					id += "max";

				icon.SetFromStock (id, IconSize.LargeToolbar);

				VolumeChanged (Volume);
			}

			get { return volume; }
		}

		public delegate void VolumeChangedHandler (int vol);
		public event VolumeChangedHandler VolumeChanged;

		public VolumeButton () : base ()
		{
			icon = new Image ();
			icon.Show ();
			Add (icon);

			popup = null;

			ScrollEvent += new ScrollEventHandler (OnScrollEvent);
			Toggled += new EventHandler (OnToggled);
			
			Flags |= (int) WidgetFlags.NoWindow;
		}

		~VolumeButton ()
		{
			Dispose ();
		}

		private void ShowScale ()
		{
			VScale scale;
			VBox box;
			Adjustment adj;
			Frame frame;
			Label label;
			Image vol_icon_top;
			Image vol_icon_bottom;
			Requisition req;
			int x, y;
			
			/* Check point the volume so that we can restore it if the user rejects the slider changes. */
			revert_volume = Volume;

			popup = new Window (WindowType.Popup);
			popup.Screen = this.Screen;

			frame = new Frame ();
			frame.Shadow = ShadowType.Out;
			frame.Show ();

			popup.Add (frame);

			box = new VBox (false, 0);
			box.Show();

			frame.Add (box);

			adj = new Adjustment (volume, 0, 100, 5, 10, 0);		

			scale = new VScale (adj);
			scale.ValueChanged += new EventHandler (OnScaleValueChanged);
			scale.KeyPressEvent += new KeyPressEventHandler (OnScaleKeyPressEvent);
			popup.ButtonPressEvent += new ButtonPressEventHandler (OnPopupButtonPressEvent);

			scale.Adjustment.Upper = 100.0;
			scale.Adjustment.Lower = 0.0;
			scale.DrawValue = false;
			scale.UpdatePolicy = UpdateType.Continuous;
			scale.Inverted = true;

			scale.Show ();

			label = new Label ("+");
			label.Show ();
			box.PackStart (label, false, true, 0);

			label = new Label ("-");
			label.Show ();
			box.PackEnd (label, false, true, 0);

			box.PackStart (scale, true, true, 0);

			req = SizeRequest ();

			GdkWindow.GetOrigin (out x, out y);

			scale.SetSizeRequest (-1, 100);
			popup.SetSizeRequest (req.Width, -1);

			popup.Move (x + Allocation.X, y + Allocation.Y + req.Height);
			popup.Show ();

			popup.GrabFocus ();

			Grab.Add (popup);

			Gdk.GrabStatus grabbed = Gdk.Pointer.Grab (popup.GdkWindow, true, 
								   Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask, 
								   null, null, 
								   CURRENT_TIME);

			if (grabbed == Gdk.GrabStatus.Success) {
				grabbed = Gdk.Keyboard.Grab (popup.GdkWindow, true, CURRENT_TIME);

				if (grabbed != Gdk.GrabStatus.Success) {
					Grab.Remove (popup);
					popup.Destroy ();
					popup = null;
				}
			} else {
				Grab.Remove (popup);
				popup.Destroy ();
				popup = null;
			}
		}

		private void HideScale ()
		{
			if (popup != null) {
				Grab.Remove (popup);
				Gdk.Pointer.Ungrab (CURRENT_TIME);
				Gdk.Keyboard.Ungrab (CURRENT_TIME);

				popup.Destroy ();
				popup = null;
			}

			Active = false;
		}

		private void OnToggled (object obj, EventArgs args)
		{
			if (Active)
				ShowScale ();
			else
				HideScale ();
		}

		private void OnScrollEvent (object obj, ScrollEventArgs args)
		{
			int tmp_vol = Volume;
			
			switch (args.Event.Direction) {
			case Gdk.ScrollDirection.Up:
				tmp_vol += 10;
				break;
			case Gdk.ScrollDirection.Down:
				tmp_vol -= 10;
				break;
			default:
				break;
			}

			// A CLAMP equiv doesn't seem to exist ... doing that manually
			tmp_vol = Math.Min (100, tmp_vol);
			tmp_vol = Math.Max (0, tmp_vol);

			Volume = tmp_vol;
		}

		private void OnScaleValueChanged (object obj, EventArgs args)
		{
			Volume = (int)((VScale)obj).Value;
		}

		private void OnScaleKeyPressEvent (object obj, KeyPressEventArgs args)
		{
			switch (args.Event.Key) {
			case Gdk.Key.Escape:
				HideScale ();
				Volume = revert_volume;
				break;
			case Gdk.Key.KP_Enter:
			case Gdk.Key.ISO_Enter:
			case Gdk.Key.Key_3270_Enter:
			case Gdk.Key.Return:
			case Gdk.Key.space:
			case Gdk.Key.KP_Space:
				HideScale ();
				break;
			default:
				break;
			}
		}

		private void OnPopupButtonPressEvent (object obj, ButtonPressEventArgs args)
		{
			if (popup != null)
				HideScale ();
		}
	}
}
