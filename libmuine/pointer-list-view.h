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

#ifndef __POINTER_LIST_VIEW_H__
#define __POINTER_LIST_VIEW_H__

#include <gtk/gtktreeview.h>

#include "pointer-list-model.h"

#define TYPE_POINTER_LIST_VIEW            (pointer_list_view_get_type ())
#define POINTER_LIST_VIEW(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), TYPE_POINTER_LIST_VIEW, PointerListView))
#define POINTER_LIST_VIEW_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), TYPE_POINTER_LIST_VIEW, PointerListViewClass))
#define IS_POINTER_LIST_VIEW(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), TYPE_POINTER_LIST_VIEW))
#define IS_POINTER_LIST_VIEW_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), TYPE_POINTER_LIST_VIEW))
#define POINTER_LIST_VIEW_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), TYPE_POINTER_LIST_VIEW, PointerListViewClass))

typedef struct _PointerListView      PointerListView;
typedef struct _PointerListViewClass PointerListViewClass;

struct _PointerListView
{
  GtkTreeView parent;
  PointerListModel *model;
  GList *data;
};

struct _PointerListViewClass
{
  GtkTreeViewClass parent_class;
};

typedef void (*CellDataFunc) (PointerListView *view,
			      GtkCellRenderer *renderer,
			      gpointer pointer);

GType            pointer_list_view_get_type      (void);
PointerListView *pointer_list_view_new           (void);

void             pointer_list_view_add_column    (PointerListView *view,
						  GtkCellRenderer *renderer,
						  CellDataFunc func,
						  gboolean expand);
void             pointer_list_view_append        (PointerListView *view,
				                  gpointer pointer);
void             pointer_list_view_changed       (PointerListView *view,
			                          gpointer pointer);
void             pointer_list_view_remove        (PointerListView *view,
			                          gpointer pointer);
void             pointer_list_view_remove_delta  (PointerListView *view,
				                  GList *delta);
void             pointer_list_view_clear         (PointerListView *view);
GList *          pointer_list_view_get_contents  (PointerListView *view);
int              pointer_list_view_get_length    (PointerListView *view);
gboolean         pointer_list_view_contains      (PointerListView *view,
				                  gpointer pointer);
GList *          pointer_list_view_get_selection (PointerListView *view);
void             pointer_list_view_select_first  (PointerListView *view);
gboolean         pointer_list_view_select_next   (PointerListView *view,
						  gboolean center,
						  gboolean scroll);
gboolean         pointer_list_view_select_prev   (PointerListView *view,
						  gboolean center,
						  gboolean scroll);
void             pointer_list_view_select        (PointerListView *view,
						  gpointer pointer);
void             pointer_list_view_scroll_to     (PointerListView *view,
						  gpointer pointer);
void             pointer_list_view_set_sort_func (PointerListView *view,
				                  GCompareFunc sort_func);
void             pointer_list_view_set_playing   (PointerListView *view,
				                  gpointer pointer);
gpointer         pointer_list_view_get_playing   (PointerListView *view);
gboolean         pointer_list_view_has_first     (PointerListView *view);
gboolean         pointer_list_view_has_prev      (PointerListView *view);
gboolean         pointer_list_view_has_next      (PointerListView *view);
gpointer         pointer_list_view_first         (PointerListView *view);
gpointer	 pointer_list_view_last		 (PointerListView *view);
gpointer         pointer_list_view_prev          (PointerListView *view);
gpointer         pointer_list_view_next          (PointerListView *view);
int              pointer_list_view_get_index_of  (PointerListView *view,
						  gpointer pointer);
void             pointer_list_view_state_changed (PointerListView *view);

#endif /* __POINTER_LIST_VIEW_H__ */
