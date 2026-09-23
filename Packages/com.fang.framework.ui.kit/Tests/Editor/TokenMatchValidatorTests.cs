using System.Collections.Generic;
using System.Linq;
using Fang.Framework.UI.Kit.Internal;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit.Editor.Tests
{
    public class TokenMatchValidatorTests
    {
        [TearDown]
        public void TearDown()
        {
            KitTestSetup.Cleanup();
        }

        [Test]
        public void Validate_reports_nothing_for_a_complete_match()
        {
            var match = KitTestSetup.NewMatch(("Color/Primary", NewImage()));
            var project = KitTestSetup.NewProject(KitTestSetup.NewToken<ColorTokenSo>("Color/Primary"));

            Assert.AreEqual(0, TokenMatchValidator.Validate(match, project).Count);
        }

        [Test]
        public void Validate_accepts_several_tokens_on_one_id()
        {
            var match = KitTestSetup.NewMatch(("Text/Title", NewText()));
            var project = KitTestSetup.NewProject(
                KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title"),
                KitTestSetup.NewToken<ColorTokenSo>("Text/Title"));

            Assert.AreEqual(0, TokenMatchValidator.Validate(match, project).Count);
        }

        [Test]
        public void Validate_reports_a_missing_id()
        {
            var image = NewImage();
            var match = KitTestSetup.NewMatch((null, image), (string.Empty, image));

            var issues = TokenMatchValidator.Validate(match, KitTestSetup.NewProject());

            CollectionAssert.AreEqual(
                new[] { TokenIssueKind.EmptyId, TokenIssueKind.EmptyId },
                Kinds(issues));
            Assert.AreEqual(0, issues[0].EntryIndex);
            Assert.AreEqual(1, issues[1].EntryIndex);
            Assert.AreSame(match, issues[0].Match);
        }

        [Test]
        public void Validate_accepts_any_hand_written_id()
        {
            var match = KitTestSetup.NewMatch(("Text/Foo", NewImage()));
            var project = KitTestSetup.NewProject(KitTestSetup.NewToken<ColorTokenSo>("Text/Foo"));

            Assert.AreEqual(0, TokenMatchValidator.Validate(match, project).Count);
        }

        [Test]
        public void Validate_reports_a_missing_target()
        {
            var match = KitTestSetup.NewMatch(("Color/Primary", null));

            var issues = TokenMatchValidator.Validate(match, KitTestSetup.NewProject());

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(TokenIssueKind.EmptyTarget, issues[0].Kind);
            Assert.AreEqual("Color/Primary", issues[0].Id);
        }

        [Test]
        public void Validate_reports_a_missing_token()
        {
            var match = KitTestSetup.NewMatch(("Color/Primary", NewImage()));

            var issues = TokenMatchValidator.Validate(match, KitTestSetup.NewProject());

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(TokenIssueKind.MissingToken, issues[0].Kind);
        }

        [Test]
        public void Validate_reports_a_target_of_the_wrong_type()
        {
            var match = KitTestSetup.NewMatch(("Text/Title", NewImage()));
            var project = KitTestSetup.NewProject(KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title"));

            var issues = TokenMatchValidator.Validate(match, project);

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(TokenIssueKind.TargetTypeMismatch, issues[0].Kind);
        }

        [Test]
        public void Validate_reports_every_token_that_does_not_accept_the_target()
        {
            var match = KitTestSetup.NewMatch(("Text/Title", NewImage()));
            var project = KitTestSetup.NewProject(
                KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title"),
                KitTestSetup.NewToken<ColorTokenSo>("Text/Title"));

            var issues = TokenMatchValidator.Validate(match, project);

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(TokenIssueKind.TargetTypeMismatch, issues[0].Kind);
        }

        [Test]
        public void Validate_reports_nothing_for_a_null_match()
        {
            Assert.AreEqual(0, TokenMatchValidator.Validate((TokenMatch)null, KitTestSetup.NewProject()).Count);
        }

        [Test]
        public void Validate_reports_the_missing_token_when_the_project_is_null()
        {
            var match = KitTestSetup.NewMatch(("Color/Primary", NewImage()));

            var issues = TokenMatchValidator.Validate(match, null);

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(TokenIssueKind.MissingToken, issues[0].Kind);
        }

        [Test]
        public void Validate_checks_every_match_of_a_sequence()
        {
            var matches = new List<TokenMatch>
            {
                KitTestSetup.NewMatch(("Color/Primary", NewImage())),
                KitTestSetup.NewMatch(("Color/Primary", null))
            };
            var project = KitTestSetup.NewProject(KitTestSetup.NewToken<ColorTokenSo>("Color/Primary"));

            var issues = TokenMatchValidator.Validate(matches, project);

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(TokenIssueKind.EmptyTarget, issues[0].Kind);
            Assert.AreSame(matches[1], issues[0].Match);
        }

        [Test]
        public void ValidateProject_reports_nothing_for_a_complete_project()
        {
            var project = KitTestSetup.NewProject(
                KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title"),
                KitTestSetup.NewToken<ColorTokenSo>("Text/Title"),
                KitTestSetup.NewToken<ColorTokenSo>("Color/Primary"));

            Assert.AreEqual(0, TokenMatchValidator.ValidateProject(project).Count);
        }

        [Test]
        public void ValidateProject_reports_an_empty_row()
        {
            var project = KitTestSetup.NewProject((TokenSo)null);

            var issues = TokenMatchValidator.ValidateProject(project);

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(TokenIssueKind.EmptyEntry, issues[0].Kind);
            Assert.IsNull(issues[0].Match);
            Assert.AreEqual(-1, issues[0].EntryIndex);
        }

        [Test]
        public void ValidateProject_hints_a_token_that_is_not_enabled()
        {
            var project = KitTestSetup.NewProject(KitTestSetup.NewToken<ColorTokenSo>());

            var issues = TokenMatchValidator.ValidateProject(project);

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(TokenIssueKind.TokenNotEnabled, issues[0].Kind);
            Assert.IsTrue(issues[0].IsHint);
            Assert.IsNull(issues[0].Id);
        }

        [Test]
        public void ValidateProject_does_not_count_a_not_enabled_token_as_an_error()
        {
            var project = KitTestSetup.NewProject(
                KitTestSetup.NewToken<ColorTokenSo>(),
                KitTestSetup.NewToken<ColorTokenSo>("Color/Primary"));

            var issues = TokenMatchValidator.ValidateProject(project);

            Assert.AreEqual(1, issues.Count);
            Assert.IsTrue(issues[0].IsHint);
            Assert.IsFalse(issues.Any(issue => !issue.IsHint));
        }

        [Test]
        public void ValidateProject_accepts_any_hand_written_id()
        {
            var project = KitTestSetup.NewProject(KitTestSetup.NewToken<ColorTokenSo>("Color/Foo"));

            Assert.AreEqual(0, TokenMatchValidator.ValidateProject(project).Count);
        }

        [Test]
        public void ValidateProject_accepts_several_tokens_on_one_id()
        {
            var project = KitTestSetup.NewProject(
                KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title"),
                KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title"));

            Assert.AreEqual(0, TokenMatchValidator.ValidateProject(project).Count);
        }

        private static List<TokenIssueKind> Kinds(IReadOnlyList<TokenIssue> issues)
        {
            return issues.Select(issue => issue.Kind).ToList();
        }

        private static Image NewImage()
        {
            return KitTestSetup.NewGameObject("Image").AddComponent<Image>();
        }

        private static TextMeshProUGUI NewText()
        {
            return KitTestSetup.NewGameObject("Text").AddComponent<TextMeshProUGUI>();
        }
    }
}
