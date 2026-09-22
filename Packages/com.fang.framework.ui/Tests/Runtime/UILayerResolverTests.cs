using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.UI.Tests
{
    public class UILayerResolverTests
    {
        private readonly List<UILayerConfigDataSo> _created = new List<UILayerConfigDataSo>();

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _created.Count; i++)
            {
                Object.DestroyImmediate(_created[i]);
            }

            _created.Clear();
        }

        [Test]
        public void Resolve_adds_the_layer_base_sort_order()
        {
            var layers = new List<UILayerConfigDataSo>
            {
                CreateLayer("hud", 100),
                CreateLayer("popup", 200)
            };

            Assert.AreEqual(105, UILayerResolver.Resolve(layers, "hud", 5));
            Assert.AreEqual(207, UILayerResolver.Resolve(layers, "popup", 7));
        }

        [Test]
        public void Resolve_without_a_layer_id_returns_the_panel_sort_order()
        {
            var layers = new List<UILayerConfigDataSo> { CreateLayer("hud", 100) };

            Assert.AreEqual(7, UILayerResolver.Resolve(layers, string.Empty, 7));
            Assert.AreEqual(7, UILayerResolver.Resolve(layers, null, 7));
        }

        [Test]
        public void Resolve_with_an_unknown_layer_returns_the_panel_sort_order()
        {
            var layers = new List<UILayerConfigDataSo> { CreateLayer("hud", 100) };

            Assert.AreEqual(7, UILayerResolver.Resolve(layers, "missing", 7));
        }

        [Test]
        public void Resolve_without_layers_returns_the_panel_sort_order()
        {
            Assert.AreEqual(7, UILayerResolver.Resolve(null, "hud", 7));
        }

        [Test]
        public void Resolve_skips_null_layer_entries()
        {
            var layers = new List<UILayerConfigDataSo> { null, CreateLayer("hud", 100) };

            Assert.AreEqual(105, UILayerResolver.Resolve(layers, "hud", 5));
        }

        private UILayerConfigDataSo CreateLayer(string id, int baseSortOrder)
        {
            var layer = ScriptableObject.CreateInstance<UILayerConfigDataSo>();
            var serialized = new SerializedObject(layer);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_baseSortOrder").intValue = baseSortOrder;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _created.Add(layer);
            return layer;
        }
    }
}
