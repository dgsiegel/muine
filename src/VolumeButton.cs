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
	public class VolumeButton : ToggleButton
	{
		// Constants
		// 	GDK_CURRENT_TIME doesn't seem to have an equiv in gtk# yet.
		private const uint CURRENT_TIME = 0;

		// Events
		public delegate void VolumeChangedHandler (int vol);
		public event         VolumeChangedHandler VolumeChanged;
				
		// Widgets
		private Image icon;
		private Window popup;
		
		// Variables
		private int volume;

		// Variables :: revert_volume
		// 	Volume to restore to if the user rejects the slider changes.
		private int revert_volume;

		// Constructor
		public VolumeButton () : base ()
		{
			icon = new Image ();
			icon.Show ();
			Add (icon);

			popup = null;

			base.ScrollEvent += new ScrollEventHandler (OnScrollEvent);
			base.Toggled     += new EventHandler       (OnToggled    );
			
			base.Flags |= (int) WidgetFlags.NoWindow;
		}

		// Destructor
		~VolumeButton ()
		{
			Dispose ();
		}

		// Properties
		// Properties :: Volume (set; get;)
		public int Volume {
			set {
				volume = value;

				string id = "muine-volume-";
						
				id += (volume <= 0)
				      ? "zero"
				      :
				      (volume <= 100 / 3)
				      ? "min"
				      :
				      (volume <= 200 / 3)
				      ? "medium"
				      : "max";

				icon.SetFromStock (id, IconSize.LargeToolbar);

				VolumeChanged (Volume);
			}

			get { return volume; }
		}

		// Methods
		// Methods :: Private
		// Methods :: Private :: ShowScale
		private void ShowScale ()
		{
			revert_volume = this.Volume;

			popup = new Window (WindowType.Popup);
			popup.Screen = base.Screen;

			Frame frame = new Frame ();
			frame.Shadow = ShadowType.Out;
			frame.Show ();

			popup.Add (frame);

			VBox box = new VBox (false, 0);
			box.Show();

			frame.Add (box);

			Adjustment adj = new Adjustment (volume, 0, 100, 5, 10, 0);		

			VScale scale = new VScale (adj);
			scale.ValueChanged += new EventHandler (OnScaleValueChanged);
			scale.KeyPressEvent += new KeyPressEventHandler (OnScaleKeyPressEvent);
			popup.ButtonPressEvent += new ButtonPressEventHandler (OnPopupButtonPressEvent);

			scale.Adjustment.Upper = 100.0;
			scale.Adjustment.Lower = 0.0;
			scale.DrawValue = false;
			scale.UpdatePolicy = UpdateType.Continuous;
			scale.Inverted = true;

			scale.Show ();

			Label label = new Label ("+");
			label.Show ();
			box.PackStart (label, false, true, 0);

			label = new Label ("-");
			label.Show ();
			box.PackEnd (label, false, true, 0);

			box.PackStart (scale, true, true, 0);

			Requisition req = SizeRequest ();

			int x, y;
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

		// Methods :: Private :: HideScale
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

		// Handlers
		// Handlers :: OnToggled
		private void OnToggled (object obj, EventArgs args)
		{
			if (Active)
				ShowScale ();
			else
				HideScale ();
		}

		// Handlers :: OnScrollEvent
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

			// Assure volume is between 0 and 100
			tmp_vol = Math.Min (100, tmp_vol);
			tmp_vol = Math.Max (  0, tmp_vol);

			this.Volume = tmp_vol;
		}

		// Handlers :: OnScaleValueChanged
		private void OnScaleValueChanged (object obj, EventArgs args)
		{
			VScale scale = (VScale) obj;
			this.Volume = (int) scale.Value;
		}

		// Handlers :: OnScaleKeyPressEvent
		private void OnScaleKeyPressEvent (object obj, KeyPressEventArgs args)
		{
			switch (args.Event.Key) {
			case Gdk.Key.Escape:
				HideScale ();
				this.Volume = revert_volume;
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

		// Handlers :: OnPopupButtonPressEvent
		private void OnPopupButtonPressEvent (object obj, ButtonPressEventArgs args)
		{
			if (popup != null)
				HideScale ();
		}
	}
}
