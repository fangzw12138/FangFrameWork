using System.Text;

namespace Fang.Framework.UI.Editor
{
    public static class UIPanelTemplateSource
    {
        public static string BuildDataScript(string namespaceName, string dataTypeName, string configTypeName)
        {
            var builder = new StringBuilder();
            builder.Append("using Fang.Framework.UI;\n\n");
            builder.Append("namespace ").Append(namespaceName).Append('\n');
            builder.Append("{\n");
            builder.Append("    public sealed class ").Append(dataTypeName).Append(" : UIPanelData<").Append(configTypeName).Append(">\n");
            builder.Append("    {\n");
            builder.Append("        public ").Append(dataTypeName).Append('(').Append(configTypeName).Append(" config) : base(config)\n");
            builder.Append("        {\n");
            builder.Append("        }\n");
            builder.Append("    }\n");
            builder.Append("}\n");
            return builder.ToString();
        }

        public static string BuildControllerScript(
            string namespaceName,
            string controllerTypeName,
            string dataTypeName,
            string baseTypeName,
            string configTypeName,
            bool visualTree)
        {
            var builder = new StringBuilder();
            builder.Append("using Fang.Framework.UI;\n\n");
            builder.Append("namespace ").Append(namespaceName).Append('\n');
            builder.Append("{\n");
            builder.Append("    public sealed class ").Append(controllerTypeName)
                .Append(" : ").Append(baseTypeName)
                .Append('<').Append(configTypeName).Append(", ").Append(dataTypeName).Append(">\n");
            builder.Append("    {\n");
            builder.Append("        public override ").Append(dataTypeName).Append(" CreateData(").Append(configTypeName).Append(" config)\n");
            builder.Append("        {\n");
            builder.Append("            return new ").Append(dataTypeName).Append("(config);\n");
            builder.Append("        }\n");
            builder.Append('\n');
            builder.Append("        public override void OnInit()\n");
            builder.Append("        {\n");
            if (visualTree)
            {
                builder.Append("            base.OnInit();\n");
            }

            builder.Append("        }\n");
            builder.Append('\n');
            builder.Append("        protected override void OnOpen()\n");
            builder.Append("        {\n");
            builder.Append("        }\n");
            builder.Append("    }\n");
            builder.Append("}\n");
            return builder.ToString();
        }

        public static string BuildPanelUxml(string panelName)
        {
            var builder = new StringBuilder();
            builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
            builder.Append("<ui:UXML xmlns:ui=\"UnityEngine.UIElements\">\n");
            builder.Append("    <ui:VisualElement name=\"root\" class=\"").Append(panelName).Append("\" style=\"flex-grow: 1;\">\n");
            builder.Append("    </ui:VisualElement>\n");
            builder.Append("</ui:UXML>\n");
            return builder.ToString();
        }

        public static string BuildPanelUss(string panelName)
        {
            var builder = new StringBuilder();
            builder.Append('.').Append(panelName).Append(" {\n");
            builder.Append("    flex-grow: 1;\n");
            builder.Append("    padding: 8px;\n");
            builder.Append("}\n");
            return builder.ToString();
        }
    }
}
