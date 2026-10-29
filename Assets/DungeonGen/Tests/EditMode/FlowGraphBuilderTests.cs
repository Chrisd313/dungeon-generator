using DungeonGen.Core;
using NUnit.Framework;

namespace DungeonGen.Tests
{
    public class FlowGraphBuilderTests
    {
        TestRecipes _recipes;

        [SetUp] public void SetUp() => _recipes = new TestRecipes();
        [TearDown] public void TearDown() => _recipes.Destroy();

        [Test]
        public void BuildsMainPathBranchesAndLoops()
        {
            DungeonRecipe recipe = _recipes.Create(mainPath: 9, branches: 2, branchLength: 3, loops: 1);
            Assert.IsTrue(FlowGraphBuilder.TryBuild(recipe, new Pcg32(5), out FlowGraph g, out string failure), failure);

            Assert.AreEqual(15, g.nodes.Count);
            Assert.AreEqual(9, g.mainPath.Count);
            for (int i = 0; i < g.mainPath.Count; i++)
            {
                FlowNode n = g.nodes[g.mainPath[i]];
                Assert.IsTrue(n.isMainPath);
                Assert.AreEqual(i, n.depth);
                Assert.AreEqual(i, n.mainPathIndex);
            }

            int treeEdges = 0, loopEdges = 0;
            foreach (FlowEdge e in g.edges)
            {
                if (e.isLoop) loopEdges++;
                else treeEdges++;
            }
            Assert.AreEqual(14, treeEdges, "A tree over 15 nodes has 14 edges.");
            Assert.AreEqual(1, loopEdges);
        }

        [Test]
        public void BranchesNeverAttachToStartOrEnd()
        {
            DungeonRecipe recipe = _recipes.Create(mainPath: 4, branches: 3, branchLength: 1, loops: 0);
            for (int seed = 0; seed < 200; seed++)
            {
                Assert.IsTrue(FlowGraphBuilder.TryBuild(recipe, new Pcg32(seed), out FlowGraph g, out _));
                Assert.AreEqual(1, g.Degree(g.StartId, false));
                Assert.AreEqual(1, g.Degree(g.EndId, false));
            }
        }

        [Test]
        public void LoopsNeverDuplicateExistingEdges()
        {
            DungeonRecipe recipe = _recipes.Create(mainPath: 6, branches: 2, branchLength: 2, loops: 3);
            for (int seed = 0; seed < 200; seed++)
            {
                Assert.IsTrue(FlowGraphBuilder.TryBuild(recipe, new Pcg32(seed), out FlowGraph g, out _));
                for (int i = 0; i < g.edges.Count; i++)
                for (int j = i + 1; j < g.edges.Count; j++)
                {
                    FlowEdge a = g.edges[i], b = g.edges[j];
                    bool same = (a.from == b.from && a.to == b.to) || (a.from == b.to && a.to == b.from);
                    Assert.IsFalse(same, $"seed {seed}: duplicate edge {a.from}-{a.to}");
                }
            }
        }

        [Test]
        public void BranchesOnTooShortMainPath_FailWithReason()
        {
            DungeonRecipe recipe = _recipes.Create(mainPath: 2, branches: 1, branchLength: 1, loops: 0);
            Assert.IsFalse(FlowGraphBuilder.TryBuild(recipe, new Pcg32(1), out _, out string failure));
            StringAssert.Contains("at least 3", failure);
        }
    }
}
