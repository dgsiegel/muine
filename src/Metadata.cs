/*
 * Copyright (C) 2004 Jorn Baayen <jbaayen@gnome.org>
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

		// Variables
		private string title;
		private string [] artists;
		private string [] performers;
		private string album;
		private Pixbuf album_art;
		private int track_number;
		private int disc_number;
		private string year;
		private int duration;
		private string mime_type;
		private int mtime;
		private double gain;
		private double peak;

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
				if (title == null) {
					IntPtr p = metadata_get_title (raw);
					if (p == IntPtr.Zero)
						title = "";
					else
						title = Marshal.PtrToStringAnsi (p);
				}
				
				return title;
			}
		}

		// Properties :: Artists (get;)
		[DllImport ("libmuine")]
		private static extern int metadata_get_artist_count (IntPtr metadata);

		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_artist (IntPtr metadata, int index);

		public string [] Artists {
			get {
				if (artists == null) {
					ArrayList strings = new ArrayList ();

					int count = metadata_get_artist_count (raw);
					for (int i = 0; i < count; i++) {
						string tmp = Marshal.PtrToStringAnsi (metadata_get_artist (raw, i));
						if (tmp.Length > 0)
							strings.Add (tmp);
					}

					artists = (string []) strings.ToArray (typeof (string));
				}

				return artists;
			}
		}

		// Properties :: Performers (get;)
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_performer (IntPtr metadata, int index);

		[DllImport ("libmuine")]
		private static extern int metadata_get_performer_count (IntPtr metadata);

		public string [] Performers {
			get {
				if (performers == null) {
					ArrayList strings = new ArrayList ();

					int count = metadata_get_performer_count (raw);
					for (int i = 0; i < count; i++) {
						string tmp = Marshal.PtrToStringAnsi (metadata_get_performer (raw, i));
						if (tmp.Length > 0)
							strings.Add (tmp);
					}

					performers = (string []) strings.ToArray (typeof (string));
				}
				
				return performers;
			}			
		}

		// Properties :: Album (get;)
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_album (IntPtr metadata);

		public string Album {
			get { 
				if (album == null) {
					IntPtr p = metadata_get_album (raw);
					if (p == IntPtr.Zero)
						album = "";
					else
						album = Marshal.PtrToStringAnsi (p);
				}
				
				return album;
			}
		}

		// Properties :: AlbumArt (get;)
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_album_art (IntPtr metadata);

		public Pixbuf AlbumArt {
			get { 
				if (album_art == null) {
					IntPtr p = metadata_get_album_art (raw);
					if (p == IntPtr.Zero)
						album_art = null;
					else
						album_art = new Pixbuf (p);
				}
				
				return album_art;
			}
		}

		// Properties :: TrackNumber (get;)
		[DllImport ("libmuine")]
		private static extern int metadata_get_track_number (IntPtr metadata);
		
		public int TrackNumber {
			get { 
				if (track_number == 0)
					track_number = metadata_get_track_number (raw);
				return track_number;
			}
		}

		// Properties :: DiscNumber (get;)
		[DllImport ("libmuine")]
		private static extern int metadata_get_disc_number (IntPtr metadata);

		public int DiscNumber {
			get { 
				if (disc_number == 0)
					disc_number = metadata_get_disc_number (raw);
				return disc_number;
			}
		}

		// Properties :: Year (get;)
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_year (IntPtr metadata);

		public string Year {
			get {
				if (year == null) {
					IntPtr p = metadata_get_year (raw);
					if (p == IntPtr.Zero)
						year = "";
					else
						year = Marshal.PtrToStringAnsi (p);
				}
				
				return year;
			}
		}

		// Properties :: Duration (get;)
		[DllImport ("libmuine")]
		private static extern int metadata_get_duration (IntPtr metadata);

		public int Duration {
			get { 
				if (duration == 0)
					duration = metadata_get_duration (raw);
				return duration;
			}
		}

		// Properties :: MimeType (get;)
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_mime_type (IntPtr metadata);

		public string MimeType {
			get {
				if (mime_type == null) {
					IntPtr p = metadata_get_mime_type (raw);
					if (p == IntPtr.Zero)
						mime_type = "";
					else
						mime_type = Marshal.PtrToStringAnsi (p);
				}
				
				return mime_type;
			}
		}

		// Properties :: MTime (get;)
		[DllImport ("libmuine")]
		private static extern int metadata_get_mtime (IntPtr metadata);

		public int MTime {
			get {
				if (mtime == 0)
					mtime = metadata_get_mtime (raw);
					
				return mtime; 
			}
		}

		// Properties :: Gain (get;)
		[DllImport ("libmuine")]
		private static extern double metadata_get_gain (IntPtr metadata);

		public double Gain {
			get { 
				if (gain == 0)
					gain = metadata_get_gain (raw);
				return gain; 
			}
		}

		// Properties :: Peak (get;)
		[DllImport ("libmuine")]
		private static extern double metadata_get_peak (IntPtr metadata);
		
		public double Peak {
			get { 
				if (peak == 0)
					peak = metadata_get_peak (raw);

				return peak; 
			}
		}
	}
}
