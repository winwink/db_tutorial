using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Winwink.MySqlite.REPL.User;

namespace Winwink.MySqlite.REPL
{
    public class Pager:IDisposable
    {
        public const int PageSize = 4096; //页大小
        public const int TableMaxPages = 100; //单表最大页数 100

        private string _filePath;
        private FileStream _fileStream;

        private byte[][] _pages = new byte[TableMaxPages][];
        public int FileLength;

        public static Pager PagerOpen(string filePath)
        {
            Pager pager = new Pager();
            FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            pager.FileLength = (int)fs.Length;
            pager._filePath = filePath;
            pager._fileStream = fs;
            return pager;
        }

        public byte[] GetPage(int pageNumber)
        {
            if (pageNumber >= TableMaxPages)
            {
                throw new ArgumentOutOfRangeException("pageNumber");
            }

            if (_pages[pageNumber] == null)
            {
                _pages[pageNumber] = new byte[PageSize];
                var pageCount = FileLength / PageSize;
                if (pageNumber < pageCount)
                {
                    _fileStream.Read(_pages[pageNumber], 0, PageSize);
                }
                else if (pageNumber == pageCount)
                {
                    var additionalRowsNumber = FileLength % PageSize;
                    if (additionalRowsNumber > 0)
                    {
                        var offset = FileLength % PageSize;
                        _fileStream.Read(_pages[pageNumber], 0, offset);
                    }
                    else
                    {
                        _fileStream.Read(_pages[pageNumber], 0, PageSize);
                    }
                }
            }

            return _pages[pageNumber];
        }

        public void Flush(int pageNumber, int size)
        {
            _fileStream.Seek(pageNumber * PageSize, SeekOrigin.Begin);
            _fileStream.Write(_pages[pageNumber], 0, size);
            //_fileStream.Flush();
        }

        public void Dispose()
        {
            _fileStream?.Dispose();
        }
    }
}
