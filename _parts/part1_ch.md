---
title: Part 1 - 介绍和实现REPL(交互式解析器)
date: 2017-08-30
---

作为一个Web开发者, 我在日常工作中每天都会使用关系型数据库, 但是它对于我来说是一个黑盒. 我经常想这些问题:
- 数据用什么格式保存的?(内存中和磁盘上)
- 什么时候数据从内存保存到磁盘上?
- 为什么每个表只有一个主键?
- 回滚操作是怎么运行的?
- 索引是什么格式的?
- 全表扫描是什么时候发生的? 是怎么运行的?
- prepared语句是以什么格式保存的?

换言之, 数据库怎么**工作**

为了搞清楚这些, 我正在用scratch写一个数据库. 它是以sqlite为原型的, 因为sqlite是一个小型数据库, 比Mysql或者PostgreSQL有更少的功能, 因此我更有希望能理解它.
整个数据库存储在一个文件中.

# Sqlite
在他们的站点上有很多文档[documentation of sqlite internals](https://www.sqlite.org/arch.html) , 另外我也下载了一个备份在[SQLite Database System: Design and Implementation](https://play.google.com/store/books/details?id=9Z6IQQnX1JEC).

一个查询语句通过一系列的组件来获取或者修改数据, 前端由以下部分构成:
- tokenizer
- parser
- code generator

输入由sql语句构成, 输出是sqlite虚拟机字节码(一个可以在数据库上执行的程序)

后端由以下组成:
- 虚拟机
- B树
- pager
- 操作系统接口层

虚拟机将前端产生的字节码作为输入. 然后在一个或多个表或者索引上执行操作, 这些都是存储在一个叫B树的数据结构上面. 
虚拟机本质上就是一个巨大的基于字节码的switch语句.

每一个B树由很多个节点构成, 每个节点都由1个页大小构成. B树一次可以从磁盘上读或写一个页大小的数据, 通过给pager写命令.

pager接收命令按页来读或写数据. 决定了从数据的正确的位移读取数据. 在内存中留存了最近使用的页, 并且决定何时写回磁盘.

操作系统接口层决定了程序编译到哪个系统, 在这个教程中, 我将不会支持跨多个平台.

[A journey of a thousand miles begins with a single step](https://en.wiktionary.org/wiki/a_journey_of_a_thousand_miles_begins_with_a_single_step)
让我们直接一点开始, 交互式解释器.

## 创建一个简单的交互式解释器
当你从一个命令行开始打开sqlite, 它给你提供了一个交互式的界面

```shell
~ sqlite3
SQLite version 3.16.0 2016-11-04 19:09:39
Enter ".help" for usage hints.
Connected to a transient in-memory database.
Use ".open FILENAME" to reopen on a persistent database.
sqlite> create table users (id int, username varchar(255), email varchar(255));
sqlite> .tables
users
sqlite> .exit
~
```

为了做到这一点, 我们的主程序会有一个无限循环, 接受输入, 然后处理输入, 最后输出

```c
int main(int argc, char* argv[]) {
  InputBuffer* input_buffer = new_input_buffer();
  while (true) {
    print_prompt();
    read_input(input_buffer);

    if (strcmp(input_buffer->buffer, ".exit") == 0) {
      close_input_buffer(input_buffer);
      exit(EXIT_SUCCESS);
    } else {
      printf("Unrecognized command '%s'.\n", input_buffer->buffer);
    }
  }
}
```
我们将会定义一个`InputBuffer`来存储输入的值.
```c
struct InputBuffer_t {
  char* buffer;
  size_t buffer_length;
  ssize_t input_length;
};
typedef struct InputBuffer_t InputBuffer;

InputBuffer* new_input_buffer() {
  InputBuffer* input_buffer = malloc(sizeof(InputBuffer));
  input_buffer->buffer = NULL;
  input_buffer->buffer_length = 0;
  input_buffer->input_length = 0;

  return input_buffer;
}
```

Next, `print_prompt()` prints a prompt to the user. We do this before reading each line of input.

```c
void print_prompt() { printf("db > "); }
```

To read a line of input, use [getline()](http://man7.org/linux/man-pages/man3/getline.3.html):
```c
ssize_t getline(char **lineptr, size_t *n, FILE *stream);
```
`lineptr` : a pointer to the variable we use to point to the buffer containing the read line. If it set to `NULL` it is mallocatted by `getline` and should thus be freed by the user, even if the command fails.

`n` : a pointer to the variable we use to save the size of allocated buffer.

`stream` : the input stream to read from. We'll be reading from standard input.

`return value` : the number of bytes read, which may be less than the size of the buffer.

We tell `getline` to store the read line in `input_buffer->buffer` and the size of the allocated buffer in `input_buffer->buffer_length`. We store the return value in `input_buffer->input_length`.

`buffer` starts as null, so `getline` allocates enough memory to hold the line of input and makes `buffer` point to it.

```c
void read_input(InputBuffer* input_buffer) {
  ssize_t bytes_read =
      getline(&(input_buffer->buffer), &(input_buffer->buffer_length), stdin);

  if (bytes_read <= 0) {
    printf("Error reading input\n");
    exit(EXIT_FAILURE);
  }

  // Ignore trailing newline
  input_buffer->input_length = bytes_read - 1;
  input_buffer->buffer[bytes_read - 1] = 0;
}
```

Now it is proper to define a function that frees the memory allocated for an
instance of `InputBuffer *` and the `buffer` element of the respective
structure (`getline` allocates memory for `input_buffer->buffer` in
`read_input`).

```c
void close_input_buffer(InputBuffer* input_buffer) {
    free(input_buffer->buffer);
    free(input_buffer);
}
```

Finally, we parse and execute the command. There is only one recognized command right now : `.exit`, which terminates the program. Otherwise we print an error message and continue the loop.

```c
if (strcmp(input_buffer->buffer, ".exit") == 0) {
  close_input_buffer(input_buffer);
  exit(EXIT_SUCCESS);
} else {
  printf("Unrecognized command '%s'.\n", input_buffer->buffer);
}
```

Let's try it out!
```shell
~ ./db
db > .tables
Unrecognized command '.tables'.
db > .exit
~
```

Alright, we've got a working REPL. In the next part, we'll start developing our command language. Meanwhile, here's the entire program from this part:

```c
#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

struct InputBuffer_t {
  char* buffer;
  size_t buffer_length;
  ssize_t input_length;
};
typedef struct InputBuffer_t InputBuffer;

InputBuffer* new_input_buffer() {
  InputBuffer* input_buffer = malloc(sizeof(InputBuffer));
  input_buffer->buffer = NULL;
  input_buffer->buffer_length = 0;
  input_buffer->input_length = 0;

  return input_buffer;
}

void print_prompt() { printf("db > "); }

void read_input(InputBuffer* input_buffer) {
  ssize_t bytes_read =
      getline(&(input_buffer->buffer), &(input_buffer->buffer_length), stdin);

  if (bytes_read <= 0) {
    printf("Error reading input\n");
    exit(EXIT_FAILURE);
  }

  // Ignore trailing newline
  input_buffer->input_length = bytes_read - 1;
  input_buffer->buffer[bytes_read - 1] = 0;
}

void close_input_buffer(InputBuffer* input_buffer) {
    free(input_buffer->buffer);
    free(input_buffer);
}

int main(int argc, char* argv[]) {
  InputBuffer* input_buffer = new_input_buffer();
  while (true) {
    print_prompt();
    read_input(input_buffer);

    if (strcmp(input_buffer->buffer, ".exit") == 0) {
      close_input_buffer(input_buffer);
      exit(EXIT_SUCCESS);
    } else {
      printf("Unrecognized command '%s'.\n", input_buffer->buffer);
    }
  }
}
```
