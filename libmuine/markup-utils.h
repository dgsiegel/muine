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

#ifndef __MARKUP_UTILS_H__
#define __MARKUP_UTILS_H__

#include <gtk/gtkcellrenderertext.h>
#include <gtk/gtklabel.h>

void label_set_markup (GtkLabel *label,
		       int start_index,
		       int end_index,
		       gboolean large,
		       gboolean bold,
		       gboolean italic);

void cell_set_markup (GtkCellRendererText *cell,
		      int start_index,
		      int end_index,
		      gboolean large,
		      gboolean bold,
		      gboolean italic);

#endif /* __MARKUP_UTILS_H__ */
