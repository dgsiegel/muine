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

#include <gtk/gtktreeselection.h>
#include <gtk/gtkversion.h>
#include <gtk/gtkwindow.h>

#include "pointer-list-view.h"

static void pointer_list_view_finalize (GObject *object);
static void pointer_list_view_init (PointerListView *view);
static void pointer_list_view_class_init (PointerListViewClass *klass);

enum {
	POINTER_ACTIVATED,
	SELECTION_CHANGED,
	LAST_SIGNAL
};

static GObjectClass *parent_class;
static guint signals[LAST_SIGNAL];

GType
pointer_list_view_get_type (void)
{
	static GType type = 0;

	if (!type) {
		static const GTypeInfo info = {
			sizeof (PointerListViewClass),
			NULL,
			NULL,
			(GClassInitFunc) pointer_list_view_class_init,
			NULL,
			NULL,
			sizeof (PointerListView),
			0,
			(GInstanceInitFunc) pointer_list_view_init,
		};

		type = g_type_register_static (GTK_TYPE_TREE_VIEW,
					       "PointerListView",
					       &info, 0);
	}

	return type;
}

static void
pointer_list_view_class_init (PointerListViewClass *klass)
{
	GObjectClass *object_class;

	parent_class = g_type_class_peek_parent (klass);
	object_class = (GObjectClass *) klass;

	object_class->finalize = pointer_list_view_finalize;

	signals[POINTER_ACTIVATED] =
		g_signal_new ("pointer_activated",
			      G_TYPE_FROM_CLASS (klass),
			      G_SIGNAL_RUN_LAST,
			      0,
			      NULL, NULL,
			      g_cclosure_marshal_VOID__POINTER,
			      G_TYPE_NONE, 1, G_TYPE_POINTER);

	signals[SELECTION_CHANGED] =
		g_signal_new ("selection_changed",
			      G_TYPE_FROM_CLASS (klass),
			      G_SIGNAL_RUN_LAST,
			      0,
			      NULL, NULL,
			      g_cclosure_marshal_VOID__VOID,
			      G_TYPE_NONE, 0);
}

static void
pointer_activated_cb (GtkTreeView *tree_view,
		      GtkTreePath *path,
		      GtkTreeViewColumn *column,
		      PointerListView *view)
{
	GtkTreeIter iter;
	gpointer ptr;

	gtk_tree_model_get_iter ((GtkTreeModel *) view->model,
				 &iter, path);

	ptr = pointer_list_model_get_pointer (view->model, &iter);

	g_signal_emit (view, signals[POINTER_ACTIVATED], 0,
		       ptr, NULL);
}

static void
selection_changed_cb (GtkTreeSelection *sel,
		      PointerListView *view)
{
	g_signal_emit (view, signals[SELECTION_CHANGED], 0, NULL);
}

static void
pointer_list_view_init (PointerListView *view)
{
	GtkTreeView *tree_view = GTK_TREE_VIEW (view);

	view->model = (PointerListModel *) pointer_list_model_new ();

	gtk_tree_view_set_model (tree_view, (GtkTreeModel *) view->model);
	gtk_tree_view_set_rules_hint (tree_view, TRUE);
	gtk_tree_view_set_enable_search (tree_view, FALSE);
	gtk_tree_view_set_headers_visible (tree_view, FALSE);
	gtk_tree_view_set_fixed_height_mode (tree_view, TRUE);

	g_signal_connect (G_OBJECT (gtk_tree_view_get_selection (tree_view)),
			  "changed",
			  G_CALLBACK (selection_changed_cb),
			  view);

	g_signal_connect (G_OBJECT (view),
			  "row_activated",
			  G_CALLBACK (pointer_activated_cb),
			  view);
}

static void
pointer_list_view_finalize (GObject *object)
{
	PointerListView *view = (PointerListView *) object;

	g_list_foreach (view->data, (GFunc) g_free, NULL);
	g_list_free (view->data);

	(* G_OBJECT_CLASS (parent_class)->finalize) (object);
}

PointerListView *
pointer_list_view_new (void)
{
	return g_object_new (TYPE_POINTER_LIST_VIEW, NULL);
}

void
pointer_list_view_changed (PointerListView *view,
			   gpointer pointer)
{
	GtkTreeIter iter;
	GtkTreePath *path;

	if (!pointer_list_model_pointer_get_iter (view->model, pointer, &iter))
		return;

	path = gtk_tree_model_get_path (GTK_TREE_MODEL (view->model), &iter);
	gtk_tree_model_row_changed (GTK_TREE_MODEL (view->model), path, &iter);
	gtk_tree_path_free (path);
}

gpointer
pointer_list_get_handle_from_path (PointerListView *view,
		                   GtkTreePath *path)
{
	GtkTreeIter iter;

	gtk_tree_model_get_iter ((GtkTreeModel *) view->model,
				 &iter, path);

	return pointer_list_model_get_pointer (view->model, &iter);
}

int
pointer_list_view_get_length (PointerListView *view)
{
	return gtk_tree_model_iter_n_children (GTK_TREE_MODEL (view->model), NULL);
}

static gboolean
pointer_foreach_func (PointerListModel *model,
	              GtkTreePath *path,
	              GtkTreeIter *iter,
	              void **data)
{
	GList **list = (GList **) data;
	gpointer pointer;

	pointer = pointer_list_model_get_pointer (model, iter);

	*list = g_list_append (*list, pointer);

	return FALSE;
}

static gboolean
path_foreach_func (PointerListModel *model,
	           GtkTreePath *path,
	           GtkTreeIter *iter,
	           void **data)
{
	GList **list = (GList **) data;

	*list = g_list_append (*list, gtk_tree_path_copy (path));

	return FALSE;
}

GList *
pointer_list_view_get_selection (PointerListView *view)
{
	GtkTreeSelection *sel;
	GList *list = NULL;

	sel = gtk_tree_view_get_selection (GTK_TREE_VIEW (view));
	gtk_tree_selection_selected_foreach (sel,
					     (GtkTreeSelectionForeachFunc) pointer_foreach_func,
					     (gpointer) &list);

	return list;
}

void
pointer_list_view_select_first (PointerListView *view)
{
	GtkTreeView *tree_view = GTK_TREE_VIEW (view);
	GtkTreePath *path;

	path = gtk_tree_path_new_first ();
	gtk_tree_view_set_cursor (tree_view, path,
				  gtk_tree_view_get_column (tree_view, 0), FALSE);
	gtk_tree_path_free (path);
}

gboolean
pointer_list_view_select_next (PointerListView *view)
{
	GtkTreeView *tree_view = GTK_TREE_VIEW (view);
	GtkTreeSelection *sel;
	GList *list = NULL, *l;
	gboolean last = TRUE;
	gboolean ret = FALSE;

	sel = gtk_tree_view_get_selection (tree_view);
	gtk_tree_selection_selected_foreach (sel,
					     (GtkTreeSelectionForeachFunc) path_foreach_func,
					     (gpointer) &list);

	for (l = g_list_last (list); l != NULL; l = g_list_previous (l)) {
		GtkTreePath *p = (GtkTreePath *) l->data;

		if (last) {
			GtkTreeIter iter;
			GtkTreePath *next;

			next = gtk_tree_path_copy (p);
			gtk_tree_path_next (next);
			if (gtk_tree_model_get_iter (GTK_TREE_MODEL (view->model), &iter, next)) {
				gtk_tree_view_set_cursor (tree_view, next,
							  gtk_tree_view_get_column (tree_view, 0), FALSE);

				ret = TRUE;
			}
			gtk_tree_path_free (next);

			last = FALSE;
		}

		gtk_tree_path_free (p);
	}

	g_list_free (list);

	return ret;
}

gboolean
pointer_list_view_select_prev (PointerListView *view)
{
	GtkTreeView *tree_view = GTK_TREE_VIEW (view);
	GtkTreeSelection *sel;
	GList *list = NULL, *l;
	gboolean first = TRUE;
	gboolean ret = FALSE;

	sel = gtk_tree_view_get_selection (tree_view);
	gtk_tree_selection_selected_foreach (sel,
					     (GtkTreeSelectionForeachFunc) path_foreach_func,
					     (gpointer) &list);

	for (l = list; l != NULL; l = g_list_next (l)) {
		GtkTreePath *p = (GtkTreePath *) l->data;

		if (first) {
			GtkTreePath *prev = gtk_tree_path_copy (p);
			if (gtk_tree_path_prev (prev)) {
				gtk_tree_view_set_cursor (tree_view, prev,
							  gtk_tree_view_get_column (tree_view, 0), FALSE);

				ret = TRUE;
			}
			gtk_tree_path_free (prev);

			first = FALSE;
		}

		gtk_tree_path_free (p);
	}

	g_list_free (list);

	return ret;
}

void
pointer_list_view_select (PointerListView *view,
		          gpointer pointer,
			  gboolean center)
{
	GtkTreeView *tree_view = GTK_TREE_VIEW (view);
	GtkTreeIter iter;
	GtkTreePath *path;
	GtkTreeViewColumn *col = gtk_tree_view_get_column (tree_view, 0);

	pointer_list_model_pointer_get_iter (view->model, pointer, &iter);

	path = gtk_tree_model_get_path (GTK_TREE_MODEL (view->model), &iter);

	if (center) {
		gtk_tree_view_scroll_to_cell (GTK_TREE_VIEW (view), path,
				              col, TRUE, 0.5, 0.5);
	}

	gtk_tree_view_set_cursor (tree_view, path, col, FALSE);
	gtk_tree_path_free (path);
}

gpointer
pointer_list_view_get_model (PointerListView *view)
{
	return view->model;
}
