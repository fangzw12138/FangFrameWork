using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fang.Framework.Editor.Hub;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.EventBus.Editor
{
    [FangHubPage("framework-eventbus", "事件总线", "EventBus",
        Description = "查看运行中 EventBus 的订阅表：事件类型、订阅者数量与处理函数。", Order = 0)]
    public sealed class EventBusPage : IFangHubVisualElementPage
    {
        private static readonly FieldInfo HandlersField = typeof(EventBus).GetField(
            "_handlers",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Color DimColor = new Color(0.65f, 0.65f, 0.65f);

        private VisualElement instances;

        public void OnInitialize(FangHubWindow window)
        {
        }

        public void OnSelected()
        {
            Refresh();
        }

        public VisualElement CreateVisualElement()
        {
            var root = new VisualElement();
            root.style.flexGrow = 1f;

            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.alignItems = Align.Center;

            var refreshButton = new Button(Refresh);
            refreshButton.text = "刷新";

            var hint = new Label("订阅表是运行时状态：进入 Play 模式后点「刷新」。");
            hint.style.marginLeft = 8f;
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.style.color = DimColor;

            toolbar.Add(refreshButton);
            toolbar.Add(hint);
            root.Add(toolbar);

            instances = new VisualElement();
            instances.style.flexGrow = 1f;
            instances.style.marginTop = 8f;
            root.Add(instances);

            Refresh();
            return root;
        }

        private void Refresh()
        {
            if (instances == null)
            {
                return;
            }

            instances.Clear();

            if (!Application.isPlaying)
            {
                instances.Add(BuildMessage("未进入 Play 模式。"));
                return;
            }

            var buses = FindBuses();
            if (buses.Count == 0)
            {
                instances.Add(BuildMessage("场景里没有 EventBus。它由 Scope.AddService<EventBus>() 创建。"));
                return;
            }

            for (var i = 0; i < buses.Count; i++)
            {
                instances.Add(BuildBusBlock(buses[i]));
            }
        }

        private static List<EventBus> FindBuses()
        {
            var buses = new List<EventBus>();
            var candidates = Resources.FindObjectsOfTypeAll<EventBus>();
            for (var i = 0; i < candidates.Length; i++)
            {
                var bus = candidates[i];
                if (bus != null && bus.gameObject.scene.IsValid())
                {
                    buses.Add(bus);
                }
            }

            return buses;
        }

        private static VisualElement BuildBusBlock(EventBus bus)
        {
            var block = new VisualElement();
            block.style.marginBottom = 12f;

            var title = new Label(BuildPath(bus.transform));
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            block.Add(title);

            var handlers = ReadHandlers(bus);
            if (handlers.Count == 0)
            {
                block.Add(BuildMessage("没有订阅。"));
                return block;
            }

            var types = new List<Type>();
            foreach (var type in handlers.Keys)
            {
                types.Add(type);
            }

            types.Sort(CompareTypeNames);

            for (var i = 0; i < types.Count; i++)
            {
                block.Add(BuildEventBlock(types[i], handlers[types[i]]));
            }

            return block;
        }

        private static VisualElement BuildEventBlock(Type type, List<Delegate> subscribers)
        {
            var block = new VisualElement();
            block.style.marginLeft = 8f;
            block.style.marginTop = 4f;

            block.Add(new Label(type.FullName + "  (" + subscribers.Count + ")"));

            for (var i = 0; i < subscribers.Count; i++)
            {
                var subscriber = subscribers[i];
                var target = subscriber.Target;
                var owner = target == null ? "静态" : target.GetType().Name;

                var line = new Label("· " + owner + "." + subscriber.Method.Name);
                line.style.marginLeft = 12f;
                line.style.fontSize = 11f;
                line.style.color = DimColor;
                block.Add(line);
            }

            return block;
        }

        private static Dictionary<Type, List<Delegate>> ReadHandlers(EventBus bus)
        {
            var handlers = new Dictionary<Type, List<Delegate>>();
            if (HandlersField == null)
            {
                return handlers;
            }

            if (!(HandlersField.GetValue(bus) is IDictionary raw))
            {
                return handlers;
            }

            foreach (DictionaryEntry entry in raw)
            {
                if (!(entry.Key is Type type))
                {
                    continue;
                }

                var subscribers = new List<Delegate>();
                if (entry.Value is IEnumerable values)
                {
                    foreach (var value in values)
                    {
                        if (value is Delegate subscriber)
                        {
                            subscribers.Add(subscriber);
                        }
                    }
                }

                handlers[type] = subscribers;
            }

            return handlers;
        }

        private static int CompareTypeNames(Type left, Type right)
        {
            return string.CompareOrdinal(left.FullName, right.FullName);
        }

        private static string BuildPath(Transform transform)
        {
            var path = transform.name;
            var parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        private static Label BuildMessage(string text)
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = DimColor;
            return label;
        }
    }
}
