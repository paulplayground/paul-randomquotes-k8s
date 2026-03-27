// test - triggering CI/CD pipeline, and again and again
using NUnit.Framework;
using RandomQuotes.Web.Models;

namespace RandomQuotes.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            Quote.Initialize();
        }

        [Test]
        public void Test1()
        {
            Assert.That(Quote.GetRandomQuote().QuoteText != "Something went wrong");
        }
    }
}