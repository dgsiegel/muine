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
using System.Runtime.InteropServices;
using System.Collections;

using Gdk;

using Mono.Posix;

namespace Muine
{
	public class Metadata
	{
		// Strings
		private static readonly string string_error_load =
			Catalog.GetString ("Failed to load metadata: {0}");

		// Objects
		private IntPtr raw = IntPtr.Zero;

		// Constructor
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_load (string filename,
					                    out IntPtr error_message_return);
		
		public Metadata (string filename)
		{
			IntPtr error_ptr;
			
			raw = metadata_load (filename, out error_ptr);
			if (error_ptr != IntPtr.Zero) {
				string error = GLib.Marshaller.PtrToStringGFree (error_ptr);
				throw new Exception (String.Format (string_error_load, error));
			}
		}

		// Properties
		// Properties :: Title (get;)
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_title (IntPtr metadata);

		public string Title {
			get {
				IntPtr p = metadata_get_title (raw);
				if (p == IntPtr.Zero)
					return "";
				else
					return Marshal.PtrToStringAnsi (p);
			}
		}

		// Properties :: Artists (get;)
		[DllImport ("libmuine")]
		private static extern int metadata_get_artist_count (IntPtr metadata);

		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_artist (IntPtr metadata, int index);

		public string [] Artists {
			get {
				ArrayList strings = new ArrayList ();

				int count = metadata_get_artist_count (raw);
				for (int i = 0; i < count; i++) {
					string tmp = Marshal.PtrToStringAnsi (metadata_get_artist (raw, i));
					if (tmp.Length > 0)
						strings.Add (tmp);
				}

				return (string []) strings.ToArray (typeof (string));
			}
		}

		// Properties :: Performers (get;)
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_performer (IntPtr metadata, int index);

		[DllImport ("libmuine")]
		private static extern int metadata_get_performer_count (IntPtr metadata);

		public string [] Performers {
			get {
				ArrayList strings = new ArrayList ();

				int count = metadata_get_performer_count (raw);
				for (int i = 0; i < count; i++) {
					string tmp = Marshal.PtrToStringAnsi (metadata_get_performer (raw, i));
					if (tmp.Length > 0)
						strings.Add (tmp);
				}

				return (string []) strings.ToArray (typeof (string));
			}			
		}

		// Properties :: Album (get;)
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_album (IntPtr metadata);

		public string Album {
			get { 
				IntPtr p = metadata_get_album (raw);
				if (p == IntPtr.Zero)
					return "";
				else
					return Marshal.PtrToStringAnsi (p);
			}
		}

		// Properties :: AlbumArt (get;)
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_album_art (IntPtr metadata);

		public Pixbuf AlbumArt {
			get { 
				IntPtr p = metadata_get_album_art (raw);
				if (p == IntPtr.Zero)
					return null;
				else
					return new Pixbuf (p);
			}
		}

		// Properties :: TrackNumber (get;)
		[DllImport ("libmuine")]
		private static extern int metadata_get_track_number (IntPtr metadata);
		
		public int TrackNumber {
			get { 
				return metadata_get_track_number (raw);
			}
		}

		// Properties :: TotalTracks (get;)
		[DllImport ("libmuine")]
		private static extern int metadata_get_total_tracks (IntPtr metadata);
		
		public int TotalTracks {
			get { 
				return metadata_get_total_tracks (raw);
			}
		}

		// Properties :: DiscNumber (get;)
		[DllImport ("libmuine")]
		private static extern int metadata_get_disc_number (IntPtr metadata);

		public int DiscNumber {
			get { 
				return metadata_get_disc_number (raw);
			}
		}

		// Properties :: Year (get;)
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_year (IntPtr metadata);

		public string Year {
			get {
				IntPtr p = metadata_get_year (raw);
				if (p == IntPtr.Zero)
					return "";
				else
					return Marshal.PtrToStringAnsi (p);
			}
		}

		// Properties :: Duration (get;)
		[DllImport ("libmuine")]
		private static extern int metadata_get_duration (IntPtr metadata);

		public int Duration {
			get { 
				return metadata_get_duration (raw);
			}
		}

		// Properties :: MimeType (get;)
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_mime_type (IntPtr metadata);

		public string MimeType {
			get {
				IntPtr p = metadata_get_mime_type (raw);
				if (p == IntPtr.Zero)
					return "";
				else
					return Marshal.PtrToStringAnsi (p);
			}
		}

		// Properties :: MTime (get;)
		[DllImport ("libmuine")]
		private static extern int metadata_get_mtime (IntPtr metadata);

		public int MTime {
			get {
				return metadata_get_mtime (raw);
			}
		}

		// Properties :: Gain (get;)
		[DllImport ("libmuine")]
		private static extern double metadata_get_gain (IntPtr metadata);

		public double Gain {
			get { 
				return metadata_get_gain (raw);
			}
		}

		// Properties :: Peak (get;)
		[DllImport ("libmuine")]
		private static extern double metadata_get_peak (IntPtr metadata);
		
		public double Peak {
			get { 
				return metadata_get_peak (raw);
			}
		}
	}
}
