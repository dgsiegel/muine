/*
 * Copyright (C) 2004 Tamara Roberson <foxxygirltamara@gmail.com>
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

public class Database
{
	public delegate IntPtr EncodeFunctionDelegate (IntPtr handle, out int length);
	public delegate void   DecodeFunctionDelegate (string key, IntPtr data);

	private IntPtr db_ptr;
	private EncodeFunctionDelegate encode_function;
	private DecodeFunctionDelegate decode_function;

	// Constructor
	public Database (string filename, int version)
	{
		IntPtr error_ptr;

		db_ptr = db_open (filename, version, out error_ptr); 

		if (db_ptr == IntPtr.Zero)
			throw new Exception (GLib.Marshaller.PtrToStringGFree (error_ptr));
	}

	[DllImport ("libmuine")]
	private static extern IntPtr db_open (string filename, int version,
	                                      out IntPtr error);

	// Handle
	public IntPtr Handle {
		get { return db_ptr; }
	}

	// EncodeFunction
	public EncodeFunctionDelegate EncodeFunction {
		get { return encode_function; }
		set { encode_function = value; }
	}
	
	// DecodeFunction
	public DecodeFunctionDelegate DecodeFunction {
		get { return decode_function; }
		
		set { decode_function = value; }
	}
		
	// Load
	public void Load ()
	{
		db_foreach (db_ptr, decode_function, IntPtr.Zero);
	}
		
	[DllImport ("libmuine")]
	private static extern void db_foreach (IntPtr db_ptr, 
					       DecodeFunctionDelegate decode_function, 
					       IntPtr data);

	// Store
	public void Store (string key, IntPtr val)
	{
		Store (key, val, false);
	}
	
	public void Store (string key, IntPtr val, bool overwrite)
	{
		db_store (db_ptr, key, overwrite, encode_function, val);
	} 

	[DllImport ("libmuine")]
	private static extern void db_store (IntPtr db_ptr, string key, bool overwrite,
					     EncodeFunctionDelegate encode_function,
					     IntPtr val);

	// Delete
	public void Delete (string key)
	{
		db_delete (db_ptr, key);
	}
	
	[DllImport ("libmuine")]
	private static extern void db_delete (IntPtr db_ptr, string key);

// ---------- Unpack ----------
	// UnpackBool
	public static IntPtr UnpackBool (IntPtr p, out bool b)
	{
		return db_unpack_bool (p, out b);        
	}

	[DllImport ("libmuine")]
	private static extern IntPtr db_unpack_bool (IntPtr p, out bool b);

	// UnpackDouble
	public static IntPtr UnpackDouble (IntPtr p, out double d)
	{
		return db_unpack_double (p, out d);
	}

	[DllImport ("libmuine")]
	private static extern IntPtr db_unpack_double (IntPtr p, out double d);

	// UnpackInt
	public static IntPtr UnpackInt (IntPtr p, out int i)
	{
		return db_unpack_int (p, out i);
	}

	[DllImport ("libmuine")]
	private static extern IntPtr db_unpack_int (IntPtr p, out int i);

	// UnpackPixbuf
	public static IntPtr UnpackPixbuf (IntPtr p, out IntPtr pixbuf)
	{
		return db_unpack_pixbuf (p, out pixbuf);
	}

	[DllImport ("libmuine")]
	private static extern IntPtr db_unpack_pixbuf (IntPtr p, out IntPtr pixbuf);

	// UnpackString
	public static IntPtr UnpackString (IntPtr p, out string str)
	{
		IntPtr str_ptr;
		IntPtr ret = UnpackString (p, out str_ptr); 
		str = GLib.Marshaller.PtrToStringGFree (str_ptr);
		return ret;
	}

	public static IntPtr UnpackString (IntPtr p, out IntPtr str_ptr)
	{
		return db_unpack_string (p, out str_ptr);
	}
		
	[DllImport ("libmuine")]
	private static extern IntPtr db_unpack_string (IntPtr p, out IntPtr str_ptr);

// ---------- Pack ----------

	// PackStart
	public static IntPtr PackStart ()
	{
		return db_pack_start ();
	}
	
	[DllImport ("libmuine")]
	private static extern IntPtr db_pack_start ();

	// PackEnd
	public static IntPtr PackEnd (IntPtr p, out int length)
	{
		return db_pack_end (p, out length);
	}
	
	[DllImport ("libmuine")]
	private static extern IntPtr db_pack_end (IntPtr p, out int length);

	// PackPixbuf
	public static void PackPixbuf (IntPtr p, IntPtr pixbuf)
	{
		db_pack_pixbuf (p, pixbuf);	
	}
	
	[DllImport ("libmuine")]
	private static extern void db_pack_pixbuf (IntPtr p, IntPtr pixbuf);

	// PackString
	public static void PackString (IntPtr p, string str)
	{
		db_pack_string (p, str);
	}
	
	[DllImport ("libmuine")]
	private static extern void db_pack_string (IntPtr p, string str);
	
	// PackInt
	public static void PackInt (IntPtr p, int i)
	{
		db_pack_int (p, i);
	}
	
	[DllImport ("libmuine")]
	private static extern void db_pack_int (IntPtr p, int i);
	
	// PackBool
	public static void PackBool (IntPtr p, bool b)
	{
		db_pack_bool (p, b);
	}
	
	[DllImport ("libmuine")]
	private static extern void db_pack_bool (IntPtr p, bool b);
	
	// PackDouble
	public static void PackDouble (IntPtr p, double d)
	{
		db_pack_double (p, d);
	}
	
	[DllImport ("libmuine")]
	private static extern void db_pack_double (IntPtr p, double d);
} 
