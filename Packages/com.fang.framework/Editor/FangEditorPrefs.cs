using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Fang.Framework.Editor
{
    public static class FangEditorPrefs
    {
        private const string Prefix = "Fang.Framework.Editor";

        private static string projectToken;

        public static string BuildKey(string scope, string key)
        {
            return Prefix + "." + GetProjectToken() + "." + scope + "." + key;
        }

        private static string GetProjectToken()
        {
            if (!string.IsNullOrEmpty(projectToken))
            {
                return projectToken;
            }

            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(Application.dataPath.Replace('\\', '/').ToLowerInvariant());
                var hash = md5.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                for (var i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                projectToken = builder.ToString();
            }

            return projectToken;
        }
    }
}
