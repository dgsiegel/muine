/*
 * Copyright (C) 2004 Lee Willis <lee@leewillis.co.uk>
 *           (C) 2005 Jorn Baayen <jbaayen@gnome.org>
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
using System.IO;
using System.Collections;
using System.Net.Sockets;
using System.Xml;
using System.Text;

using Muine.PluginLib;

public class DashboardPlugin : Plugin
{
	private IPlayer player;

	public override void Initialize (IPlayer player)
	{
		this.player = player;
		
		player.SongChangedEvent += new SongChangedEventHandler (OnSongChangedEvent);
	}
	
	private void OnSongChangedEvent (ISong song)
	{
		if (song == null)
			return;

		bool has_focus = player.Window.HasToplevelFocus;

		SendClue (song.Artists, song.Album, song.Title, has_focus);
	}

	private void SendClue (string [] artists, string album, string song_title, bool has_focus)
	{
		TcpClient tcp_client = new TcpClient ();
			
		tcp_client.SendTimeout = 50;

		try {
			tcp_client.Connect ("127.0.0.1", 5913);

			NetworkStream network_stream = tcp_client.GetStream ();

			if (network_stream.CanWrite) {
				XmlDocument doc;
				XmlElement cluepacket, elem;

				doc = new XmlDocument ();

				/* Create the cluepacket */
				cluepacket = doc.CreateElement ("CluePacket");
				doc.AppendChild (cluepacket);

				/* Add standard stuff */
				elem = doc.CreateElement ("Frontend");
				elem.InnerText = "Muine";
				cluepacket.AppendChild (elem);

				elem = doc.CreateElement ("Context");
				elem.InnerText = "FIXME - What should go here";
				cluepacket.AppendChild (elem);

				elem = doc.CreateElement ("Focused");
				elem.InnerText = has_focus ? "True" : "False";
				cluepacket.AppendChild (elem);

				elem = doc.CreateElement ("Additive");
				elem.InnerText = "False";
				cluepacket.AppendChild (elem);

				/* Add the artist clues */
				foreach (string artist in artists) {
					elem = doc.CreateElement ("Clue");
					elem.SetAttribute ("Type", "artist");
					elem.SetAttribute ("Relevance", "10");
					elem.InnerText = artist;
					cluepacket.AppendChild (elem);
				}

				/* Add the album clue */
				elem = doc.CreateElement ("Clue");
				elem.SetAttribute ("Type", "album");
				elem.SetAttribute ("Relevance", "10");
				elem.InnerText = album;
				cluepacket.AppendChild (elem);
				
				/* Add the song_title clue */
				elem = doc.CreateElement ("Clue");
				elem.SetAttribute ("Type", "song_title");
				elem.SetAttribute ("Relevance", "10");
				elem.InnerText = song_title;
				cluepacket.AppendChild (elem);

				XmlTextWriter writer = new XmlTextWriter (network_stream, null);
				doc.WriteTo (writer);
				writer.Flush ();
			}

			tcp_client.Close ();

		} catch {
			return;
		}
	}
}
