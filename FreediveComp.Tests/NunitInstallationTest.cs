using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MilanWilczak.FreediveComp.Tests
{
    [TestFixture]
    public class NunitInstallationTest
    {
        [Test]
        public void TestFail()
        {
            Assert.That(5, Is.EqualTo(3));
        }
    }
}
