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

#include <gtk/gtktreeselection.h>

#include "pointer-list-view.h"

static void pointer_list_view_finalize (GObject *object);
static void pointer_list_view_init (PointerListView *view);
static void pointer_list_view_class_init (PointerListViewClass *klass);

enum {
	POINTER_ACTIVATED,
	POINTERS_REORDERED,
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

	signals[POINTERS_REORDERED] =
		g_signal_new ("pointers_reordered",
			      G_TYPE_FROM_CLASS (klass),
			      G_SIGNAL_RUN_LAST,
			      0,
			      NULL, NULL,
			      g_cclosure_marshal_VOID__VOID,
			      G_TYPE_NONE, 0);

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
pointers_reordered_cb (GtkTreeModel *tree_model,
		       GtkTreePath *path,
		       GtkTreeIter *unused_iter,
		       gint *new_order,
		       PointerListView *view)
{
	PointerListModel *model;
	GtkTreeSelection *sel;
	GtkTreeIter iter;

	model = POINTER_LIST_MODEL (tree_model);

	sel = gtk_tree_view_get_selection (GTK_TREE_VIEW (view));

	pointer_list_model_get_moved_iter (model, &iter);
	gtk_tree_selection_select_iter (sel, &iter);

	g_signal_emit (view, signals[POINTERS_REORDERED], 0, NULL);
}

static void
row_deleted_cb (GtkTreeModel *tree_model,
		GtkTreePath *path,
		PointerListView *view)
{
	GtkTreeSelection *sel;
	GtkTreeIter iter;

	sel = gtk_tree_view_get_selection (GTK_TREE_VIEW (view));

	if (!gtk_tree_model_get_iter (tree_model, &iter, path)) {
		GtkTreePath *prev = gtk_tree_path_copy (path);
		if (!gtk_tree_path_prev (prev)) {
			gtk_tree_path_free (prev);
			return;
		}
		gtk_tree_model_get_iter (tree_model, &iter, prev);
		gtk_tree_path_free (prev);
	}

	gtk_tree_selection_select_iter (sel, &iter);
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
	GtkTreeSelection *sel;

	view->model = (PointerListModel *) pointer_list_model_new ();

	gtk_tree_view_set_model (GTK_TREE_VIEW (view), (GtkTreeModel *) view->model);
	gtk_tree_view_set_rules_hint (GTK_TREE_VIEW (view), TRUE);
	gtk_tree_view_set_enable_search (GTK_TREE_VIEW (view), FALSE);
	gtk_tree_view_set_headers_visible (GTK_TREE_VIEW (view), FALSE);

	sel = gtk_tree_view_get_selection (GTK_TREE_VIEW (view));
	gtk_tree_selection_set_mode (sel, GTK_SELECTION_SINGLE);

	g_signal_connect (G_OBJECT (sel),
			  "changed",
			  G_CALLBACK (selection_changed_cb),
			  view);

	g_signal_connect (G_OBJECT (view),
			  "row_activated",
			  G_CALLBACK (pointer_activated_cb),
			  view);

	g_signal_connect (G_OBJECT (view->model),
			  "rows_reordered",
			  G_CALLBACK (pointers_reordered_cb),
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

typedef struct {
	PointerListView *view;
	CellDataFunc func;
} CellDataFuncData;

static void
cell_data_func (GtkTreeViewColumn *col,
		GtkCellRenderer *cell,
		GtkTreeModel *model,
		GtkTreeIter *iter,
		CellDataFuncData *data)
{
	data->func (data->view, cell,
	            pointer_list_model_get_pointer ((PointerListModel *) model, iter));
}

void
pointer_list_view_add_column (PointerListView *view,
			      GtkCellRenderer *renderer,
			      CellDataFunc func)
{
	GtkTreeViewColumn *column;
	CellDataFuncData *data;

	data = g_new0 (CellDataFuncData, 1);
	data->func = func;
	data->view = view;
	view->data = g_list_append (view->data, data);

	column = gtk_tree_view_column_new ();
	gtk_tree_view_column_set_sizing (column,
					 GTK_TREE_VIEW_COLUMN_AUTOSIZE);
	gtk_tree_view_column_pack_start (column,
					 renderer,
					 FALSE);
	gtk_tree_view_column_set_cell_data_func (column,
						 renderer,
						 (GtkTreeCellDataFunc) cell_data_func,
						 data,
						 NULL);
	gtk_tree_view_append_column (GTK_TREE_VIEW (view),
				     column);
}

void
pointer_list_view_append (PointerListView *view,
		          gpointer pointer)
{
	pointer_list_model_add (view->model, pointer);
}

void
pointer_list_view_remove (PointerListView *view,
			  gpointer pointer)
{
	pointer_list_model_remove (view->model, pointer);
}

void
pointer_list_view_remove_delta (PointerListView *view,
				GList *delta)
{
	pointer_list_model_remove_delta (view->model, delta);
}

void
pointer_list_view_clear (PointerListView *view)
{
	pointer_list_model_clear (view->model);
}

GList *
pointer_list_view_get_contents (PointerListView *view)
{
	return pointer_list_model_get_pointers (view->model);
}

int
pointer_list_view_get_length (PointerListView *view)
{
	return gtk_tree_model_iter_n_children (GTK_TREE_MODEL (view->model), NULL);
}

gboolean
pointer_list_view_contains (PointerListView *view,
		            gpointer pointer)
{
	return pointer_list_model_contains (view->model, pointer);
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
pointer_list_view_set_keep_selection (PointerListView *view,
				      gboolean keep)
{
	/* doesn't handle remove yet */
	if (keep)
		g_signal_connect (G_OBJECT (view->model),
				  "row_deleted",
				  G_CALLBACK (row_deleted_cb),
				  view);
}

void
pointer_list_view_select_first (PointerListView *view)
{
	GtkTreePath *path;
	GtkTreeSelection *sel;

	sel = gtk_tree_view_get_selection (GTK_TREE_VIEW (view));

	path = gtk_tree_path_new_first ();
	gtk_tree_selection_unselect_all (sel);
	gtk_tree_selection_select_path (sel, path);
	gtk_tree_path_free (path);
}

static void
scroll_to_path (PointerListView *view, GtkTreePath *path,
		gboolean center)
{
	gtk_tree_view_scroll_to_cell (GTK_TREE_VIEW (view), path,
				      gtk_tree_view_get_column (GTK_TREE_VIEW (view), 0),
				      center, 0.5, 0.5);
}

void
pointer_list_view_select_next (PointerListView *view, gboolean center)
{
	GtkTreeSelection *sel;
	GList *list = NULL, *l;
	gboolean last = TRUE;

	sel = gtk_tree_view_get_selection (GTK_TREE_VIEW (view));
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
				gtk_tree_selection_unselect_all (sel);
				gtk_tree_selection_select_path (sel, next);
				scroll_to_path (view, next, center);
			} else {
				scroll_to_path (view, p, center);
			}
			gtk_tree_path_free (next);

			last = FALSE;
		}

		gtk_tree_path_free (p);
	}

	g_list_free (list);
}

void
pointer_list_view_select_prev (PointerListView *view, gboolean center)
{
	GtkTreeSelection *sel;
	GList *list = NULL, *l;
	gboolean last = TRUE;

	sel = gtk_tree_view_get_selection (GTK_TREE_VIEW (view));
	gtk_tree_selection_selected_foreach (sel,
					     (GtkTreeSelectionForeachFunc) path_foreach_func,
					     (gpointer) &list);

	for (l = g_list_last (list); l != NULL; l = g_list_previous (l)) {
		GtkTreePath *p = (GtkTreePath *) l->data;

		if (last) {
			GtkTreePath *prev = gtk_tree_path_copy (p);
			if (gtk_tree_path_prev (prev)) {
				gtk_tree_selection_unselect_all (sel);
				gtk_tree_selection_select_path (sel, prev);
				scroll_to_path (view, prev, center);
			} else {
				scroll_to_path (view, p, center);
			}
			gtk_tree_path_free (prev);

			last = FALSE;
		}

		gtk_tree_path_free (p);
	}

	g_list_free (list);
}

void
pointer_list_view_scroll_to (PointerListView *view,
			     gpointer pointer)
{
	GtkTreeIter iter;
	GtkTreePath *path;

	pointer_list_model_pointer_get_iter (view->model, pointer, &iter);
	path = gtk_tree_model_get_path (GTK_TREE_MODEL (view->model), &iter);

	gtk_tree_view_scroll_to_cell (GTK_TREE_VIEW (view), path,
				      gtk_tree_view_get_column (GTK_TREE_VIEW (view), 0),
				      TRUE, 0.5, 0.5);

	gtk_tree_path_free (path);
}

void
pointer_list_view_set_sort_func (PointerListView *view,
				 GCompareFunc sort_func)
{
	pointer_list_model_set_sorting (view->model,
					sort_func,
					GTK_SORT_ASCENDING);
}

void
pointer_list_view_set_playing (PointerListView *view,
			       gpointer pointer)
{
	pointer_list_model_set_current (view->model, pointer);
}

gpointer
pointer_list_view_get_playing (PointerListView *view)
{
	return pointer_list_model_get_current (view->model);
}

gboolean
pointer_list_view_has_first (PointerListView *view)
{
	return pointer_list_model_has_first (view->model);
}

gboolean
pointer_list_view_has_prev (PointerListView *view)
{
	return pointer_list_model_has_prev (view->model);
}

gboolean
pointer_list_view_has_next (PointerListView *view)
{
	return pointer_list_model_has_next (view->model);
}

gpointer
pointer_list_view_first (PointerListView *view)
{
	return pointer_list_model_first (view->model);
}

gpointer
pointer_list_view_prev (PointerListView *view)
{
	return pointer_list_model_prev (view->model);
}

gpointer
pointer_list_view_next (PointerListView *view)
{
	return pointer_list_model_next (view->model);
}

void
pointer_list_view_state_changed (PointerListView *view)
{
	pointer_list_model_state_changed (view->model);
}
