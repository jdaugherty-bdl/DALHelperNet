using DALHelperNet.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.Tests.Extensions
{
    [TestFixture]
    public class StatementDecorationTests
    {
        public string NoDecorationsName { get; set; }
        public string StartDecorationsName { get; set; }
        public string EndDecorationsName { get; set; }
        public string CompleteDecorationsName { get; set; }

        [SetUp]
        public void Setup()
        {
            CompleteDecorationsName = "`column_name`";
            EndDecorationsName = "column_name`";
            StartDecorationsName = "`column_name";
            NoDecorationsName = "column_name";
        }

        [Test]
        public void QuoteDecorationsStartsWithTest()
        {
            var result = StatementDecorationsExtension.MySqlObjectQuote(StartDecorationsName);

            Assert.That(result, Is.EqualTo(CompleteDecorationsName));
        }

        [Test]
        public void QuoteDecorationsEndsWithTest()
        {
            var result = StatementDecorationsExtension.MySqlObjectQuote(EndDecorationsName);

            Assert.That(result, Is.EqualTo(CompleteDecorationsName));
        }

        [Test]
        public void QuoteDecorationsNoDecorationsTest()
        {
            var result = StatementDecorationsExtension.MySqlObjectQuote(NoDecorationsName);

            Assert.That(result, Is.EqualTo(CompleteDecorationsName));
        }
    }
}
