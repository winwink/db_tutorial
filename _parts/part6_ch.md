---
title: Part 6 - 游标抽象
date: 2017-09-10
---

这章比上章短, 我们将进行一个小的重构, 让我们更容易的开始b树的实现.
This should be a shorter part than the last one. We're just going to refactor a bit to make it easier to start the B-Tree implementation.

我们将增加一个`Cursor`的对象来代表表中的位置. 你可以用游标做这些事:
We're going to add a `Cursor` object which represents a location in the table. Things you might want to do with cursors:

- 在表的开始的地方创建一个游标
- 在表的结束的地方创建一个游标
- 访问游标指向的数据行
- 把游标移向下一行

- Create a cursor at the beginning of the table
- Create a cursor at the end of the table
- Access the row the cursor is pointing to
- Advance the cursor to the next row

那些是我们将要实现的行为. 之后, 我们再来实现以下特性:
Those are the behaviors we're going to implement now. Later, we will also want to:

- 删除游标指向的行
- 修改游标指向的行
- 根据一个指定的ID搜索表, 并创建一个指向这个行的游标

我们先定义`Cursor`的结构如下:

- Delete the row pointed to by a cursor
- Modify the row pointed to by a cursor
- Search a table for a given ID, and create a cursor pointing to the row with that ID

Without further ado, here's the `Cursor` type:

```diff
+struct Cursor_t {
+  Table* table;
+  uint32_t row_num;
+  bool end_of_table;  // Indicates a position one past the last element
+};
+typedef struct Cursor_t Cursor;
```

Given our current table data structure, all you need to identify a location in a table is the row number.

A cursor also has a reference to the table it's part of (so our cursor functions can take just the cursor as a parameter).

Finally, it has a boolean called `end_of_table`. This is so we can represent a position past the end of the table (which is somewhere we may want to insert a row).

`table_start()` and `table_end()` create new cursors:

```diff
+Cursor* table_start(Table* table) {
+  Cursor* cursor = malloc(sizeof(Cursor));
+  cursor->table = table;
+  cursor->row_num = 0;
+  cursor->end_of_table = (table->num_rows == 0);
+
+  return cursor;
+}
+
+Cursor* table_end(Table* table) {
+  Cursor* cursor = malloc(sizeof(Cursor));
+  cursor->table = table;
+  cursor->row_num = table->num_rows;
+  cursor->end_of_table = true;
+
+  return cursor;
+}
```

Our `row_slot()` function will become `cursor_value()`, which returns a pointer to the position described by the cursor:

```diff
-void* row_slot(Table* table, uint32_t row_num) {
+void* cursor_value(Cursor* cursor) {
+  uint32_t row_num = cursor->row_num;
   uint32_t page_num = row_num / ROWS_PER_PAGE;
-  void* page = get_page(table->pager, page_num);
+  void* page = get_page(cursor->table->pager, page_num);
   uint32_t row_offset = row_num % ROWS_PER_PAGE;
   uint32_t byte_offset = row_offset * ROW_SIZE;
   return page + byte_offset;
 }
```

Advancing the cursor in our current table structure is as simple as incrementing the row number. This will be a bit more complicated in a B-tree.

```diff
+void cursor_advance(Cursor* cursor) {
+  cursor->row_num += 1;
+  if (cursor->row_num >= cursor->table->num_rows) {
+    cursor->end_of_table = true;
+  }
+}
```

最终我们改变我们的`virtual machine`方法来使用`cursor`抽象. 当我们插入一行的时候, 我们打开一个表末尾的游标, 向这个游标的位置写数据, 然后关闭游标.

Finally we can change our "virtual machine" methods to use the cursor abstraction. When inserting a row, we open a cursor at the end of table, write to that cursor location, then close the cursor.

```diff
   Row* row_to_insert = &(statement->row_to_insert);
+  Cursor* cursor = table_end(table);

-  serialize_row(row_to_insert, row_slot(table, table->num_rows));
+  serialize_row(row_to_insert, cursor_value(cursor));
   table->num_rows += 1;

+  free(cursor);
+
   return EXECUTE_SUCCESS;
 }
 ```

When selecting all rows in the table, we open a cursor at the start of the table, print the row, then advance the cursor to the next row. Repeat until we've reached the end of the table.

```diff
 ExecuteResult execute_select(Statement* statement, Table* table) {
+  Cursor* cursor = table_start(table);
+
   Row row;
-  for (uint32_t i = 0; i < table->num_rows; i++) {
-    deserialize_row(row_slot(table, i), &row);
+  while (!(cursor->end_of_table)) {
+    deserialize_row(cursor_value(cursor), &row);
     print_row(&row);
+    cursor_advance(cursor);
   }
+
+  free(cursor);
+
   return EXECUTE_SUCCESS;
 }
 ```

Alright, that's it! Like I said, this was a shorter refactor that should help us as we rewrite our table data structure into a B-Tree. `execute_select()` and `execute_insert()` can interact with the table entirely through the cursor without assuming anything about how the table is stored.

Here's the complete diff to this part:
```diff
@@ -78,6 +78,13 @@ struct Table_t {
 };
 typedef struct Table_t Table;

+struct Cursor_t {
+  Table* table;
+  uint32_t row_num;
+  bool end_of_table; // Indicates a position one past the last element
+};
+typedef struct Cursor_t Cursor;
+
 void print_row(Row* row) {
     printf("(%d, %s, %s)\n", row->id, row->username, row->email);
 }
@@ -126,12 +133,38 @@ void* get_page(Pager* pager, uint32_t page_num) {
     return pager->pages[page_num];
 }

-void* row_slot(Table* table, uint32_t row_num) {
-  uint32_t page_num = row_num / ROWS_PER_PAGE;
-  void *page = get_page(table->pager, page_num);
-  uint32_t row_offset = row_num % ROWS_PER_PAGE;
-  uint32_t byte_offset = row_offset * ROW_SIZE;
-  return page + byte_offset;
+Cursor* table_start(Table* table) {
+  Cursor* cursor = malloc(sizeof(Cursor));
+  cursor->table = table;
+  cursor->row_num = 0;
+  cursor->end_of_table = (table->num_rows == 0);
+
+  return cursor;
+}
+
+Cursor* table_end(Table* table) {
+  Cursor* cursor = malloc(sizeof(Cursor));
+  cursor->table = table;
+  cursor->row_num = table->num_rows;
+  cursor->end_of_table = true;
+
+  return cursor;
+}
+
+void* cursor_value(Cursor* cursor) {
+  uint32_t row_num = cursor->row_num;
+  uint32_t page_num = row_num / ROWS_PER_PAGE;
+  void *page = get_page(cursor->table->pager, page_num);
+  uint32_t row_offset = row_num % ROWS_PER_PAGE;
+  uint32_t byte_offset = row_offset * ROW_SIZE;
+  return page + byte_offset;
+}
+
+void cursor_advance(Cursor* cursor) {
+  cursor->row_num += 1;
+  if (cursor->row_num >= cursor->table->num_rows) {
+    cursor->end_of_table = true;
+  }
 }

 Pager* pager_open(const char* filename) {
@@ -327,19 +360,28 @@ ExecuteResult execute_insert(Statement* statement, Table* table) {
     }

   Row* row_to_insert = &(statement->row_to_insert);
+  Cursor* cursor = table_end(table);

-  serialize_row(row_to_insert, row_slot(table, table->num_rows));
+  serialize_row(row_to_insert, cursor_value(cursor));
   table->num_rows += 1;

+  free(cursor);
+
   return EXECUTE_SUCCESS;
 }

 ExecuteResult execute_select(Statement* statement, Table* table) {
+  Cursor* cursor = table_start(table);
+
   Row row;
-  for (uint32_t i = 0; i < table->num_rows; i++) {
-     deserialize_row(row_slot(table, i), &row);
+  while (!(cursor->end_of_table)) {
+     deserialize_row(cursor_value(cursor), &row);
      print_row(&row);
+     cursor_advance(cursor);
   }
+
+  free(cursor);
+
   return EXECUTE_SUCCESS;
 }
```
