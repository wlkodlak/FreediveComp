using MilanWilczak.FreediveComp.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MilanWilczak.FreediveComp.Tests.Models
{
    [TestFixture]
    public class PerformanceComponentTest
    {
        [Test]
        public void GetDuration()
        {
            var performance = new Performance { Duration = TimeSpan.FromSeconds(180) };
            Assert.That(PerformanceComponent.Duration.Get(performance), Is.EqualTo(180));
            Assert.That(PerformanceComponent.Duration.Get(new Performance()), Is.Null);
        }

        [Test]
        public void GetDistance()
        {
            var performance = new Performance { Distance = 180 };
            Assert.That(PerformanceComponent.Distance.Get(performance), Is.EqualTo(180));
            Assert.That(PerformanceComponent.Distance.Get(new Performance()), Is.Null);
        }

        [Test]
        public void GetDepth()
        {
            var performance = new Performance { Depth = 180 };
            Assert.That(PerformanceComponent.Depth.Get(performance), Is.EqualTo(180));
            Assert.That(PerformanceComponent.Depth.Get(new Performance()), Is.Null);
        }

        [Test]
        public void SetDuration()
        {
            var performance = new Performance();
            PerformanceComponent.Duration.Modify(performance, 115);
            Assert.That(performance.Duration, Is.EqualTo(TimeSpan.FromSeconds(115)));
        }

        [Test]
        public void SetDistance()
        {
            var performance = new Performance();
            PerformanceComponent.Distance.Modify(performance, 115);
            Assert.That(performance.Distance, Is.EqualTo(115));
        }

        [Test]
        public void SetDepth()
        {
            var performance = new Performance();
            PerformanceComponent.Depth.Modify(performance, 115);
            Assert.That(performance.Depth, Is.EqualTo(115));
        }
    }
}
