using NUnit.Framework;
using Omnikeeper.Base.Model;
using System;

namespace Tests.Integration.Model
{
    class CIIDSelectionTest
    {
        [Test]
        public void TestUnionAll()
        {
            var ga = Guid.NewGuid();
            var gb = Guid.NewGuid();
            var gc = Guid.NewGuid();

            Assert.AreEqual(
                SpecificCIIDsSelection.Build(ga, gb, gc),
                CIIDSelectionExtensions.UnionAll(new[] { SpecificCIIDsSelection.Build(ga), SpecificCIIDsSelection.Build(ga, gb, gc) }));
            Assert.AreEqual(
                AllCIIDsSelection.Instance,
                CIIDSelectionExtensions.UnionAll(new[] { SpecificCIIDsSelection.Build(ga), AllCIIDsSelection.Instance }));
            Assert.AreEqual(
                SpecificCIIDsSelection.Build(ga, gb),
                CIIDSelectionExtensions.UnionAll(new[] { SpecificCIIDsSelection.Build(ga), SpecificCIIDsSelection.Build(ga, gb), new NoCIIDsSelection() }));
            Assert.AreEqual(
                AllCIIDsExceptSelection.Build(gb),
                CIIDSelectionExtensions.UnionAll(new[] { SpecificCIIDsSelection.Build(ga), AllCIIDsExceptSelection.Build(ga, gb) }));
            Assert.AreEqual(
                AllCIIDsSelection.Instance,
                CIIDSelectionExtensions.UnionAll(new[] { SpecificCIIDsSelection.Build(ga), AllCIIDsExceptSelection.Build(ga, gb), AllCIIDsSelection.Instance }));
        }

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
                SpecificCIIDsSelection.Build(ga, gb).Except(AllCIIDsSelection.Instance));
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
                AllCIIDsExceptSelection.Build(ga, gb).Except(AllCIIDsSelection.Instance));
            Assert.AreEqual(
                AllCIIDsExceptSelection.Build(ga, gb),
                AllCIIDsExceptSelection.Build(ga, gb).Except(new NoCIIDsSelection()));

            // all.Except(X)
            Assert.AreEqual(
                AllCIIDsExceptSelection.Build(gb, gc),
                AllCIIDsSelection.Instance.Except(SpecificCIIDsSelection.Build(gb, gc)));
            Assert.AreEqual(
                SpecificCIIDsSelection.Build(gb, gc),
                AllCIIDsSelection.Instance.Except(AllCIIDsExceptSelection.Build(gb, gc)));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                AllCIIDsSelection.Instance.Except(AllCIIDsSelection.Instance));
            Assert.AreEqual(
                AllCIIDsSelection.Instance,
                AllCIIDsSelection.Instance.Except(new NoCIIDsSelection()));

            // None.Except(X)
            Assert.AreEqual(
                new NoCIIDsSelection(),
                new NoCIIDsSelection().Except(SpecificCIIDsSelection.Build(gb, gc)));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                new NoCIIDsSelection().Except(AllCIIDsExceptSelection.Build(gb, gc)));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                new NoCIIDsSelection().Except(AllCIIDsSelection.Instance));
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
                SpecificCIIDsSelection.Build(ga, gb).Intersect(AllCIIDsSelection.Instance));
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
                AllCIIDsExceptSelection.Build(ga, gb).Intersect(AllCIIDsSelection.Instance));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                AllCIIDsExceptSelection.Build(ga, gb).Intersect(new NoCIIDsSelection()));

            // all.Intersect(X)
            Assert.AreEqual(
                SpecificCIIDsSelection.Build(gb, gc),
                AllCIIDsSelection.Instance.Intersect(SpecificCIIDsSelection.Build(gb, gc)));
            Assert.AreEqual(
                AllCIIDsExceptSelection.Build(gb, gc),
                AllCIIDsSelection.Instance.Intersect(AllCIIDsExceptSelection.Build(gb, gc)));
            Assert.AreEqual(
                AllCIIDsSelection.Instance,
                AllCIIDsSelection.Instance.Intersect(AllCIIDsSelection.Instance));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                AllCIIDsSelection.Instance.Intersect(new NoCIIDsSelection()));

            // None.Intersect(X)
            Assert.AreEqual(
                new NoCIIDsSelection(),
                new NoCIIDsSelection().Intersect(SpecificCIIDsSelection.Build(gb, gc)));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                new NoCIIDsSelection().Intersect(AllCIIDsExceptSelection.Build(gb, gc)));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                new NoCIIDsSelection().Intersect(AllCIIDsSelection.Instance));
            Assert.AreEqual(
                new NoCIIDsSelection(),
                new NoCIIDsSelection().Intersect(new NoCIIDsSelection()));
        }
    }
}
