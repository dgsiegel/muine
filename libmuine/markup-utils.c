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

#include <stdio.h>

#include "markup-utils.h"

void
label_set_markup (GtkLabel *label,
		  int start_index,
		  int end_index,
		  gboolean large,
		  gboolean bold,
		  gboolean italic)
{
	PangoAttrList *list = pango_attr_list_new ();
	PangoAttribute *attr;

	if (large) {
		attr = pango_attr_scale_new (PANGO_SCALE_LARGE);
		attr->start_index = start_index;
		attr->end_index = end_index;

		pango_attr_list_insert (list, attr);
	}

	if (bold) {
		attr = pango_attr_weight_new (PANGO_WEIGHT_BOLD);
		attr->start_index = start_index;
		attr->end_index = end_index;

		pango_attr_list_insert (list, attr);
	}

	if (italic) {
		attr = pango_attr_style_new (PANGO_STYLE_ITALIC);
		attr->start_index = start_index;
		attr->end_index = end_index;

		pango_attr_list_insert (list, attr);
	}

	gtk_label_set_attributes (label, list);

	pango_attr_list_unref (list);
}

void
cell_set_markup (GtkCellRendererText *cell,
		 int start_index,
		 int end_index,
		 gboolean large,
		 gboolean bold,
		 gboolean italic)
{
	PangoAttrList *list = pango_attr_list_new ();
	PangoAttribute *attr;

	if (large) {
		attr = pango_attr_scale_new (PANGO_SCALE_LARGE);
		attr->start_index = start_index;
		attr->end_index = end_index;

		pango_attr_list_insert (list, attr);
	}

	if (bold) {
		attr = pango_attr_weight_new (PANGO_WEIGHT_BOLD);
		attr->start_index = start_index;
		attr->end_index = end_index;

		pango_attr_list_insert (list, attr);
	}

	if (italic) {
		attr = pango_attr_style_new (PANGO_STYLE_ITALIC);
		attr->start_index = start_index;
		attr->end_index = end_index;

		pango_attr_list_insert (list, attr);
	}

	g_object_set (G_OBJECT (cell),
		      "attributes", list,
		      NULL);

	pango_attr_list_unref (list);
}
