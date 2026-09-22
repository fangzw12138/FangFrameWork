using System;
using System.Collections.Generic;
using System.Linq;
using Fang.Framework.Editor.Hub;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.Editor.Tests
{
    public class FangHubPrefsTests
    {
        private const string Scope = "FangHubPrefsTests";
        private const string TextKey = "text";
        private const string FlagKey = "flag";
        private const string CountKey = "count";

        [TearDown]
        public void TearDown()
        {
            EditorPrefs.DeleteKey(FangEditorPrefs.BuildKey(Scope, TextKey));
            EditorPrefs.DeleteKey(FangEditorPrefs.BuildKey(Scope, FlagKey));
            EditorPrefs.DeleteKey(FangEditorPrefs.BuildKey(Scope, CountKey));
        }

        [Test]
        public void BuildKey_prefixes_scope_and_key_and_is_stable()
        {
            var key = FangEditorPrefs.BuildKey("Scope", "Key");

            StringAssert.StartsWith("Fang.Framework.Editor.", key);
            StringAssert.Contains(".Scope.Key", key);
            Assert.AreEqual(key, FangEditorPrefs.BuildKey("Scope", "Key"));
            Assert.AreNotEqual(key, FangEditorPrefs.BuildKey("OtherScope", "Key"));
            Assert.AreNotEqual(key, FangEditorPrefs.BuildKey("Scope", "OtherKey"));
        }

        [Test]
        public void PageState_round_trips_string_bool_and_int()
        {
            FangHubPageState.SetString(Scope, TextKey, "值");
            FangHubPageState.SetBool(Scope, FlagKey, true);
            FangHubPageState.SetInt(Scope, CountKey, 7);

            Assert.AreEqual("值", FangHubPageState.GetString(Scope, TextKey));
            Assert.IsTrue(FangHubPageState.GetBool(Scope, FlagKey));
            Assert.AreEqual(7, FangHubPageState.GetInt(Scope, CountKey));
        }

        [Test]
        public void PageState_returns_defaults_for_missing_keys()
        {
            Assert.AreEqual("fallback", FangHubPageState.GetString(Scope, "missing", "fallback"));
            Assert.IsTrue(FangHubPageState.GetBool(Scope, "missing", true));
            Assert.AreEqual(3, FangHubPageState.GetInt(Scope, "missing", 3));
        }

        [Test]
        public void Layout_survives_a_json_round_trip()
        {
            var pages = Pages(
                new FangHubPageDescriptor("a", "A", "A", string.Empty, 0, typeof(FangHubPrefsTests)),
                new FangHubPageDescriptor("b", "B", "A", string.Empty, 1, typeof(FangHubPrefsTests)));
            var layout = FangHubLayout.CreateDefault(pages);
            layout.SetExpanded(layout.groups[0].id, false);
            layout.RemovePage("b");

            var restored = JsonUtility.FromJson<FangHubLayout>(JsonUtility.ToJson(layout));

            Assert.IsNotNull(restored);
            Assert.AreEqual(layout.groups.Count, restored.groups.Count);
            Assert.AreEqual(layout.groups[0].id, restored.groups[0].id);
            Assert.AreEqual(layout.groups[0].name, restored.groups[0].name);
            Assert.IsFalse(restored.groups[0].expanded);
            CollectionAssert.AreEqual(layout.groups[0].pageIds, restored.groups[0].pageIds);
            CollectionAssert.AreEqual(layout.seenPageIds, restored.seenPageIds);
        }

        [Test]
        public void Layout_json_parsing_throws_on_invalid_text()
        {
            Assert.Throws<ArgumentException>(() => JsonUtility.FromJson<FangHubLayout>("not json"));
        }

        [Test]
        public void LayoutStore_load_returns_a_reconciled_layout()
        {
            var pages = FangHubPageRegistry.Discover();

            var layout = FangHubLayoutStore.Load(pages);

            Assert.IsNotNull(layout);
            var hidden = layout.GetHiddenPageIds(pages);
            foreach (var page in pages)
            {
                var visible = layout.groups.Any(group => group.pageIds.Contains(page.Id));
                Assert.IsTrue(visible || hidden.Contains(page.Id));
            }
        }

        private static List<FangHubPageDescriptor> Pages(params FangHubPageDescriptor[] pages)
        {
            return new List<FangHubPageDescriptor>(pages);
        }
    }
}
