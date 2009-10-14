//  
//  Copyright (C) 2009 GNOME Do
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Gdk;
using Cairo;

namespace Cairo
{
	public class CairoExtension
	{
		public static Pixbuf RoundOff (Gdk.Pixbuf input)
		{
			ImageSurface surface = new ImageSurface (Format.Argb32, input.Width, input.Height);
			Cairo.Context cr = new Cairo.Context (surface);

			double x = 1;
			double y = 1;
			double width = input.Width - 2;
			double height = input.Height - 2;
			double radius = 5;

			cr.MoveTo (x + radius, y);
			cr.Arc (x + width - radius, y + radius, radius, Math.PI * 1.5, Math.PI * 2);
			cr.Arc (x + width - radius, y + height - radius, radius, 0, Math.PI * .5);
			cr.Arc (x + radius, y + height - radius, radius, Math.PI * .5, Math.PI);
			cr.Arc (x + radius, y + radius, radius, Math.PI, Math.PI * 1.5);

			Gdk.CairoHelper.SetSourcePixbuf (cr, input, 0, 0);
			cr.FillPreserve ();

			cr.LineWidth = 1;
			cr.Color = new Cairo.Color (0, 0, 0, 0.5);
			cr.Stroke ();

			byte[] data = new byte[surface.Data.Length];
			unsafe {
				fixed (byte* dataPtrSrc = data) {
					byte* dataPtr = dataPtrSrc;
					byte* imagePtr = (byte*) surface.DataPtr;

					for (int i = 0; i < data.Length; i+=4) {
						dataPtr[0] = imagePtr[2];
						dataPtr[1] = imagePtr[1];
						dataPtr[2] = imagePtr[0];
						dataPtr[3] = imagePtr[3];

						dataPtr += 4;
						imagePtr += 4;
					}
				}
			}

			int stride = surface.Stride;

			surface.Destroy ();
			(cr as IDisposable).Dispose ();

			return new Pixbuf (data, true, 8, input.Width, input.Height, stride);
		}
	}
}

