using System;
using System.Collections.Generic;

namespace XboxDrivePermissionTool
{
    internal class ManagementObjectSearcher
    {
        private readonly string v;

        public ManagementObjectSearcher(string v)
        {
            this.v = v;
        }

        internal IEnumerable<ManagementObject> Get()
        {
            throw new NotImplementedException();
        }
    }
}