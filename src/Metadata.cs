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

using Mono.Unix;

namespace Muine
{
	public class Metadata
	{
		// Strings
		private static readonly string string_error_load =
			Catalog.GetString ("Failed to load metadata: {0}");

		// Objects
		private IntPtr raw = IntPtr.Zero;
		private Pixbuf album_art = null;

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
				IntPtr title_ptr = metadata_get_title (raw);
				
				string title = "";
				
				if (title_ptr != IntPtr.Zero) {
					string title_tmp = Marshal.PtrToStringAnsi (title_ptr);
					title = title_tmp.Trim ();
				}
				
				return title;
			}
		}

		// Properties :: Artists (get;)
		//	FIXME: Refactor Artists and Performers properties
		[DllImport ("libmuine")]
		private static extern int metadata_get_artist_count (IntPtr metadata);

		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_artist
		  (IntPtr metadata, int index);

		public string [] Artists {
			get {
				ArrayList strings = new ArrayList ();

				int count = metadata_get_artist_count (raw);

				for (int i = 0; i < count; i++) {
					IntPtr artist_ptr = metadata_get_artist (raw, i);
					string artist_tmp = Marshal.PtrToStringAnsi (artist_ptr);
					string artist = artist_tmp.Trim ();

					if (artist.Length <= 0)
						continue;

					strings.Add (artist);
				}

				Type string_type = typeof (string);
				return (string []) strings.ToArray (string_type);
			}
		}

		// Properties :: Performers (get;)
		//	FIXME: Refactor Artists and Performers properties
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_performer
		  (IntPtr metadata, int index);

		[DllImport ("libmuine")]
		private static extern int metadata_get_performer_count
		  (IntPtr metadata);

		public string [] Performers {
			get {
				ArrayList strings = new ArrayList ();

				int count = metadata_get_performer_count (raw);

				for (int i = 0; i < count; i++) {
					IntPtr performer_ptr = metadata_get_performer (raw, i);

					string performer_tmp =
					  Marshal.PtrToStringAnsi (performer_ptr);

					string performer = performer_tmp.Trim ();

					if (performer.Length <= 0)
						continue;

					strings.Add (performer);
				}

				Type string_type = typeof (string);
				return (string []) strings.ToArray (string_type);
			}			
		}

		// Properties :: Album (get;)
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_album (IntPtr metadata);

		public string Album {
			get { 
				IntPtr album_ptr = metadata_get_album (raw);
				
				string album;
				if (album_ptr == IntPtr.Zero) {
					album = "";
				} else {
					string album_tmp = Marshal.PtrToStringAnsi (album_ptr);
					album = album_tmp.Trim ();
				}
				
				return album;
			}
		}

		// Properties :: AlbumArt (get;)
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_album_art (IntPtr metadata);

		public Pixbuf AlbumArt {
			get { 
				if (album_art != null)
					return album_art;
					
				IntPtr album_art_ptr = metadata_get_album_art (raw);

				if (album_art_ptr != IntPtr.Zero)
					album_art = new Pixbuf (album_art_ptr);

				return album_art;
			}
		}

		// Properties :: TrackNumber (get;)
		[DllImport ("libmuine")]
		private static extern int metadata_get_track_number (IntPtr metadata);
		
		public int TrackNumber {
			get { return metadata_get_track_number (raw); }
		}

		// Properties :: TotalTracks (get;)
		[DllImport ("libmuine")]
		private static extern int metadata_get_total_tracks (IntPtr metadata);
		
		public int TotalTracks {
			get { return metadata_get_total_tracks (raw); }
		}

		// Properties :: DiscNumber (get;)
		[DllImport ("libmuine")]
		private static extern int metadata_get_disc_number (IntPtr metadata);

		public int DiscNumber {
			get { return metadata_get_disc_number (raw); }
		}

		// Properties :: Year (get;)
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_year (IntPtr metadata);

		public string Year {
			get {
				IntPtr year_ptr = metadata_get_year (raw);
				
				string year = "";
				
				if (year_ptr != IntPtr.Zero)
					year = Marshal.PtrToStringAnsi (year_ptr);
				
				return year;
			}
		}

		// Properties :: Duration (get;)
		[DllImport ("libmuine")]
		private static extern int metadata_get_duration (IntPtr metadata);

		public int Duration {
			get { return metadata_get_duration (raw); }
		}

		// Properties :: MimeType (get;)
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_mime_type (IntPtr metadata);

		public string MimeType {
			get {
				IntPtr type_ptr = metadata_get_mime_type (raw);
				
				string type = "";
				if (type_ptr != IntPtr.Zero)
					Marshal.PtrToStringAnsi (type_ptr);
				
				return type;
			}
		}

		// Properties :: MTime (get;)
		[DllImport ("libmuine")]
		private static extern int metadata_get_mtime (IntPtr metadata);

		public int MTime {
			get { return metadata_get_mtime (raw); }
		}

		// Properties :: Gain (get;)
		[DllImport ("libmuine")]
		private static extern double metadata_get_gain (IntPtr metadata);

		public double Gain {
			get { return metadata_get_gain (raw); }
		}

		// Properties :: Peak (get;)
		[DllImport ("libmuine")]
		private static extern double metadata_get_peak (IntPtr metadata);
		
		public double Peak {
			get { return metadata_get_peak (raw); }
		}
	}
}
