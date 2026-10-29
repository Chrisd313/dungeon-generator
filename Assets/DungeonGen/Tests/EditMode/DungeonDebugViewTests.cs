using NUnit.Framework;
using UnityEngine;

namespace DungeonGen.Tests
{
    public class DungeonDebugViewTests
    {
        [Test]
        public void AddingComponent_GeneratesWithoutExplicitCall()
        {
            var go = new GameObject("DebugViewTest");
            try
            {
                var view = go.AddComponent<DungeonDebugView>();
                Assert.IsNotNull(view.Result, "OnEnable should generate a dungeon in edit mode.");
                Assert.IsTrue(view.Result.success, view.Result.failureReason);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
