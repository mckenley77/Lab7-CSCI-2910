using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TestProject1
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string ISBN { get; set; }
    }

    public class BookService
    {
        private readonly string booksFilePath;
        private List<Book> books;

        public BookService(string filePath, List<Book> bookList)
        {
            booksFilePath = filePath;
            books = bookList;
        }

        public List<Book> GetBooks() => books;

        public async Task<List<Book>> LoadBooks()
        {
            if (!File.Exists(booksFilePath)) return new List<Book>();

            var lines = await File.ReadAllLinesAsync(booksFilePath);
            books = lines.Skip(1)
                .Select(line => line.Split(','))
                .Select(fields => new Book
                {
                    Id = int.Parse(fields[0]),
                    Title = fields[1],
                    Author = fields[2],
                    ISBN = fields[3]
                }).ToList();

            return books;
        }

        public async Task<bool> AddBook(Book book)
        {
            book.Id = books.Any() ? books.Max(b => b.Id) + 1 : 1;
            books.Add(book);
            await SaveBooks();
            return true;
        }

        public async Task<bool> EditBook(Book book)
        {
            var existing = books.FirstOrDefault(b => b.Id == book.Id);
            if (existing == null) return false;

            existing.Title = book.Title;
            existing.Author = book.Author;
            existing.ISBN = book.ISBN;
            await SaveBooks();
            return true;
        }

        public async Task<bool> DeleteBook(int bookId)
        {
            var book = books.FirstOrDefault(b => b.Id == bookId);
            if (book == null) return false;

            books.Remove(book);
            await SaveBooks();
            return true;
        }

        private async Task SaveBooks()
        {
            var lines = new List<string> { "Id,Title,Author,ISBN" };
            lines.AddRange(books.Select(b => $"{b.Id},{b.Title},{b.Author},{b.ISBN}"));
            await File.WriteAllLinesAsync(booksFilePath, lines);
        }
    }

    [TestClass]
    public class BookServiceTests
    {
        private string testFilePath;
        private List<Book> books;
        private BookService service;

        [TestInitialize]
        public void Setup()
        {
            testFilePath = Path.GetTempFileName();
            books = new List<Book>();
            service = new BookService(testFilePath, books);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(testFilePath))
                File.Delete(testFilePath);
        }

        [TestMethod]
        public async Task LoadBooks_FileExists_ReturnsBooks()
        {
            var lines = new List<string>
            {
                "Id,Title,Author,ISBN",
                "1,Test Book,Author A,123456"
            };
            await File.WriteAllLinesAsync(testFilePath, lines);

            var result = await service.LoadBooks();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Test Book", result[0].Title);
        }

        [TestMethod]
        public async Task AddBook_AddsNewBook()
        {
            var book = new Book { Title = "New Book", Author = "Author A", ISBN = "1111" };

            var result = await service.AddBook(book);

            Assert.IsTrue(result);
            Assert.AreEqual(1, books.Count);
            Assert.AreEqual("New Book", books[0].Title);
        }

        [TestMethod]
        public async Task EditBook_ExistingBook_UpdatesDetails()
        {
            var book = new Book { Id = 1, Title = "Old", Author = "X", ISBN = "123" };
            books.Add(book);
            var updated = new Book { Id = 1, Title = "New", Author = "Y", ISBN = "456" };

            var result = await service.EditBook(updated);

            Assert.IsTrue(result);
            var edited = books.First();
            Assert.AreEqual("New", edited.Title);
            Assert.AreEqual("Y", edited.Author);
            Assert.AreEqual("456", edited.ISBN);
        }

        [TestMethod]
        public async Task DeleteBook_ExistingBook_RemovesIt()
        {
            var book = new Book { Id = 1, Title = "Delete Me", Author = "Z", ISBN = "999" };
            books.Add(book);

            var result = await service.DeleteBook(1);

            Assert.IsTrue(result);
            Assert.AreEqual(0, books.Count);
        }

        [TestMethod]
        public async Task DeleteBook_NonExistent_ReturnsFalse()
        {
            var result = await service.DeleteBook(99);

            Assert.IsFalse(result);
        }
    }
}
