using NUnit.Framework;
using Omnikeeper.Base.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.Integration.Model
{
    class CIIDSelectionTest
    {
        [Test]
        public void TestExcept()
        {
            var ga = Guid.NewGuid();
            var gb = Guid.NewGuid();
            var gc = Guid.NewGuid();

            // specific.Except(X)
            Assert.AreEqual(
                SpecificCIIDsSelection.Build(ga), 
                SpecificCIIDsSelection.Build(ga, gb).Except(SpecificCIIDsSelection.Build(gb, gc)));
            Assert.AreEqual(
                SpecificCIIDsSelection.Build(gb),
                SpecificCIIDsSelection.Build(ga, gb).Except(AllCIIDsExceptSelection.Build(gb, gc)));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                SpecificCIIDsSelection.Build(ga, gb).Except(new AllCIIDsSelection()));
            Assert.AreEqual(
                SpecificCIIDsSelection.Build(ga, gb),
                SpecificCIIDsSelection.Build(ga, gb).Except(new NoCIIDsSelection()));

            // allExcept.Except(X)
            Assert.AreEqual(
                AllCIIDsExceptSelection.Build(ga, gb, gc),
                AllCIIDsExceptSelection.Build(ga, gb).Except(SpecificCIIDsSelection.Build(gb, gc)));
            Assert.AreEqual(
                SpecificCIIDsSelection.Build(gc),
                AllCIIDsExceptSelection.Build(ga, gb).Except(AllCIIDsExceptSelection.Build(gb, gc)));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                AllCIIDsExceptSelection.Build(ga, gb).Except(new AllCIIDsSelection()));
            Assert.AreEqual(
                AllCIIDsExceptSelection.Build(ga, gb),
                AllCIIDsExceptSelection.Build(ga, gb).Except(new NoCIIDsSelection()));

            // all.Except(X)
            Assert.AreEqual(
                AllCIIDsExceptSelection.Build(gb, gc),
                new AllCIIDsSelection().Except(SpecificCIIDsSelection.Build(gb, gc)));
            Assert.AreEqual(
                SpecificCIIDsSelection.Build(gb, gc),
                new AllCIIDsSelection().Except(AllCIIDsExceptSelection.Build(gb, gc)));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                new AllCIIDsSelection().Except(new AllCIIDsSelection()));
            Assert.AreEqual(
                new AllCIIDsSelection(),
                new AllCIIDsSelection().Except(new NoCIIDsSelection()));

            // None.Except(X)
            Assert.AreEqual(
                new NoCIIDsSelection(),
                new NoCIIDsSelection().Except(SpecificCIIDsSelection.Build(gb, gc)));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                new NoCIIDsSelection().Except(AllCIIDsExceptSelection.Build(gb, gc)));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                new NoCIIDsSelection().Except(new AllCIIDsSelection()));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                new NoCIIDsSelection().Except(new NoCIIDsSelection()));
        }


        [Test]
        public void TestMinus()
        {
            var ga = Guid.NewGuid();
            var gb = Guid.NewGuid();
            var gc = Guid.NewGuid();

            // specific.Intersect(X)
            Assert.AreEqual(
                SpecificCIIDsSelection.Build(gb),
                SpecificCIIDsSelection.Build(ga, gb).Intersect(SpecificCIIDsSelection.Build(gb, gc)));
            Assert.AreEqual(
                SpecificCIIDsSelection.Build(ga),
                SpecificCIIDsSelection.Build(ga, gb).Intersect(AllCIIDsExceptSelection.Build(gb, gc)));
            Assert.AreEqual(
                SpecificCIIDsSelection.Build(ga, gb),
                SpecificCIIDsSelection.Build(ga, gb).Intersect(new AllCIIDsSelection()));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                SpecificCIIDsSelection.Build(ga, gb).Intersect(new NoCIIDsSelection()));

            // allExcept.Intersect(X)
            Assert.AreEqual(
                SpecificCIIDsSelection.Build(gc),
                AllCIIDsExceptSelection.Build(ga, gb).Intersect(SpecificCIIDsSelection.Build(gb, gc)));
            Assert.AreEqual( // gc intersect ga
                AllCIIDsExceptSelection.Build(ga, gb, gc),
                AllCIIDsExceptSelection.Build(ga, gb).Intersect(AllCIIDsExceptSelection.Build(gb, gc)));
            Assert.AreEqual(
                AllCIIDsExceptSelection.Build(ga, gb),
                AllCIIDsExceptSelection.Build(ga, gb).Intersect(new AllCIIDsSelection()));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                AllCIIDsExceptSelection.Build(ga, gb).Intersect(new NoCIIDsSelection()));

            // all.Intersect(X)
            Assert.AreEqual(
                SpecificCIIDsSelection.Build(gb, gc),
                new AllCIIDsSelection().Intersect(SpecificCIIDsSelection.Build(gb, gc)));
            Assert.AreEqual(
                AllCIIDsExceptSelection.Build(gb, gc),
                new AllCIIDsSelection().Intersect(AllCIIDsExceptSelection.Build(gb, gc)));
            Assert.AreEqual(
                new AllCIIDsSelection(),
                new AllCIIDsSelection().Intersect(new AllCIIDsSelection()));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                new AllCIIDsSelection().Intersect(new NoCIIDsSelection()));

            // None.Intersect(X)
            Assert.AreEqual(
                new NoCIIDsSelection(),
                new NoCIIDsSelection().Intersect(SpecificCIIDsSelection.Build(gb, gc)));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                new NoCIIDsSelection().Intersect(AllCIIDsExceptSelection.Build(gb, gc)));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                new NoCIIDsSelection().Intersect(new AllCIIDsSelection()));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                new NoCIIDsSelection().Intersect(new NoCIIDsSelection()));
        }
    }
}
