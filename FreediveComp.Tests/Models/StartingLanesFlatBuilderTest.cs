using MilanWilczak.FreediveComp.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MilanWilczak.FreediveComp.Tests.Models
{
    [TestFixture]
    public class StartingLanesFlatBuilderTest
    {
        [Test]
        public void GetParentNull()
        {
            var flattened = new StartingLanesFlatBuilder().GetParent(GetRootLanes(), null);
            Assert.That(flattened, Is.Null);
        }

        [Test]
        public void GetParentInRoot()
        {
            var flattened = new StartingLanesFlatBuilder().GetParent(GetRootLanes(), "STA");
            Assert.That(flattened, Is.Not.Null);
            Assert.That(flattened.StartingLaneId, Is.EqualTo("STA"));
            Assert.That(flattened.ShortName, Is.EqualTo("STA"));
            Assert.That(flattened.FullName, Is.EqualTo("STA"));
        }

        [Test]
        public void GetParentInSublevel()
        {
            var flattened = new StartingLanesFlatBuilder().GetParent(GetRootLanes(), "DYN/Elite");
            Assert.That(flattened, Is.Not.Null);
            Assert.That(flattened.StartingLaneId, Is.EqualTo("DYN/Elite"));
            Assert.That(flattened.ShortName, Is.EqualTo("Elite"));
            Assert.That(flattened.FullName, Is.EqualTo("DYN Elite"));
        }

        [Test]
        public void GetParentInDeepLevel()
        {
            var flattened = new StartingLanesFlatBuilder().GetParent(GetRootLanes(), "DYN/Elite/A");
            Assert.That(flattened, Is.Not.Null);
            Assert.That(flattened.StartingLaneId, Is.EqualTo("DYN/Elite/A"));
            Assert.That(flattened.ShortName, Is.EqualTo("A"));
            Assert.That(flattened.FullName, Is.EqualTo("DYN Elite A"));
        }

        [Test]
        public void GetLeavesNull()
        {
            var flattened = new StartingLanesFlatBuilder().GetLeaves(GetRootLanes(), null);
            var ids = flattened.Select(f => f.StartingLaneId).ToArray();
            var fullnames = flattened.Select(f => f.FullName).ToArray();
            Assert.That(ids, Is.EqualTo(new String[] {
                "STA/ME/A", "STA/ME/B", "STA/Hobbies/A", "STA/Hobbies/B", "STA/Elite/A", "STA/Elite/B",
                "DYN/ME/A", "DYN/ME/B", "DYN/Hobbies/A", "DYN/Hobbies/B", "DYN/Elite/A", "DYN/Elite/B",
                "CWT/ME/A", "CWT/ME/B", "CWT/Hobbies/A", "CWT/Hobbies/B", "CWT/Elite/A", "CWT/Elite/B",
            }));
            Assert.That(fullnames, Is.EqualTo(new String[] {
                "STA ME A", "STA ME B", "STA Hobbies A", "STA Hobbies B", "STA Elite A", "STA Elite B",
                "DYN ME A", "DYN ME B", "DYN Hobbies A", "DYN Hobbies B", "DYN Elite A", "DYN Elite B",
                "CWT ME A", "CWT ME B", "CWT Hobbies A", "CWT Hobbies B", "CWT Elite A", "CWT Elite B",
            }));
        }

        [Test]
        public void GetLeavesForRootLane()
        {
            var flattened = new StartingLanesFlatBuilder().GetLeaves(GetRootLanes(), "STA");
            var ids = flattened.Select(f => f.StartingLaneId).ToArray();
            var fullnames = flattened.Select(f => f.FullName).ToArray();
            Assert.That(ids, Is.EqualTo(new String[] {
                "STA/ME/A", "STA/ME/B", "STA/Hobbies/A", "STA/Hobbies/B", "STA/Elite/A", "STA/Elite/B",
            }));
            Assert.That(fullnames, Is.EqualTo(new String[] {
                "ME A", "ME B", "Hobbies A", "Hobbies B", "Elite A", "Elite B",
            }));
        }

        [Test]
        public void GetLeavesForAlmostLeaf()
        {
            var flattened = new StartingLanesFlatBuilder().GetLeaves(GetRootLanes(), "DYN/Hobbies");
            var ids = flattened.Select(f => f.StartingLaneId).ToArray();
            var fullnames = flattened.Select(f => f.FullName).ToArray();
            Assert.That(ids, Is.EqualTo(new String[] { "DYN/Hobbies/A", "DYN/Hobbies/B" }));
            Assert.That(fullnames, Is.EqualTo(new String[] { "A", "B" }));
        }

        [Test]
        public void GetLeavesForLeaf()
        {
            var flattened = new StartingLanesFlatBuilder().GetLeaves(GetRootLanes(), "DYN/Hobbies/B");
            var ids = flattened.Select(f => f.StartingLaneId).ToArray();
            var fullnames = flattened.Select(f => f.FullName).ToArray();
            Assert.That(ids, Is.EqualTo(new String[] { "DYN/Hobbies/B" }));
            Assert.That(fullnames, Is.EqualTo(new String[] { "" }));
        }

        private List<StartingLane> GetRootLanes()
        {
            return new List<StartingLane> {
                BuildLane("STA",
                    BuildLane("ME", BuildLane("A"), BuildLane("B")),
                    BuildLane("Hobbies", BuildLane("A"), BuildLane("B")),
                    BuildLane("Elite", BuildLane("A"), BuildLane("B"))
                ),
                BuildLane("DYN",
                    BuildLane("ME", BuildLane("A"), BuildLane("B")),
                    BuildLane("Hobbies", BuildLane("A"), BuildLane("B")),
                    BuildLane("Elite", BuildLane("A"), BuildLane("B"))
                ),
                BuildLane("CWT",
                    BuildLane("ME", BuildLane("A"), BuildLane("B")),
                    BuildLane("Hobbies", BuildLane("A"), BuildLane("B")),
                    BuildLane("Elite", BuildLane("A"), BuildLane("B"))
                )
            };
        }

        private StartingLane BuildLane(string name, params StartingLane[] subLanes)
        {
            var thisLane = new StartingLane
            {
                StartingLaneId = name,
                ShortName = name,
                SubLanes = subLanes.ToList()
            };
            AdjustSublanesIds(name, thisLane.SubLanes);
            return thisLane;
        }

        private void AdjustSublanesIds(string prefix, IEnumerable<StartingLane> lanes)
        {
            foreach (var subLane in lanes)
            {
                subLane.StartingLaneId = prefix + "/" + subLane.StartingLaneId;
                AdjustSublanesIds(prefix, subLane.SubLanes);
            }
        }
    }
}
