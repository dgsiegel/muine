/*
 * Copyright (C) 2004 Jorn Baayen <jorn@nl.linux.org>
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

public class Metadata 
{
	private string title;
	public string Title {
		get {
			return title;
		}
	}

	private string [] artists;
	public string [] Artists {
		get {
			return artists;
		}
	}

	private string [] performers;
	public string [] Performers {
		get {
			return performers;
		}
	}

	private string album;
	public string Album {
		get {
			return album;
		}
	}

	private Pixbuf album_art;
	public Pixbuf AlbumArt {
		get {
			return album_art;
		}
	}

	private int track_number;
	public int TrackNumber {
		get {
			return track_number;
		}
	}

	private string year;
	public string Year {
		get {
			return year;
		}
	}

	private int duration;
	public int Duration {
		get {
			return duration;
		}
	}

	private string mime_type;
	public string MimeType {
		get {
			return mime_type;
		}
	}

	private int mtime;
	public int MTime {
		get {
			return mtime;
		}
	}

	private double gain;
	public double Gain {
		get {
			return gain;
		}
	}

	private double peak;
	public double Peak {
		get {
			return peak;
		}
	}

	[DllImport ("libmuine")]
	private static extern IntPtr metadata_load (string filename,
				                    out string error_message_return);
						   
	[DllImport ("libmuine")]
	private static extern void metadata_free (IntPtr metadata);
	
	[DllImport ("libmuine")]
	private static extern string metadata_get_title (IntPtr metadata);

	[DllImport ("libmuine")]
	private static extern string metadata_get_artist (IntPtr metadata,
	                                                  int index);
	[DllImport ("libmuine")]
	private static extern int metadata_get_artist_count (IntPtr metadata);

	[DllImport ("libmuine")]
	private static extern string metadata_get_performer (IntPtr metadata,
	                                                     int index);
	[DllImport ("libmuine")]
	private static extern int metadata_get_performer_count (IntPtr metadata);

	[DllImport ("libmuine")]
	private static extern string metadata_get_album (IntPtr metadata);

	[DllImport ("libmuine")]
	private static extern IntPtr metadata_get_album_art (IntPtr metadata);

	[DllImport ("libmuine")]
	private static extern int metadata_get_track_number (IntPtr metadata);

	[DllImport ("libmuine")]
	private static extern string metadata_get_year (IntPtr metadata);

	[DllImport ("libmuine")]
	private static extern int metadata_get_duration (IntPtr metadata);

	[DllImport ("libmuine")]
	private static extern string metadata_get_mime_type (IntPtr metadata);

	[DllImport ("libmuine")]
	private static extern int metadata_get_mtime (IntPtr metadata);

	[DllImport ("libmuine")]
	private static extern double metadata_get_gain (IntPtr metadata);

	[DllImport ("libmuine")]
	private static extern double metadata_get_peak (IntPtr metadata);
	
	public Metadata (string filename)
	{
		string error = null, s;
		IntPtr md = metadata_load (filename, out error);

		if (error != null)
			throw new Exception (String.Format (Muine.Catalog.GetString ("Failed to load metadata: {0}"), error));

		s = metadata_get_title (md);
		if (s != null)
			title = s;
		else
			title = "";

		artists = new string [metadata_get_artist_count (md)];
		for (int i = 0; i < artists.Length; i++)
			artists[i] = metadata_get_artist (md, i);

		performers = new string [metadata_get_performer_count (md)];
		for (int i = 0; i < performers.Length; i++)
			performers[i] = metadata_get_performer (md, i);
		
		s = metadata_get_album (md);
		if (s != null)
			album = s;
		else
			album = "";

		if (metadata_get_album_art (md) != IntPtr.Zero)
			album_art = new Pixbuf (metadata_get_album_art (md));
		else
			album_art = null;

		track_number = metadata_get_track_number (md);

		s = metadata_get_year (md);
		if (s != null)
			year = s;
		else
			year = "";

		duration = metadata_get_duration (md);

		mime_type = metadata_get_mime_type (md);

		mtime = metadata_get_mtime (md);

		gain = metadata_get_gain (md);
		peak = metadata_get_peak (md);

		metadata_free (md);
	}
}
