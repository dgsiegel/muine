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
using System.Runtime.InteropServices;

public class Metadata 
{
	private string [] titles;
	public string [] Titles {
		get {
			return titles;
		}
	}

	private string [] artists;
	public string [] Artists {
		get {
			return artists;
		}
	}

	private string album;
	public string Album {
		get {
			return album;
		}
	}

	private string year;
	public string Year {
		get {
			return year;
		}
	}

	private long duration;
	public long Duration {
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

	[DllImport ("libmuine")]
	private static extern IntPtr metadata_load (string filename,
				                    out string error_message_return);
						   
	[DllImport ("libmuine")]
	private static extern void metadata_free (IntPtr metadata);
	
	[DllImport ("libmuine")]
	private static extern string metadata_get_title (IntPtr metadata,
	                                                 int index);
	[DllImport ("libmuine")]
	private static extern int metadata_get_title_count (IntPtr metadata);

	[DllImport ("libmuine")]
	private static extern string metadata_get_artist (IntPtr metadata,
	                                                  int index);
	[DllImport ("libmuine")]
	private static extern int metadata_get_artist_count (IntPtr metadata);

	[DllImport ("libmuine")]
	private static extern string metadata_get_album (IntPtr metadata,
	                                                 int index);
	[DllImport ("libmuine")]
	private static extern int metadata_get_album_count (IntPtr metadata);

	[DllImport ("libmuine")]
	private static extern string metadata_get_year (IntPtr metadata);

	[DllImport ("libmuine")]
	private static extern int metadata_get_duration (IntPtr metadata);

	[DllImport ("libmuine")]
	private static extern string metadata_get_mime_type (IntPtr metadata);

	public Metadata (string filename)
	{
		string error = null;
		IntPtr md = metadata_load (filename, out error);

		if (error != null)
			throw new Exception ("Failed to load metadata: " + error);

		titles = new string [metadata_get_title_count (md)];
		for (int i = 0; i < titles.Length; i++) {
			titles[i] = String.Copy (metadata_get_title (md, i));
		}

		artists = new string [metadata_get_artist_count (md)];
		for (int i = 0; i < artists.Length; i++) {
			artists[i] = String.Copy (metadata_get_artist (md, i));
		}
		
		if (metadata_get_album_count (md) > 0)
			album = String.Copy (metadata_get_album (md, 0));
		else
			album = "";

		string y = metadata_get_year (md);
		if (y != null)
			year = String.Copy (y);
		else
			year = "";

		duration = metadata_get_duration (md);

		mime_type = String.Copy (metadata_get_mime_type (md));

		metadata_free (md);
	}
}
