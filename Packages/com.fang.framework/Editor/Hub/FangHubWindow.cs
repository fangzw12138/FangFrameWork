using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.Editor.Hub
{
    public sealed class FangHubWindow : EditorWindow
    {
        private const float SidebarWidth = 240f;
        private const float RowHeight = 20f;
        private const float DragHandleWidth = 16f;
        private const float IndicatorWidth = 12f;
        private const string DragTypeGroup = "group";
        private const string DragTypePage = "page";
        private const string DropIndicatorName = "fanghub-drop-indicator";

        private static readonly Color DimColor = new Color(0.62f, 0.62f, 0.62f);
        private static readonly Color LineColor = new Color(0.28f, 0.28f, 0.28f);
        private static readonly Color HoverColor = new Color(0.21f, 0.29f, 0.44f);
        private static readonly Color SelectedColor = new Color(0.24f, 0.37f, 0.59f);
        private static readonly Color ActiveColor = new Color(0.3f, 0.8f, 0.3f);
        private static readonly Color DropColor = new Color(0.31f, 0.55f, 1f);
        private static readonly Color ErrorColor = new Color(0.9f, 0.5f, 0.4f);

        private sealed class PageRowRefs
        {
            public VisualElement Row;
            public Label Indicator;
        }

        private readonly Dictionary<string, PageRowRefs> pageRows = new Dictionary<string, PageRowRefs>();
        private readonly Dictionary<string, VisualElement> groupHeaders = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, FangHubPageDescriptor> pagesById = new Dictionary<string, FangHubPageDescriptor>();

        private IReadOnlyList<FangHubPageDescriptor> pages = new List<FangHubPageDescriptor>();
        private FangHubLayout layout;
        private string searchText = string.Empty;
        private FangHubPageDescriptor selectedPage;
        private IFangHubPage pageInstance;
        private VisualElement pageElement;
        private VisualElement sidebar;
        private VisualElement detailContent;
        private Label detailTitle;
        private Label detailDescription;
        private VisualElement dropIndicator;
        private bool dragging;

        [MenuItem("Tools/Fang Framework/Fang Hub")]
        public static void Open()
        {
            var window = GetWindow<FangHubWindow>("Fang Hub");
            window.minSize = new Vector2(720f, 420f);
            window.Show();
        }

        private void CreateGUI()
        {
            BuildTree();
            Reload();
        }

        private void OnDisable()
        {
            DestroyPage();
        }

        private void BuildTree()
        {
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.Add(BuildToolbar());
            rootVisualElement.Add(BuildBody());
            rootVisualElement.RegisterCallback<DragExitedEvent>(_ => CleanupDrag());
        }

        private VisualElement BuildToolbar()
        {
            var toolbar = CreateRow();
            toolbar.style.paddingLeft = 8f;
            toolbar.style.paddingRight = 8f;
            toolbar.style.paddingTop = 4f;
            toolbar.style.paddingBottom = 4f;
            toolbar.style.borderBottomWidth = 1f;
            toolbar.style.borderBottomColor = LineColor;

            var title = CreateLabel("Fang Hub");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginRight = 8f;

            var addButton = new Button(ShowAddPageMenu);
            addButton.text = "+";
            addButton.style.width = 22f;
            addButton.style.height = 20f;
            addButton.style.marginRight = 8f;

            var search = new TextField();
            search.style.flexGrow = 1f;
            search.style.maxWidth = 240f;
            search.style.marginLeft = StyleKeyword.Auto;
            search.RegisterValueChangedCallback(evt =>
            {
                searchText = evt.newValue ?? string.Empty;
                RebuildSidebar();
            });

            toolbar.Add(title);
            toolbar.Add(addButton);
            toolbar.Add(search);
            return toolbar;
        }

        private VisualElement BuildBody()
        {
            var body = CreateRow();
            body.style.flexGrow = 1f;
            body.style.alignItems = Align.Stretch;

            var scroll = new ScrollView();
            scroll.style.width = SidebarWidth;
            scroll.style.flexShrink = 0f;
            scroll.style.borderRightWidth = 1f;
            scroll.style.borderRightColor = LineColor;

            sidebar = scroll.contentContainer;
            sidebar.RegisterCallback<DragUpdatedEvent>(OnGroupDragUpdated);
            sidebar.RegisterCallback<DragPerformEvent>(OnGroupDragPerform);

            var detail = new VisualElement();
            detail.style.flexGrow = 1f;
            detail.style.paddingLeft = 16f;
            detail.style.paddingRight = 16f;
            detail.style.paddingTop = 12f;
            detail.style.paddingBottom = 12f;

            detailTitle = CreateLabel(string.Empty);
            detailTitle.style.fontSize = 18f;
            detailTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            detailTitle.style.marginBottom = 6f;

            detailDescription = CreateLabel(string.Empty);
            detailDescription.style.color = DimColor;
            detailDescription.style.whiteSpace = WhiteSpace.Normal;
            detailDescription.style.marginBottom = 10f;

            detailContent = new VisualElement();
            detailContent.style.flexGrow = 1f;

            detail.Add(detailTitle);
            detail.Add(detailDescription);
            detail.Add(detailContent);

            body.Add(scroll);
            body.Add(detail);
            return body;
        }

        private void Reload()
        {
            pages = FangHubPageRegistry.Discover();
            pagesById.Clear();
            for (var i = 0; i < pages.Count; i++)
            {
                pagesById[pages[i].Id] = pages[i];
            }

            layout = FangHubLayoutStore.Load(pages);
            RebuildSidebar();
            ClearDetail();
        }

        private void RebuildSidebar()
        {
            if (sidebar == null)
            {
                return;
            }

            sidebar.Clear();
            pageRows.Clear();
            groupHeaders.Clear();

            var views = FangHubPageRegistry.BuildView(pages, layout, searchText);
            for (var i = 0; i < views.Count; i++)
            {
                sidebar.Add(BuildGroup(views[i]));
            }

            if (views.Count == 0)
            {
                var empty = CreateLabel(string.IsNullOrWhiteSpace(searchText) ? "没有可用的工具页。" : "没有匹配的工具页。");
                empty.style.color = DimColor;
                empty.style.paddingLeft = 8f;
                sidebar.Add(empty);
            }
        }

        private VisualElement BuildGroup(FangHubGroupView view)
        {
            var group = view.Group;
            var container = new VisualElement();

            var header = CreateRow();
            header.style.paddingLeft = 4f;
            header.style.paddingRight = 6f;
            header.style.paddingTop = 4f;
            header.style.paddingBottom = 3f;
            header.style.borderBottomWidth = 1f;
            header.style.borderBottomColor = LineColor;

            var groupHandle = CreateDragHandle("≡");
            groupHandle.RegisterCallback<PointerDownEvent>(evt => StartGroupDrag(evt, group.id, header));

            var arrow = CreateLabel(group.expanded ? "▼" : "▶");
            arrow.style.width = 14f;
            arrow.style.fontSize = 10f;
            arrow.style.color = DimColor;
            arrow.style.unityTextAlign = TextAnchor.MiddleCenter;

            var name = CreateLabel(group.name);
            name.style.flexGrow = 1f;
            name.style.unityFontStyleAndWeight = FontStyle.Bold;

            var count = CreateLabel("(" + view.Pages.Count + ")");
            count.style.fontSize = 10f;
            count.style.color = DimColor;

            header.Add(groupHandle);
            header.Add(arrow);
            header.Add(name);
            header.Add(count);
            header.RegisterCallback<ClickEvent>(_ =>
            {
                if (dragging)
                {
                    return;
                }

                layout.SetExpanded(group.id, !group.expanded);
                SaveLayout();
                RebuildSidebar();
            });
            header.AddManipulator(new ContextualMenuManipulator(menu =>
            {
                menu.menu.AppendAction("重命名", _ => StartRenameGroup(container, header, group));
                menu.menu.AppendAction("新建分组", _ => CreateGroup());
                menu.menu.AppendSeparator();
                menu.menu.AppendAction("删除分组", _ => DeleteGroup(group));
            }));

            var entries = new VisualElement();
            entries.userData = group.id;
            entries.style.display = group.expanded ? DisplayStyle.Flex : DisplayStyle.None;
            entries.RegisterCallback<DragUpdatedEvent>(OnPageDragUpdated);
            entries.RegisterCallback<DragPerformEvent>(OnPageDragPerform);
            entries.RegisterCallback<DragExitedEvent>(_ => HideDropIndicator());

            for (var i = 0; i < view.Pages.Count; i++)
            {
                var page = view.Pages[i];
                var row = BuildPageRow(page, group.id);
                entries.Add(row);
                pageRows[page.Id] = new PageRowRefs { Row = row, Indicator = (Label)row[1] };
            }

            container.Add(header);
            container.Add(entries);
            groupHeaders[group.id] = header;
            return container;
        }

        private VisualElement BuildPageRow(FangHubPageDescriptor page, string groupId)
        {
            var row = CreateRow();
            row.style.paddingLeft = 20f;
            row.style.paddingRight = 6f;
            row.style.minHeight = RowHeight;

            var handle = CreateDragHandle("⋮");
            handle.RegisterCallback<PointerDownEvent>(evt => StartPageDrag(evt, page.Id, groupId, row));

            var indicator = CreateLabel(IsSelected(page) ? "●" : string.Empty);
            indicator.style.width = IndicatorWidth;
            indicator.style.fontSize = 9f;
            indicator.style.unityTextAlign = TextAnchor.MiddleCenter;
            indicator.style.color = ActiveColor;

            var title = CreateLabel(page.Title);
            title.style.flexGrow = 1f;
            title.style.overflow = Overflow.Hidden;

            row.Add(handle);
            row.Add(indicator);
            row.Add(title);
            row.RegisterCallback<ClickEvent>(_ =>
            {
                if (!dragging)
                {
                    SelectPage(page);
                }
            });
            row.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (!IsSelected(page))
                {
                    SetBackground(row, HoverColor);
                }
            });
            row.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (!IsSelected(page))
                {
                    SetBackground(row, Color.clear);
                }
            });
            row.AddManipulator(new ContextualMenuManipulator(menu =>
            {
                menu.menu.AppendAction("删除页面", _ => RemovePage(page));
                menu.menu.AppendSeparator();
                for (var i = 0; i < layout.groups.Count; i++)
                {
                    var target = layout.groups[i];
                    if (target.id == groupId)
                    {
                        continue;
                    }

                    var targetId = target.id;
                    menu.menu.AppendAction("移动到/" + target.name, _ => MovePageToGroup(page, targetId));
                }
            }));

            SetBackground(row, IsSelected(page) ? SelectedColor : Color.clear);
            return row;
        }

        private void SelectPage(FangHubPageDescriptor page)
        {
            selectedPage = page;
            UpdateSelectionHighlight();
            ShowDetail(page);
        }

        private void ShowDetail(FangHubPageDescriptor page)
        {
            DestroyPage();

            if (detailContent == null)
            {
                return;
            }

            if (page == null)
            {
                detailTitle.text = string.Empty;
                detailDescription.text = "从左侧选择一个工具页。";
                return;
            }

            detailTitle.text = page.Title;
            detailDescription.text = string.IsNullOrEmpty(page.Description) ? page.Category : page.Description;

            try
            {
                pageInstance = (IFangHubPage)Activator.CreateInstance(page.PageType);
                pageInstance.OnInitialize(this);
            }
            catch (Exception exception)
            {
                pageInstance = null;
                detailContent.Add(CreateErrorLabel(exception.Message));
                return;
            }

            if (pageInstance is IFangHubVisualElementPage visualElementPage)
            {
                try
                {
                    pageElement = visualElementPage.CreateVisualElement();
                }
                catch (Exception exception)
                {
                    pageElement = null;
                    detailContent.Add(CreateErrorLabel(exception.Message));
                }

                if (pageElement != null)
                {
                    pageElement.style.flexGrow = 1f;
                    detailContent.Add(pageElement);
                }
            }
            else if (pageInstance is IFangHubImGuiPage)
            {
                var scroll = new ScrollView();
                scroll.style.flexGrow = 1f;
                scroll.Add(new IMGUIContainer(DrawImGuiPage));
                detailContent.Add(scroll);
            }

            try
            {
                pageInstance.OnSelected();
            }
            catch (Exception exception)
            {
                detailContent.Add(CreateErrorLabel(exception.Message));
            }
        }

        private void DrawImGuiPage()
        {
            if (!(pageInstance is IFangHubImGuiPage imGuiPage))
            {
                return;
            }

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("游戏运行时不刷新工具页预览。停止运行后可正常使用。", MessageType.Info);
                return;
            }

            try
            {
                var message = imGuiPage.OnGUI();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.HelpBox(message, MessageType.Info);
                }
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox("页面错误：" + exception.Message, MessageType.Error);
            }
        }

        private void ClearDetail()
        {
            selectedPage = null;
            UpdateSelectionHighlight();
            ShowDetail(null);
        }

        private void DestroyPage()
        {
            pageInstance = null;

            if (pageElement != null)
            {
                pageElement.RemoveFromHierarchy();
                pageElement = null;
            }

            if (detailContent != null)
            {
                detailContent.Clear();
            }
        }

        private void UpdateSelectionHighlight()
        {
            foreach (var pair in pageRows)
            {
                var selected = selectedPage != null && pair.Key == selectedPage.Id;
                SetBackground(pair.Value.Row, selected ? SelectedColor : Color.clear);
                pair.Value.Indicator.text = selected ? "●" : string.Empty;
            }
        }

        private bool IsSelected(FangHubPageDescriptor page)
        {
            return selectedPage != null && page != null && selectedPage.Id == page.Id;
        }

        private void StartRenameGroup(VisualElement container, VisualElement header, FangHubGroup group)
        {
            var field = new TextField();
            field.value = group.name;
            field.isDelayed = true;
            field.style.marginLeft = 4f;
            field.style.marginRight = 4f;

            container.Insert(0, field);
            header.style.display = DisplayStyle.None;

            void FinishRename(bool apply)
            {
                if (apply && !string.IsNullOrWhiteSpace(field.value) && field.value.Trim() != group.name)
                {
                    layout.RenameGroup(group.id, field.value);
                    SaveLayout();
                    RebuildSidebar();
                    return;
                }

                container.Remove(field);
                header.style.display = DisplayStyle.Flex;
            }

            field.RegisterCallback<BlurEvent>(_ => FinishRename(true));
            field.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    FinishRename(true);
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    FinishRename(false);
                    evt.StopPropagation();
                }
            });

            field.Focus();
            field.SelectAll();
        }

        private void CreateGroup()
        {
            layout.CreateGroup("新分组");
            SaveLayout();
            RebuildSidebar();
        }

        private void DeleteGroup(FangHubGroup group)
        {
            if (!EditorUtility.DisplayDialog("删除分组", "删除分组「" + group.name + "」？组内页面会回到未分配状态。", "删除", "取消"))
            {
                return;
            }

            layout.DeleteGroup(group.id);
            SaveLayout();
            RebuildSidebar();
        }

        private void RemovePage(FangHubPageDescriptor page)
        {
            layout.RemovePage(page.Id);
            SaveLayout();

            if (IsSelected(page))
            {
                selectedPage = null;
                UpdateSelectionHighlight();
                ShowDetail(null);
            }

            RebuildSidebar();
        }

        private void MovePageToGroup(FangHubPageDescriptor page, string groupId)
        {
            layout.MovePage(page.Id, groupId, -1);
            SaveLayout();
            RebuildSidebar();
        }

        private void ShowAddPageMenu()
        {
            var hidden = layout.GetHiddenPageIds(pages);
            var menu = new GenericMenu();

            if (hidden.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("没有未分配的页面"));
                menu.ShowAsContext();
                return;
            }

            var target = ResolveTargetGroup();
            if (target == null)
            {
                menu.AddDisabledItem(new GUIContent("没有分组，先新建分组"));
                menu.ShowAsContext();
                return;
            }

            for (var i = 0; i < hidden.Count; i++)
            {
                if (!pagesById.TryGetValue(hidden[i], out var page))
                {
                    continue;
                }

                var pageId = page.Id;
                var groupId = target.id;
                menu.AddItem(new GUIContent(page.Title + "  ->  " + target.name), false, () =>
                {
                    layout.AddPage(pageId, groupId);
                    SaveLayout();
                    RebuildSidebar();
                });
            }

            menu.ShowAsContext();
        }

        private FangHubGroup ResolveTargetGroup()
        {
            if (layout.groups.Count == 0)
            {
                return null;
            }

            if (selectedPage != null)
            {
                for (var i = 0; i < layout.groups.Count; i++)
                {
                    if (layout.groups[i].pageIds.Contains(selectedPage.Id))
                    {
                        return layout.groups[i];
                    }
                }
            }

            return layout.groups[0];
        }

        private void SaveLayout()
        {
            FangHubLayoutStore.Save(layout);
        }

        private void StartGroupDrag(PointerDownEvent evt, string groupId, VisualElement header)
        {
            if (evt.button != 0 || !evt.isPrimary)
            {
                return;
            }

            dragging = true;
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData("fanghub-drag-type", DragTypeGroup);
            DragAndDrop.SetGenericData("fanghub-group-id", groupId);
            DragAndDrop.StartDrag("FangHub Group");
            header.style.opacity = 0.5f;
            evt.StopPropagation();
        }

        private void StartPageDrag(PointerDownEvent evt, string pageId, string groupId, VisualElement row)
        {
            if (evt.button != 0 || !evt.isPrimary)
            {
                return;
            }

            dragging = true;
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData("fanghub-drag-type", DragTypePage);
            DragAndDrop.SetGenericData("fanghub-page-id", pageId);
            DragAndDrop.SetGenericData("fanghub-source-group-id", groupId);
            DragAndDrop.StartDrag("FangHub Page");
            row.style.opacity = 0.5f;
            evt.StopPropagation();
        }

        private void OnPageDragUpdated(DragUpdatedEvent evt)
        {
            if (!IsDragging(DragTypePage))
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            ShowDropIndicator(evt.currentTarget as VisualElement, CalculateInsertIndex(evt.currentTarget as VisualElement, evt.mousePosition));
            evt.StopPropagation();
        }

        private void OnPageDragPerform(DragPerformEvent evt)
        {
            if (!IsDragging(DragTypePage))
            {
                return;
            }

            var container = evt.currentTarget as VisualElement;
            var groupId = container?.userData as string;
            var pageId = DragAndDrop.GetGenericData("fanghub-page-id") as string;
            var sourceGroupId = DragAndDrop.GetGenericData("fanghub-source-group-id") as string;

            if (container != null && !string.IsNullOrEmpty(groupId) && !string.IsNullOrEmpty(pageId))
            {
                var insertIndex = CalculateInsertIndex(container, evt.mousePosition);
                if (sourceGroupId == groupId)
                {
                    layout.ReorderPage(groupId, pageId, insertIndex);
                }
                else
                {
                    layout.MovePage(pageId, groupId, insertIndex);
                }

                SaveLayout();
            }

            DragAndDrop.AcceptDrag();
            CleanupDrag();
            RebuildSidebar();
            evt.StopPropagation();
        }

        private void OnGroupDragUpdated(DragUpdatedEvent evt)
        {
            if (!IsDragging(DragTypeGroup))
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            var container = evt.currentTarget as VisualElement;
            ShowDropIndicator(container, CalculateInsertIndex(container, evt.mousePosition));
            evt.StopPropagation();
        }

        private void OnGroupDragPerform(DragPerformEvent evt)
        {
            if (!IsDragging(DragTypeGroup))
            {
                return;
            }

            var groupId = DragAndDrop.GetGenericData("fanghub-group-id") as string;
            var fromIndex = FindGroupIndex(groupId);
            var container = evt.currentTarget as VisualElement;

            if (fromIndex >= 0 && container != null)
            {
                layout.MoveGroup(fromIndex, CalculateInsertIndex(container, evt.mousePosition));
                SaveLayout();
            }

            DragAndDrop.AcceptDrag();
            CleanupDrag();
            RebuildSidebar();
            evt.StopPropagation();
        }

        private int FindGroupIndex(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                return -1;
            }

            for (var i = 0; i < layout.groups.Count; i++)
            {
                if (layout.groups[i].id == groupId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsDragging(string dragType)
        {
            return DragAndDrop.GetGenericData("fanghub-drag-type") as string == dragType;
        }

        private static int CalculateInsertIndex(VisualElement container, Vector2 mousePosition)
        {
            if (container == null)
            {
                return 0;
            }

            var localY = container.WorldToLocal(mousePosition).y;
            var index = 0;
            foreach (var child in container.Children())
            {
                if (IsDropIndicator(child))
                {
                    continue;
                }

                var bounds = child.layout;
                if (bounds.height > 0f && localY < bounds.y + bounds.height * 0.5f)
                {
                    return index;
                }

                index++;
            }

            return index;
        }

        private void ShowDropIndicator(VisualElement container, int insertIndex)
        {
            HideDropIndicator();

            if (container == null)
            {
                return;
            }

            dropIndicator = new VisualElement();
            dropIndicator.name = DropIndicatorName;
            dropIndicator.style.height = 2f;
            dropIndicator.style.flexShrink = 0f;
            dropIndicator.style.backgroundColor = DropColor;

            var index = insertIndex < 0 ? 0 : insertIndex;
            if (index > container.childCount)
            {
                index = container.childCount;
            }

            container.Insert(index, dropIndicator);
        }

        private void HideDropIndicator()
        {
            if (dropIndicator != null)
            {
                dropIndicator.RemoveFromHierarchy();
                dropIndicator = null;
            }
        }

        private void CleanupDrag()
        {
            dragging = false;
            HideDropIndicator();

            foreach (var pair in pageRows)
            {
                pair.Value.Row.style.opacity = 1f;
            }

            foreach (var pair in groupHeaders)
            {
                pair.Value.style.opacity = 1f;
            }
        }

        private static bool IsDropIndicator(VisualElement element)
        {
            return element.name == DropIndicatorName;
        }

        private static VisualElement CreateRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            return row;
        }

        private static Label CreateLabel(string text)
        {
            var label = new Label(text);
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            return label;
        }

        private static Label CreateDragHandle(string text)
        {
            var handle = CreateLabel(text);
            handle.style.width = DragHandleWidth;
            handle.style.flexShrink = 0f;
            handle.style.color = DimColor;
            handle.style.unityTextAlign = TextAnchor.MiddleCenter;
            return handle;
        }

        private static Label CreateErrorLabel(string message)
        {
            var label = CreateLabel("页面错误：" + message);
            label.style.color = ErrorColor;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static void SetBackground(VisualElement element, Color color)
        {
            element.style.backgroundColor = color;
        }
    }
}
