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
using Gdk;

using Mono.Posix;

namespace Muine
{
	public class Metadata 
	{
		// Strings
		private static readonly string string_load_failed =
			Catalog.GetString ("Failed to load metadata: {0}");

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
							   
		[DllImport ("libmuine")]
		private static extern void metadata_free (IntPtr metadata);
		
		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_title (IntPtr metadata);

		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_artist (IntPtr metadata,
		                                                  int index);
		[DllImport ("libmuine")]
		private static extern int metadata_get_artist_count (IntPtr metadata);

		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_performer (IntPtr metadata,
		                                                     int index);
		[DllImport ("libmuine")]
		private static extern int metadata_get_performer_count (IntPtr metadata);

		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_album (IntPtr metadata);

		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_album_art (IntPtr metadata);

		[DllImport ("libmuine")]
		private static extern int metadata_get_track_number (IntPtr metadata);

		[DllImport ("libmuine")]
		private static extern int metadata_get_disc_number (IntPtr metadata);

		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_year (IntPtr metadata);

		[DllImport ("libmuine")]
		private static extern int metadata_get_duration (IntPtr metadata);

		[DllImport ("libmuine")]
		private static extern IntPtr metadata_get_mime_type (IntPtr metadata);

		[DllImport ("libmuine")]
		private static extern int metadata_get_mtime (IntPtr metadata);

		[DllImport ("libmuine")]
		private static extern double metadata_get_gain (IntPtr metadata);

		[DllImport ("libmuine")]
		private static extern double metadata_get_peak (IntPtr metadata);
		
		public Metadata (string filename)
		{
			IntPtr error_ptr, p;
			
			IntPtr md = metadata_load (filename, out error_ptr);
			if (error_ptr != IntPtr.Zero) {
				string error = GLib.Marshaller.PtrToStringGFree (error_ptr);
				throw new Exception (String.Format (string_load_failed, error));
			}

			p = metadata_get_title (md);
			title = (p == IntPtr.Zero)
				 ? ""
				 : Marshal.PtrToStringAnsi (p);

			artists = new string [metadata_get_artist_count (md)];
			for (int i = 0; i < artists.Length; i++)
				artists [i] = Marshal.PtrToStringAnsi (metadata_get_artist (md, i));

			performers = new string [metadata_get_performer_count (md)];
			for (int i = 0; i < performers.Length; i++)
				performers [i] = Marshal.PtrToStringAnsi (metadata_get_performer (md, i));
			
			p = metadata_get_album (md);
			album = (p == IntPtr.Zero)
				 ? ""
				 : Marshal.PtrToStringAnsi (p);

			p = metadata_get_album_art (md);
			album_art = (p == IntPtr.Zero)
				     ? null
				     : new Pixbuf (metadata_get_album_art (md));

			track_number = metadata_get_track_number (md);
			disc_number = metadata_get_disc_number (md);

			p = metadata_get_year (md);
			year = (p == IntPtr.Zero)
				? ""
				: Marshal.PtrToStringAnsi (p);

			duration = metadata_get_duration (md);

			mime_type = Marshal.PtrToStringAnsi (metadata_get_mime_type (md));

			mtime = metadata_get_mtime (md);

			gain = metadata_get_gain (md);
			peak = metadata_get_peak (md);

			metadata_free (md);
		}
								
		// Properties
		// Properties :: Title (get;)
		public string Title {
			get { return title; }
		}

		// Properties :: Artists (get;)
		public string [] Artists {
			get { return artists; }
		}

		// Properties :: Performers (get;)
		public string [] Performers {
			get { return performers; }
		}

		// Properties :: Album (get;)
		public string Album {
			get { return album; }
		}

		// Properties :: AlbumArt (get;)
		public Pixbuf AlbumArt {
			get { return album_art; }
		}

		// Properties :: TrackNumber (get;)
		public int TrackNumber {
			get { return track_number; }
		}

		// Properties :: DiscNumber (get;)
		public int DiscNumber {
			get { return disc_number; }
		}

		// Properties :: Year (get;)
		public string Year {
			get { return year; }
		}

		// Properties :: Duration (get;)
		public int Duration {
			get { return duration; }
		}

		// Properties :: MimeType (get;)
		public string MimeType {
			get { return mime_type; }
		}

		// Properties :: MTime (get;)
		public int MTime {
			get { return mtime; }
		}

		// Properties :: Gain (get;)
		public double Gain {
			get { return gain; }
		}

		// Properties :: Peak (get;)
		public double Peak {
			get { return peak; }
		}
	}
}
