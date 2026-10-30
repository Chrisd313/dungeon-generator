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
                Assert.IsNotNull(view.PreviewTexture);

                var sprite = view.GetComponentInChildren<SpriteRenderer>(true);
                Assert.IsNotNull(sprite, "Preview sprite renderer should exist.");
                Assert.IsNotNull(sprite.sprite);
                Assert.IsNotNull(sprite.sharedMaterial, "An unlit sprite shader should be found.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
