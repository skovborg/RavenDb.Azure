using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RavenDb.Bundles.Azure.Hooks
{
    public static class DocumentUtilities
    {
        public static bool TryGetDatabaseNameFromKey(string key, out string databaseName)
        {
            const string prefix = "Raven/Databases/";

            if (key.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
            {
                databaseName = key.Replace(prefix, String.Empty);
                return true;
            }

            databaseName = null;
            return false;
        }
    }
}
